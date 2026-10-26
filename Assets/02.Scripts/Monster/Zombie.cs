using System.Collections;
using UnityEngine;

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

public class Zombie : MonoBehaviour
{
    [Header("Stat")]
    [SerializeField] private float _detectDistance = 8f;


    [SerializeField] private float _comebackDistance = 15f;
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackInterval = 1.2f;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _maxHealth = 30f;
    [SerializeField] private float _health = 100f;

    [Header("Patrol")]
    [SerializeField] private float _patrolDistance = 5f; // 처음 자리에서 순찰 지점까지의 거리

    [SerializeField] private float _patrolSpeed = 2f; // 순찰할 때는 추격보다 천천히 걷는다
    [SerializeField] private float _idleWaitTime = 2f; // 지점에 도착한 뒤 다음 지점으로 가기 전까지 쉬는 시간
    [SerializeField] private float _arriveDistance = 0.5f; // 이 거리 안이면 도착한 것으로 본다

    private float _attackTimer = 0f;
    private float _idleTimer = 0f;
    private float _patrolTimer = 0f;
    private Vector3 _patrolTarget;
    private CharacterController _controller;
    private Vector3 _startPosition;
    private GameObject _player;

    private ZombieState _state = ZombieState.Idle;

    public float MaxHealth => _maxHealth;
    public float Health => _health;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _startPosition = transform.position;
        _player = GameObject.FindGameObjectWithTag("Player");
        _health = _maxHealth;
    }

    private void Update()
    {
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

        _patrolTarget = isNearStart ? GetRandomPatrolPoint() : _startPosition;
        _patrolTimer = 0f;

        GameLogger.Log($"{_state} -> Patrol ({(isNearStart ? "랜덤 지점" : "처음 자리")})");
        _state = ZombieState.Patrol;
    }

    // 처음 자리에서 순찰 거리만큼 떨어진 원 위의 랜덤 지점
    private Vector3 GetRandomPatrolPoint()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        if (randomDirection == Vector2.zero)
        {
            randomDirection = Vector2.right;
        }

        return _startPosition + new Vector3(randomDirection.x, 0f, randomDirection.y) * _patrolDistance;
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

        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _patrolSpeed);
    }

    private void Trace()
    {
        // Todo. Run 애니메이션 실행 

        if (Vector3.Distance(transform.position, _startPosition) > _comebackDistance)
        {
            GameLogger.Log($"{_state} -> Comeback");
            _state = ZombieState.Comeback;
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

        Vector3 direction = _player.transform.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    private void Comback()
    {
        if (Vector3.Distance(transform.position, _startPosition) < 1f)
        {
            GameLogger.Log($"{_state} -> Idle");
            _state = ZombieState.Idle;
            return;
        }

        Vector3 direction = _startPosition - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    private void Attack()
    {
        // 플레이어가 공격 사정 거리를 벗어나면 
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            GameLogger.Log($"{_state} -> Trace ");
            _state = ZombieState.Trace;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackInterval)
        {
            // Todo. 공격 애니메이션 실행
            // Todo. 플레이러르 때려 !
            //GameLogger.Log("플레이어를 때렸습니다.");
            _attackTimer = 0f;
        }
    }

    public void TakeDamage(float damage)
    {
        if (_state == ZombieState.Death)
        {
            return;
        }

        _health -= damage;

        StopAllCoroutines();

        if (_health > 0f)
        {
            StartCoroutine(Hit_Coroutine());
        }
        else
        {
            StartCoroutine(Death_Coroutine());
        }
    }

    private IEnumerator Hit_Coroutine()
    {
        GameLogger.Log($"{_state} -> Hit ");
        _state = ZombieState.Hit; // 잠깐 멈칫 한다.

        yield return new WaitForSeconds(0.3f);

        _state = ZombieState.Trace;
    }

    private IEnumerator Death_Coroutine()
    {
        GameLogger.Log($"{_state} -> Death ");
        _state = ZombieState.Death; // 사망.

        yield return new WaitForSeconds(0.5f);
    }
}