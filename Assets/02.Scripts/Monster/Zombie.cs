using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

// FSM(Finite State Machine/유한 상태 머신) 유한한 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법 
// - 상태 : 객체가 가질 수 있는 어떤 조건이나 상황
// - 전이 : 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것 
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신" 

// 좀비가 가질 수 있는 상태 
public enum ZombieState
{
    Idle, // 대기 : 제자리에서 플레이어를 기다린다.
    Patrol, // 순찰 : 처음 자리와 주변의 랜덤 지점을 오간다.
    Trace, // 추격 : 플레이어를 쫓아간다.
    Comeback, // 복귀 : 처음 자리로 돌아간다.
    Attack, // 공격 : 일정 시간마다 플레이어를 때린다.
    Hit, // 피격 : 공격(총알, 폭탄)을 받아 잠깐 멈칫한다.
    Death // 죽음 : 쓰러지고 잠시 뒤 사라진다. 
}

[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour, IDamageable
{
    [Header("Stat")]
    [SerializeField] private float _detectDistance = 8f;


    [SerializeField] private float _comebackDistance = 15f;
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackInterval = 1.2f;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _maxHealth = 30f;
    [SerializeField] private float _health = 100f;

    [Header("이펙트")]
    [SerializeField] private GameObject _bloodSprayPrefab; // 맞은 자리에서 뿜어지는 피

    [Header("Patrol")]
    [SerializeField] private float _patrolDistance = 5f; // 처음 자리에서 순찰 지점까지의 거리

    [SerializeField] private float _patrolSpeed = 2f; // 순찰할 때는 추격보다 천천히 걷는다
    [SerializeField] private float _idleWaitTime = 2f; // 지점에 도착한 뒤 다음 지점으로 가기 전까지 쉬는 시간
    [SerializeField] private float _arriveDistance = 0.5f; // 이 거리 안이면 도착한 것으로 본다

    [Header("Navigation")]
    [SerializeField] private float _navMeshSampleRadius = 2f; // 위치를 NavMesh 위로 맞출 때 찾아볼 반경 (Base Offset 보다 커야 함)
    [SerializeField] private int _patrolPointAttempts = 10; // 갈 수 있는 순찰 지점을 찾을 때 시도할 횟수

    private NavMeshPath _path; // 경로 계산용. 매번 새로 만들지 않도록 하나를 재사용한다.

    private float _attackTimer = 0f;
    private float _idleTimer = 0f;
    private float _patrolTimer = 0f;
    private Vector3 _patrolTarget;
    private Vector3 _startPosition;
    private GameObject _player;
    private NavMeshAgent _agent;
    private CapsuleCollider _collider;

    private ZombieState _state = ZombieState.Idle;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

    // 체력이 바뀔 때마다 알린다. (체력바가 구독)
    public event Action OnHealthChanged;

    private void Awake()
    {
        // 구독자(체력바)가 Start 에서 처음 값을 읽을 수 있도록 Awake 에서 초기화한다.
        _health = _maxHealth;
    }

    private void Start()
    {
        _startPosition = transform.position;
        _player = GameObject.FindGameObjectWithTag("Player");
        _collider = GetComponent<CapsuleCollider>();
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _moveSpeed;
        _path = new NavMeshPath();

        GameManager.Instance.RegisterZombie();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        switch (_state)
        {
            case ZombieState.Idle:
            {
                Idle();
                break;
            }
            case ZombieState.Patrol:
            {
                Patrol();
                break;
            }
            case ZombieState.Trace:
            {
                Trace();
                break;
            }
            case ZombieState.Comeback:
            {
                Comback();
                break;
            }
            case ZombieState.Attack:
            {
                Attack();
                break;
            }
            default:
            {
                break;
            }
        }
    }


    private void Idle()
    {
        // 대기 : 제자리에서 플레이어를 기다린다.
        StopAgent();

        // 플레이어 가까워지면 Trace
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance <= _detectDistance)
        {
            GameLogger.Log($"{_state} -> Trace ");
            _state = ZombieState.Trace;
            return;
        }

        // 잠깐 쉬었다가 다음 순찰 지점으로 출발한다.
        _idleTimer += Time.deltaTime;
        if (_idleTimer >= _idleWaitTime)
        {
            _idleTimer = 0f;
            StartPatrol();
        }
    }

    // 처음 자리 → 랜덤 지점 → 처음 자리 → 새 랜덤 지점 … 순서로 오간다.
    // 처음 자리 근처면 랜덤 지점으로, 아니면 처음 자리로 간다. (복귀 직후에도 헛걸음하지 않는다)
    private void StartPatrol()
    {
        Vector3 offsetFromStart = transform.position - _startPosition;
        offsetFromStart.y = 0f;
        bool isNearStart = offsetFromStart.magnitude <= _arriveDistance * 2f;

        // 처음 자리로 돌아갈 길이 없으면 (절벽 아래로 내려온 경우 등) 지금 자리를 새 처음 자리로 삼는다.
        if (isNearStart == false && HasCompletePath(transform.position, _startPosition) == false)
        {
            SetNewStartPosition();
            isNearStart = true;
        }

        _patrolTarget = isNearStart ? GetRandomPatrolPoint() : _startPosition;
        _patrolTimer = 0f;

        GameLogger.Log($"{_state} -> Patrol ({(isNearStart ? "랜덤 지점" : "처음 자리")})");
        _state = ZombieState.Patrol;
    }

    // 처음 자리에서 순찰 거리만큼 떨어진 원 위의 랜덤 지점
    // 순찰 중에 절벽 아래로 뛰어내리지 않도록, 처음 자리에서 갔다가 다시 돌아올 수 있는 지점만 고른다.
    private Vector3 GetRandomPatrolPoint()
    {
        for (int i = 0; i < _patrolPointAttempts; i++)
        {
            // 1. 처음 자리 둘레의 랜덤 방향 지점을 고른다.
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            if (randomDirection == Vector2.zero)
            {
                randomDirection = Vector2.right;
            }

            Vector3 point = _startPosition + new Vector3(randomDirection.x, 0f, randomDirection.y) * _patrolDistance;

            // 2. NavMesh 위의 가장 가까운 자리로 맞춘다. 근처에 NavMesh가 없으면 다시 뽑는다.
            if (NavMesh.SamplePosition(point, out NavMeshHit hit, _navMeshSampleRadius, _agent.areaMask) == false)
            {
                continue;
            }

            // 3. 갔다가 돌아올 수 있는 지점이면 사용한다.
            if (HasCompletePath(_startPosition, hit.position) && HasCompletePath(hit.position, _startPosition))
            {
                return hit.position;
            }
        }

        // 갈 수 있는 지점을 못 찾으면 처음 자리에 머문다.
        return _startPosition;
    }

    // from 에서 to 까지 끝까지 이어진 길이 있는지 (절벽 링크는 내려가는 방향만 있으므로 올라가는 길은 없다)
    private bool HasCompletePath(Vector3 from, Vector3 to)
    {
        if (NavMesh.SamplePosition(from, out NavMeshHit fromHit, _navMeshSampleRadius, _agent.areaMask) == false)
        {
            return false;
        }

        if (NavMesh.SamplePosition(to, out NavMeshHit toHit, _navMeshSampleRadius, _agent.areaMask) == false)
        {
            return false;
        }

        return NavMesh.CalculatePath(fromHit.position, toHit.position, _agent.areaMask, _path)
            && _path.status == NavMeshPathStatus.PathComplete;
    }

    // 지금 자리를 새 처음 자리로 삼는다. 이후 순찰·복귀는 이 자리를 기준으로 한다.
    private void SetNewStartPosition()
    {
        _startPosition = transform.position;
        GameLogger.Log($"[Zombie] 처음 자리로 돌아갈 길이 없어 새 처음 자리를 잡음 : {_startPosition}");
    }

    private void Patrol()
    {
        // Todo. Walk 애니메이션 실행

        // 순찰 중에도 플레이어가 가까워지면 Trace
        if (Vector3.Distance(transform.position, _player.transform.position) <= _detectDistance)
        {
            GameLogger.Log($"{_state} -> Trace ");
            _state = ZombieState.Trace;
            return;
        }

        Vector3 direction = _patrolTarget - transform.position;
        direction.y = 0f; // 높이 차이는 무시하고 바닥 기준 거리로 판단한다.

        // 목표에 도착했거나, 벽 등에 막혀 너무 오래 걸리면 대기하고 다음 지점으로 간다.
        _patrolTimer += Time.deltaTime;
        float patrolTimeLimit = _patrolDistance / _patrolSpeed * 2f + 1f;
        if (direction.magnitude <= _arriveDistance || _patrolTimer >= patrolTimeLimit)
        {
            GameLogger.Log($"{_state} -> Idle");
            _state = ZombieState.Idle;
            return;
        }

        //direction.Normalize();
        //
        //transform.forward = direction;
        //_controller.SimpleMove(direction * _patrolSpeed);
        _agent.SetDestination(_patrolTarget);
    }

    private void Trace()
    {
        // Todo. Run 애니메이션 실행 

        if (Vector3.Distance(transform.position, _startPosition) > _comebackDistance)
        {
            StartComeback();
            return;
        }

        // 플레이어가 공격 거리 안이면 공격한다. 
        if (Vector3.Distance(transform.position, _player.transform.position) <= _attackRange)
        {
            _attackTimer = 0f;

            GameLogger.Log($"{_state} -> Attack");
            _state = ZombieState.Attack;
            return;
        }

        //SetDestination은 목적지 설정
        // - 호출할 때마다 목적지를 향해 조금씩 이동 (X)
        // - 한 번 세팅하면 내부에서 계속 이동 (O)
        _agent.SetDestination(_player.transform.position);
    }

    // 추격을 멈추고 처음 자리로 돌아가기 시작한다.
    private void StartComeback()
    {
        // 처음 자리로 돌아갈 길이 없으면 (절벽 아래로 뛰어내린 경우 등) 지금 자리를 새 처음 자리로 삼고 대기한다.
        if (HasCompletePath(transform.position, _startPosition) == false)
        {
            SetNewStartPosition();
            GameLogger.Log($"{_state} -> Idle");
            _state = ZombieState.Idle;
            return;
        }

        GameLogger.Log($"{_state} -> Comeback");
        _state = ZombieState.Comeback;
        _agent.SetDestination(_startPosition);
    }

    private void Comback()
    {
        // pathStatus 는 "도착했다"가 아니라 "경로를 찾았다"는 뜻이므로, 남은 거리로 도착을 판단한다.
        if (_agent.pathPending == false && _agent.remainingDistance <= _arriveDistance)
        {
            GameLogger.Log($"{_state} -> Idle");
            _state = ZombieState.Idle;
            return;
        }

        //Vector3 direction = _startPosition - transform.position;
        //direction.y = 0f;
        //direction.Normalize();
        //
        //transform.forward = direction;
        //_controller.SimpleMove(direction * _moveSpeed);
        // 목적지는 StartComeback 에서 한 번만 정한다. 매 프레임 다시 정하면 경로를 계속 새로 계산해서 남은 거리가 흔들린다.
    }

    private void Attack()
    {
        // 공격 시작  
        StopAgent();

        // 플레이어가 공격 사정 거리를 벗어나면 
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            GameLogger.Log($"{_state} -> Trace ");
            _state = ZombieState.Trace;
            return;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackInterval)
        {
            // Todo. 공격 애니메이션 실행

            DamageInfo damageInfo = new DamageInfo()
            {
                Value = _attackDamage,
                HitPoint = _player.transform.position,
                HitNormal = (transform.position - _player.transform.position).normalized,
            };
            _player.GetComponent<IDamageable>()?.TakeDamage(damageInfo);

            _attackTimer = 0f;
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_state == ZombieState.Death)
        {
            return;
        }

        _health = Mathf.Max(0f, _health - damageInfo.Value);
        OnHealthChanged?.Invoke();

        StopAllCoroutines();

        if (_health > 0f)
        {
            SprayBlood(damageInfo);
            StartCoroutine(Hit_Coroutine());
        }
        else
        {
            StartCoroutine(Death_Coroutine());
        }
    }

    // 좀비의 자식으로 피 스프레이를 만들고, 맞은 자리와 방향으로 옮긴다.
    private void SprayBlood(DamageInfo damageInfo)
    {
        // Blood Gush 에셋을 아직 넣지 않았으면 건너뛴다.
        if (_bloodSprayPrefab == null) return;

        GameObject bloodSpray = Instantiate(_bloodSprayPrefab, transform);
        bloodSpray.transform.position = damageInfo.HitPoint;
        bloodSpray.transform.forward = damageInfo.HitNormal;
    }

    private IEnumerator Hit_Coroutine()
    {
        StopAgent();

        GameLogger.Log($"{_state} -> Hit ");
        _state = ZombieState.Hit; // 잠깐 멈칫 한다.

        yield return new WaitForSeconds(0.3f);


        _state = ZombieState.Trace;
    }

    private IEnumerator Death_Coroutine()
    {
        StopAgent();

        GameLogger.Log($"{_state} -> Death ");
        _state = ZombieState.Death; // 사망.

        // 1. 죽는 순간 남은 좀비 수에서 뺀다.
        GameManager.Instance.UnregisterZombie();

        // 2. 쓰러진 몸에 길이 막히지 않게 충돌을 끈다.
        _collider.enabled = false;

        // 3. 잠시 뒤 사라진다.
        yield return new WaitForSeconds(0.5f);


        Destroy(gameObject);
    }

    private void StopAgent()
    {
        // 절벽 링크를 지나는(뛰어내리는) 중에 멈추면 링크 위에 걸리므로, 착지할 때까지는 멈추지 않는다.
        if (_agent.isOnOffMeshLink) return;

        _agent.ResetPath(); // 에이전트의 목적지 삭제
        _agent.velocity = Vector3.zero; // 미끄럼 방지를 위해서 속도를 0으로 맞춰주기 
    }
}