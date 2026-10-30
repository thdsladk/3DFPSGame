using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

// FSM(Finite State Machine/유한 상태 머신) 유한한 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법
// - 상태 : 객체가 가질 수 있는 어떤 조건이나 상황
// - 전이 : 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신"

// 상태 패턴 : 상태마다 클래스(IZombieState)를 하나씩 만들어 그 상태의 행동을 맡긴다. (ZombieStates 폴더)
// Zombie 는 상태들이 함께 쓰는 값·컴포넌트·도우미 함수를 들고, 지금 상태를 바꿔 주는 역할(문맥, Context)만 한다.

// 좀비가 가질 수 있는 상태
public enum ZombieState
{
    Idle, // 대기 : 제자리에서 플레이어를 기다린다.
    Patrol, // 순찰 : 처음 자리와 주변의 랜덤 지점을 오간다.
    Trace, // 추격 : 플레이어를 쫓아간다.
    Comeback, // 복귀 : 처음 자리로 돌아간다.
    Jump, // 점프 : 추격 중 길이 끊긴 곳을 뛰어서 건넌다.
    Attack, // 공격 : 일정 시간마다 플레이어를 때린다.
    Hit, // 피격 : 공격(총알, 폭탄)을 받아 잠깐 멈칫한다.
    Death // 죽음 : 쓰러지고 잠시 뒤 사라진다.
}

