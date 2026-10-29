using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어가 가질 수 있는 상태 (우선순위 : Fire > Walk > Idle)
public enum PlayerState
{
    Idle, // 대기 : 가만히 서 있다.
    Walk, // 걷기 : 움직이는 중이다.
    Fire // 발사 : 마우스 왼쪽 버튼을 누르는 동안 쏜다. (이동은 그대로 할 수 있다)
}

// 역할 : 플레이어의 체력과 행동 상태. 맞으면 체력이 줄고, 화면 효과를 내고, 구독자(UI)에게 알린다.
// 상태 패턴 : 상태마다 클래스(IPlayerState)를 하나씩 만들어 그 상태의 행동·전환을 맡긴다. (PlayerStates 폴더)
// Player 는 상태들이 함께 쓰는 값과 도우미 함수, 그리고 상태를 바꿔 주는 역할(문맥, Context)을 한다.
public partial class Player : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _maxHealth = 100f; // 최대 체력

    [SerializeField] private float _dangerHealthRatio = 0.3f; // 이 비율 아래면 위험 상태

    [Header("피격 효과")]
    [SerializeField] private UI_HitEffect _uiHitEffect; // 맞았을 때 화면 효과

    [SerializeField] private float _hitEffectAlpha = 0.7f; // 번쩍이는 진하기

    [Header("애니메이션")]
    [SerializeField] private float _moveThreshold = 0.1f; // 이 속도보다 빠르면 움직이는 중으로 본다

    [SerializeField] private float _moveBlendDamping = 0.1f; // 이동 블렌드 값이 목표에 다다르는 데 걸리는 시간 (초)

    [SerializeField] private float _fireAnimationLength = 0.5f; // 발사 애니메이션 클립 길이 (초)
    [SerializeField] private float _maxFireAnimationSpeed = 5f; // 연사가 빠를 때 발사 애니메이션을 최대 몇 배까지 빠르게 재생할지

    [SerializeField]
    private float _injuredStartHealthRatio = 0.7f; // 체력이 이 비율 아래로 내려가면 다친 모션이 섞이기 시작한다 (위험 비율에서 완전히 다친 모션)

    // 애니메이터 트리거·값. 문자열로 매번 찾지 않도록 해시로 한 번만 바꿔 둔다.
    private static readonly int MoveMotionHash = Animator.StringToHash("MoveMotion");
    private static readonly int IsFireHash = Animator.StringToHash("IsFire");
    private static readonly int FireSpeedHash = Animator.StringToHash("FireSpeed");
    private const string InjuredLayerName = "Injured Layer"; // 애니메이터의 레이어 이름과 똑같아야 한다

    private float _health; // 현재 체력
    private int _injuredLayerIndex;

    // 상태 머신
    private Dictionary<PlayerState, IPlayerState> _states;
    private IPlayerState _currentState;
    private PlayerState _state = PlayerState.Idle;

    private Animator _animator;
    private PlayerMove _playerMove;
    private PlayerGunFire _gunFire;

    private WeaponMode _weaponMode = WeaponMode.Normal;
    public WeaponMode WeaponMode => _weaponMode;

    // UI가 읽어 가는 값 (읽기만 할 수 있다)
    public float MaxHealth => _maxHealth;
    public float Health => _health;
    public PlayerState State => _state;

    // Action 자료형 : 함수(들)를 저장할 수 있다. 맞으면 체력바(UI_PlayerStats)에 알린다.
    public event Action OnDamaged;

    // 움직이는 중인지 (수평 속도로 판단한다)
    private bool IsMoving => _playerMove.HorizontalVelocity.sqrMagnitude > _moveThreshold * _moveThreshold;

    // 쏘고 싶은지 (마우스 왼쪽 버튼을 누르고 있고, 쏠 수 있는 상태)
    private bool WantsToFire => _gunFire.WantsToFire;

    // 발사 애니메이션 한 번에 걸리는 시간. 발사 간격에 맞춰 재생 속도가 정해진다.
    private float FireCycleDuration => _fireAnimationLength / GetFireAnimationSpeed();

    private void Awake()
    {
        // 구독자(UI)가 Start 에서 처음 값을 읽을 수 있도록 Awake 에서 최대 체력으로 채운다.
        _health = _maxHealth;

        _animator = GetComponentInChildren<Animator>();
        _playerMove = GetComponent<PlayerMove>();
        _gunFire = GetComponent<PlayerGunFire>();

        // 캐릭터의 위치는 CharacterController 가 정하므로, 애니메이션이 몸을 움직이지 않게 한다.
        _animator.applyRootMotion = false;

        // 상태마다 객체를 하나씩 만들어 둔다.
        _states = new Dictionary<PlayerState, IPlayerState>
        {
            { PlayerState.Idle, new IdleState(this) },
            { PlayerState.Walk, new WalkState(this) },
            { PlayerState.Fire, new FireState(this) },
        };
    }

    private void Start()
    {
        _currentState = _states[_state];
        _currentState.Enter();
        _animator = GetComponentInChildren<Animator>();
        _injuredLayerIndex = _animator.GetLayerIndex(InjuredLayerName);

        // 컨트롤러에 적어 둔 기본 가중치와 상관없이 처음 체력에 맞게 시작한다.
        UpdateInjuredLayer();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _weaponMode = WeaponMode.Normal;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _weaponMode = WeaponMode.Sniper;
        }

        UpdateMoveAnimation();
        _currentState.Update();
    }

    // 지금 상태를 나가고(Exit) 다음 상태로 들어간다(Enter). 발사 애니메이션 값도 함께 바꾼다.
    private void ChangeState(PlayerState nextState)
    {
        if (_state == nextState) return;

        GameLogger.Log($"[Player] {_state} -> {nextState}");

        _currentState.Exit();

        PlayerState previousState = _state;
        _state = nextState;
        _currentState = _states[nextState];

        PlayAnimation(previousState, nextState);
        _currentState.Enter();
    }

    // 발사 애니메이션은 Fire Layer 가 맡는다. Fire 상태에 있는 동안만 IsFire 를 켠다.
    private void PlayAnimation(PlayerState from, PlayerState to)
    {
        if (from == PlayerState.Fire) _animator.SetBool(IsFireHash, false);
        if (to == PlayerState.Fire) _animator.SetBool(IsFireHash, true);
    }

    // 대기·달리기 애니메이션은 Base Layer 의 블렌드 트리(MoveMotion : 0 = 대기, 1 = 달리기)가 맡는다.
    // 발사 중에도 이동에 맞춰 바뀌도록 상태와 별개로 이동 속도만 따른다.
    private void UpdateMoveAnimation()
    {
        // 1. 평소 이동 속도를 1로 보고 현재 수평 속도를 0~1 로 바꾼다. (그보다 빠른 질주도 1 로 본다)
        float moveMotion = Mathf.Clamp01(_playerMove.HorizontalVelocity.magnitude / _playerMove.MoveSpeed);

        // 2. 값이 갑자기 튀지 않도록 부드럽게 따라가게 한다.
        _animator.SetFloat(MoveMotionHash, moveMotion, _moveBlendDamping, Time.deltaTime);
    }

    // 체력이 줄수록 다친 모션(Injured Layer)을 더 많이 섞는다. 시작 비율에서 0, 위험 비율에서 1 이다.
    private void UpdateInjuredLayer()
    {
        float healthRatio = _health / _maxHealth;
        float injured = Mathf.InverseLerp(_injuredStartHealthRatio, _dangerHealthRatio, healthRatio);
        _animator.SetLayerWeight(_injuredLayerIndex, injured);
    }

    // 발사 애니메이션 재생 속도. 한 번 재생하는 시간이 발사 간격과 같아지도록 맞추고, 너무 빨라지지 않게 제한한다.
    private float GetFireAnimationSpeed()
    {
        float speed = _fireAnimationLength / Mathf.Max(0.01f, _gunFire.FireInterval);
        return Mathf.Clamp(speed, 1f, _maxFireAnimationSpeed);
    }

    private void SetFireAnimationSpeed()
    {
        _animator.SetFloat(FireSpeedHash, GetFireAnimationSpeed());
    }

    // 발사 애니메이션의 총을 쏘는 프레임에서 PlayerHitEvent 가 호출한다.
    public void AttackAction()
    {
        _gunFire.TryShoot();
    }

    // 대미지를 받는다.
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f) return; // 이미 쓰러졌으면 받지 않는다.

        // 1. 체력을 깎는다.
        _health = Mathf.Max(0f, _health - damageInfo.Value);

        // 체력에 따라 Injured Layer의 가중치를 바꿔준다.
        UpdateInjuredLayer();

        // 2. 화면을 붉게 번쩍였다가 서서히 사라지게 한다. 체력이 낮으면 옅게 붉은 채로 남긴다.
        _uiHitEffect.SetDanger(_health < _maxHealth * _dangerHealthRatio);
        _uiHitEffect.Fade(_hitEffectAlpha);

        // 3. 체력바에 알린다.
        OnDamaged?.Invoke();

        GameLogger.Log($"플레이어가 맞았다. 대미지 {damageInfo.Value}, 남은 체력 {_health}");
        if (_health <= 0f)
        {
            GameLogger.Log("플레이어가 쓰러졌다!");
            GameManager.Instance.GameOver();
        }
    }
}