[RequireComponent(typeof(NavMeshAgent))]
public partial class Zombie : MonoBehaviour, IDamageable
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
    [SerializeField] private float _deathRemoveDelay = 2f; // 죽고 나서 사라지기까지의 시간

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

    [Header("Hit")]
    [SerializeField] private float _hitDuration = 0.3f; // 맞고 멈칫하는 시간 (Hit 애니메이션 길이에 맞춘다)

    [SerializeField] private float _hitRecoverBlendTime = 0.1f; // Hit 가 끝나고 원래 애니메이션으로 섞여 돌아가는 시간

    [Header("Jump")]
    [SerializeField] private float _maxJumpHeight = 1.5f; // 이 높이까지 올라가는 점프만 고른다

    [SerializeField] private float _maxDropHeight = 3f; // 이 높이까지 내려가는 점프만 고른다 (Agent 의 Drop Height 와 맞춤)
    [SerializeField] private float _maxJumpDistance = 4f; // 수평으로 이 거리까지 착지 후보를 찾는다
    [SerializeField] private float _jumpCandidateStep = 0.5f; // 착지 후보를 찍는 간격
    [SerializeField] private float _jumpSpeed = 6f; // 점프 이동 속도 (점프 시간 = 거리 / 속도)
    [SerializeField] private float _jumpArcHeight = 0.5f; // 출발·착지 중 높은 쪽보다 추가로 솟는 높이
    [SerializeField] private float _jumpCheckInterval = 0.3f; // 끊긴 길에서 점프할 수 있는지 다시 확인하는 간격
    [SerializeField] private float _jumpCooldown = 1f; // 착지 후 다시 뛰기까지 기다리는 시간
    [SerializeField] private float _jumpLandBlendTime = 0.1f; // 착지 후 Move 애니메이션으로 섞여 돌아가는 시간
    [SerializeField] private LayerMask _jumpSurfaceMask = 1; // 착지 바닥을 찾고 벽 막힘을 확인할 레이어 (기본 : Default)

    private const float MinJumpDuration = 0.3f; // 아주 가까운 점프도 이 시간보다는 길게 뛴다
    private const float JumpRayStartOffset = 0.1f; // 착지 바닥을 찾는 레이를 점프 높이보다 살짝 위에서 쏜다
    private const float LandingSampleRadius = 0.5f; // 레이가 맞은 바닥 근처에 NavMesh 가 있는지 찾는 반경

    // 애니메이터 트리거. 문자열로 매번 찾지 않도록 해시로 한 번만 바꿔 둔다.
    private static readonly int IdleToMoveHash = Animator.StringToHash("IdleToMove");
    private static readonly int MoveToIdleHash = Animator.StringToHash("MoveToIdle");
    private static readonly int MoveToAttackDelayHash = Animator.StringToHash("MoveToAttackDelay");
    private static readonly int StartAttackHash = Animator.StringToHash("StartAttack");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    // Hit·점프가 끝나고 CrossFade 로 돌아갈 애니메이터 상태 (레이어·서브 스테이트 머신까지 포함한 전체 경로)
    private static readonly int IdleStateHash = Animator.StringToHash("Base Layer.Idle");
    private static readonly int MoveStateHash = Animator.StringToHash("Base Layer.Move");
    private static readonly int AttackDelayStateHash = Animator.StringToHash("Base Layer.AttackDelay");

    private NavMeshPath _path; // 경로 계산용. 매번 새로 만들지 않도록 하나를 재사용한다.

    private float _nextJumpCheckTime = 0f; // 이 시간이 지나야 다시 점프를 확인한다 (확인 간격·착지 후 쿨다운)
    private Vector3 _jumpLanding; // 점프할 착지 지점 (NavMesh 위)
    private Vector3 _patrolTarget;
    private Vector3 _startPosition; // 처음 자리 (발밑, NavMesh 높이 기준)
    private GameObject _player;
    private CharacterController _playerController; // 플레이어 발밑 높이를 구할 때 쓴다
    private NavMeshAgent _agent;
    private CapsuleCollider _collider;
    private Collider[] _colliders; // 몸 캡슐과 머리(HeadShot) 등 이 좀비의 모든 콜라이더 (죽으면 함께 끄고 되살아나면 켠다)
    private Animator _animator;

    // 상태 머신
    private Dictionary<ZombieState, IZombieState> _states;
    private IZombieState _currentState;
    private ZombieState _state = ZombieState.Idle;
    private ZombieState _previousState = ZombieState.Idle; // 바로 전 상태 (Hit 가 돌아갈 곳을 정할 때 쓴다)
    private ZombieState _stateBeforeHit = ZombieState.Idle; // Hit 가 끝나면 돌아갈 상태
    private bool _isFromSpawner = false; // 스포너(풀)가 만든 좀비인지 (스포너가 미리 수를 등록하므로 다시 등록하지 않는다)
    private ObjectPool<Zombie> _pool; // 죽으면 돌아갈 풀 (씬에 미리 놓인 좀비는 없음 → 삭제)

    public float MaxHealth => _maxHealth;
    public float Health => _health;
    public ZombieState State => _state;

    // 체력이 바뀔 때마다 알린다. (체력바가 구독)
    public event Action OnHealthChanged;

    private void Awake()
    {
        // 구독자(체력바)가 Start 에서 처음 값을 읽을 수 있도록 Awake 에서 초기화한다.
        _health = _maxHealth;

        // 컴포넌트는 한 번만 찾아 둔다. (풀에서 다시 꺼낼 때는 Start 가 다시 불리지 않는다)
        _player = GameObject.FindGameObjectWithTag("Player");
        _playerController = _player.GetComponent<CharacterController>();
        _collider = GetComponent<CapsuleCollider>();
        _colliders = GetComponentsInChildren<Collider>(true);
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _path = new NavMeshPath();

        // 상태마다 객체를 하나씩 만들어 둔다.
        _states = new Dictionary<ZombieState, IZombieState>
        {
            { ZombieState.Idle, new IdleState(this) },
            { ZombieState.Patrol, new PatrolState(this) },
            { ZombieState.Trace, new TraceState(this) },
            { ZombieState.Comeback, new ComebackState(this) },
            { ZombieState.Jump, new JumpState(this) },
            { ZombieState.Attack, new AttackState(this) },
            { ZombieState.Hit, new HitState(this) },
            { ZombieState.Death, new DeathState(this) },
        };
    }

    private void Start()
    {
        // 스포너(풀)가 만든 좀비는 스포너가 수를 등록하고 Spawn 으로 준비시킨다.
        if (_isFromSpawner) return;

        // 씬에 미리 놓인 좀비는 스스로 등록하고 준비한다.
        GameManager.Instance.RegisterZombie();
        ResetZombie();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        _currentState?.Update();
    }

    // 지금 상태를 나가고(Exit) 다음 상태로 들어간다(Enter).
    private void ChangeState(ZombieState nextState)
    {
        GameLogger.Log($"[Zombie] {_state} -> {nextState}");

        _currentState?.Exit();

        _previousState = _state;
        _state = nextState;
        _currentState = _states[nextState];

        _currentState.Enter();
    }

    // 스포너가 풀에서 꺼낸 직후 호출한다. 이전에 죽었던 좀비라도 처음 상태로 되돌린다.
    public void Spawn(ObjectPool<Zombie> pool)
    {
        _pool = pool;
        _isFromSpawner = true;
        ResetZombie();
    }

    // 체력·충돌·내비게이션·애니메이션·상태를 처음 상태로 되돌린다.
    private void ResetZombie()
    {
        // 1. 체력과 충돌을 되돌리고, 체력바에 알린다.
        _health = _maxHealth;
        SetCollidersEnabled(true);
        OnHealthChanged?.Invoke();

        // 2. 에이전트를 지금 자리의 NavMesh 위에 올리고 다시 움직일 수 있게 한다. 이 자리가 처음 자리가 된다.
        _agent.Warp(transform.position);
        _agent.updatePosition = true;
        _agent.updateRotation = true;
        _agent.isStopped = false;
        _agent.speed = _moveSpeed;
        _startPosition = GetFeetPosition();
        _nextJumpCheckTime = 0f;

        // 3. 애니메이터를 처음(Idle) 상태로 되돌린다. (죽은 자세 등이 남지 않게)
        _animator.Rebind();
        _animator.Update(0f);

        // 4. 대기 상태에서 시작한다.
        _currentState = null;
        _state = ZombieState.Idle;
        ChangeState(ZombieState.Idle);
    }

    // 죽은 뒤 사라진다. 풀에서 나온 좀비는 풀로 돌아가고, 씬에 놓인 좀비는 삭제한다.
    private void Despawn()
    {
        if (_pool != null)
        {
            _pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 몸과 머리 콜라이더를 함께 켜고 끈다. (죽은 뒤 머리 콜라이더만 허공에 남아 총알을 막지 않게)
    private void SetCollidersEnabled(bool isEnabled)
    {
        foreach (Collider col in _colliders)
        {
            col.enabled = isEnabled;
        }
    }

    private bool IsPlayerWithin(float distance)
    {
        return Vector3.Distance(transform.position, _player.transform.position) <= distance;
    }

    // 다음 순찰 목표를 정한다.
    // 처음 자리 → 랜덤 지점 → 처음 자리 → 새 랜덤 지점 … 순서로 오간다.
    // 처음 자리 근처면 랜덤 지점으로, 아니면 처음 자리로 간다. (복귀 직후에도 헛걸음하지 않는다)
    private void PreparePatrol()
    {
        Vector3 offsetFromStart = transform.position - _startPosition;
        offsetFromStart.y = 0f;
        bool isNearStart = offsetFromStart.magnitude <= _arriveDistance * 2f;

        // 처음 자리로 돌아갈 길이 없으면 (절벽 아래로 내려온 경우 등) 지금 자리를 새 처음 자리로 삼는다.
        if (isNearStart == false && HasCompletePath(GetFeetPosition(), _startPosition) == false)
        {
            SetNewStartPosition();
            isNearStart = true;
        }

        _patrolTarget = isNearStart ? GetRandomPatrolPoint() : _startPosition;
        GameLogger.Log($"[Zombie] 순찰 목표 : {(isNearStart ? "랜덤 지점" : "처음 자리")}");
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

    // 좀비의 발밑 위치. 에이전트는 Base Offset 만큼 바닥보다 위에 있어서,
    // transform 위치로 NavMesh 를 찾으면 그 높이의 상자 윗면이 바닥보다 더 가깝게 잡힌다.
    private Vector3 GetFeetPosition()
    {
        return transform.position - Vector3.up * _agent.baseOffset;
    }

    // 플레이어의 발밑 위치. 플레이어 기준점은 몸 가운데라서 발밑 높이로 내려서 쓴다.
    private Vector3 GetPlayerFeetPosition()
    {
        Vector3 position = _player.transform.position;
        if (_playerController != null)
        {
            position.y = _playerController.bounds.min.y;
        }

        return position;
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
        _startPosition = GetFeetPosition();
        GameLogger.Log($"[Zombie] 처음 자리로 돌아갈 길이 없어 새 처음 자리를 잡음 : {_startPosition}");
    }

    // 플레이어 쪽으로 뛰어서 닿을 수 있는 착지 지점을 찾는다.
    // 점프 높이·낙하 높이·거리 안에 있고, 걸어서는 못 가지만 착지하면 플레이어까지 이어지는 곳만 선택지에 넣는다.
    private bool TryFindJumpLanding(out Vector3 landing)
    {
        landing = Vector3.zero;

        // 0. 지금 자리를 NavMesh 위로 맞추고, 플레이어 쪽 수평 방향을 구한다.
        if (NavMesh.SamplePosition(GetFeetPosition(), out NavMeshHit fromHit, _navMeshSampleRadius, _agent.areaMask) == false)
        {
            return false;
        }

        Vector3 from = fromHit.position;
        Vector3 playerFeet = GetPlayerFeetPosition();
        Vector3 toPlayer = playerFeet - from;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        Vector3 direction = toPlayer.normalized;
        Vector3 head = Vector3.up * _agent.height;
        float rayLength = _maxJumpHeight + _maxDropHeight + JumpRayStartOffset;

        // 후보가 왜 빠졌는지 세어 두었다가, 점프할 곳이 없으면 로그로 알려준다.
        int noGroundCount = 0;
        int noNavMeshCount = 0;
        int tooHighCount = 0;
        int walkableCount = 0;
        int notToPlayerCount = 0;
        int blockedCount = 0;

        // 가까운 곳부터 찍어 보고, 조건을 모두 통과한 첫 지점을 고른다.
        for (float distance = _jumpCandidateStep; distance <= _maxJumpDistance; distance += _jumpCandidateStep)
        {
            // 1. 점프 높이 위에서 아래로 레이를 쏴서 그 자리의 가장 위쪽 바닥을 찾는다. (상자 위가 먼저 잡힌다)
            Vector3 rayStart = from + direction * distance + Vector3.up * (_maxJumpHeight + JumpRayStartOffset);
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, rayLength, _jumpSurfaceMask, QueryTriggerInteraction.Ignore) == false)
            {
                noGroundCount++;
                continue;
            }

            // 2. 그 바닥이 이동 가능 지역(NavMesh)이어야 한다.
            if (NavMesh.SamplePosition(groundHit.point, out NavMeshHit landingHit, LandingSampleRadius, _agent.areaMask) == false)
            {
                noNavMeshCount++;
                continue;
            }

            Vector3 candidate = landingHit.position;

            // 3. 높이 비교 : 점프로 오를 수 있는 높이, 내려갈 수 있는 높이 안이어야 선택지에 들어간다.
            float heightDifference = candidate.y - from.y;
            if (heightDifference > _maxJumpHeight || heightDifference < -_maxDropHeight)
            {
                tooHighCount++;
                continue;
            }

            // 4. 걸어서 갈 수 있는 곳이면 뛸 필요가 없다.
            if (HasCompletePath(from, candidate))
            {
                walkableCount++;
                continue;
            }

            // 5. 착지하면 플레이어까지 걸어서 이어져야 한다.
            if (HasCompletePath(candidate, playerFeet) == false)
            {
                notToPlayerCount++;
                continue;
            }

            // 6. 머리 높이에서 착지점까지 벽에 막히지 않아야 한다.
            if (Physics.Linecast(from + head, candidate + head, _jumpSurfaceMask, QueryTriggerInteraction.Ignore))
            {
                blockedCount++;
                continue;
            }

            landing = candidate;
            return true;
        }

        GameLogger.Log($"[Zombie] 길이 끊겼지만 점프할 곳 없음 - 바닥 없음 {noGroundCount}, NavMesh 없음 {noNavMeshCount}, "
                       + $"높이 초과 {tooHighCount}, 걸어서 갈 수 있음 {walkableCount}, 플레이어와 안 이어짐 {notToPlayerCount}, 벽에 막힘 {blockedCount}");
        return false;
    }

    // 공중에서 점프가 끊겼을 때 내려놓을 자리. 바로 아래 NavMesh 가 없으면 원래 착지점을 쓴다.
    private Vector3 GetGroundBelowOrLanding()
    {
        float searchRadius = _agent.baseOffset + _maxJumpHeight + _maxDropHeight;
        if (NavMesh.SamplePosition(GetFeetPosition(), out NavMeshHit hit, searchRadius, _agent.areaMask))
        {
            return hit.position;
        }

        return _jumpLanding;
    }

    // 에이전트를 NavMesh 위 지점으로 옮기고, 점프 때 꺼 둔 이동을 다시 켠다.
    private void EndJump(Vector3 navMeshPosition)
    {
        // 1. 남아 있는 경로·속도를 지우고 착지 자리로 순간 이동시킨다. (Base Offset 은 에이전트가 알아서 더한다)
        _agent.ResetPath();
        if (_agent.Warp(navMeshPosition) == false)
        {
            GameLogger.LogWarning($"[Zombie] 착지 자리로 옮기지 못함 : {navMeshPosition}");
        }

        _agent.velocity = Vector3.zero;

        // 2. 에이전트가 다시 위치·회전을 맡는다.
        _agent.updatePosition = true;
        _agent.updateRotation = true;
        _agent.isStopped = false;
    }

    private void StopAgent()
    {
        // 절벽 링크를 지나는(뛰어내리는) 중에 멈추면 링크 위에 걸리므로, 착지할 때까지는 멈추지 않는다.
        if (_agent.isOnOffMeshLink) return;

        _agent.ResetPath(); // 에이전트의 목적지 삭제
        _agent.velocity = Vector3.zero; // 미끄럼 방지를 위해서 속도를 0으로 맞춰주기
    }

    // 공격 애니메이션의 타격 프레임에서 ZombieHitEvent 가 호출한다.
    public void AttackAction()
    {
        // Hit 로 넘어가며 공격 모션이 섞이는 동안 이벤트가 불려도, 공격 상태가 아니면 때리지 않는다.
        if (_state != ZombieState.Attack) return;

        DamageInfo damageInfo = new DamageInfo()
        {
            Value = _attackDamage,
            HitPoint = _player.transform.position,
            HitNormal = (transform.position - _player.transform.position).normalized,
        };
        _player.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_state == ZombieState.Death)
        {
            return;
        }

        _health = Mathf.Max(0f, _health - damageInfo.Value);
        OnHealthChanged?.Invoke();

        if (_health > 0f)
        {
            SprayBlood(damageInfo);

            // 점프 중에는 멈칫하지 않고 점프를 끝까지 이어간다.
            if (_state == ZombieState.Jump) return;

            // 진행 중인 상태 코루틴(Hit·점프 등)을 멈추고 피격 상태로 간다.
            StopAllCoroutines();
            ChangeState(ZombieState.Hit);
        }
        else
        {
            // 점프 중이었다면 JumpState.Exit 가 바닥에 내려놓는다.
            StopAllCoroutines();
            ChangeState(ZombieState.Death);
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
}
