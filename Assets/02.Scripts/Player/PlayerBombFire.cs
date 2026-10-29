using System;
using UnityEngine;

public class PlayerBombFire : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrepab;
    [SerializeField] private float _throwPower;
    [SerializeField] private int _ammo = 10;
    [SerializeField] private float _powerGauge = 0f;
    [SerializeField] private int _bombPoolSize = 5; // 미리 만들어 둘 폭탄 수

    [SerializeField]
    private float _holdNormalizedTime = 0.64f; // 버튼을 누르고 있는 동안 던지기 모션을 멈춰 둘 지점 (손을 놓는 ThrowEvent 0.66 바로 앞)

    private const float MaxChagingTime = 2f;
    private const string ThrowLayerName = "ThrowBomb Layer"; // 애니메이터의 레이어 이름과 똑같아야 한다
    private static readonly int ThrowBombHash = Animator.StringToHash("ThrowBomb"); // 던지는 애니메이션 트리거
    private static readonly int ThrowBombStateHash = Animator.StringToHash("ThrowBomb"); // 던지는 애니메이션 상태 이름
    private static readonly int ThrowSpeedHash = Animator.StringToHash("ThrowSpeed"); // 던지는 애니메이션 재생 속도 (0 이면 멈춤)
    private bool _isChaging = false;
    private bool _isThrowing = false; // 던지는 애니메이션이 재생 중이고 아직 손을 놓지 않았는지
    private bool _isHolding = false; // 버튼을 누르고 있어 손을 놓기 직전에서 멈춰 있어야 하는지
    private bool _isReleaseReached = false; // 버튼을 떼기 전에 손을 놓는 프레임(ThrowEvent)을 지나쳤는지
    private float _throwGauge; // 버튼을 뗄 때 모은 게이지 (손을 놓는 순간 이 힘으로 던진다)
    private ObjectPool<Bomb> _bombPool;
    private Animator _animator;
    private int _throwLayerIndex;
    private int _maxAmmo; // 처음 가진 폭탄 수
    private Player _player;

    public int Ammo => _ammo;
    public int MaxAmmo => _maxAmmo;

    // 폭탄 수가 바뀔 때마다 알린다. (UI_Ammo 가 구독)
    public event Action OnAmmoChanged;

    private void Awake()
    {
        // 폭탄은 플레이어를 따라다니지 않도록 부모 없이 만든다.
        _bombPool = new ObjectPool<Bomb>(_bombPrepab, _bombPoolSize);
        _maxAmmo = _ammo;
        _animator = GetComponentInChildren<Animator>();
        _throwLayerIndex = _animator.GetLayerIndex(ThrowLayerName);
        _player = GetComponent<Player>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        if (_isChaging)
        {
            _powerGauge = Mathf.Min(MaxChagingTime, _powerGauge + Time.deltaTime);
            //GameLogger.Log($"PowerGauge : {_powerGauge}");
        }

        // 1. 마우스 오른쪽 버튼 입력 받기 : 누르면 던지기 시작, 떼면 손을 놓는다.
        if (Input.GetButtonDown("Fire2") && _player.WeaponMode == WeaponMode.Normal)
        {
            StartThrow();
        }
        else if (Input.GetButtonUp("Fire2"))
        {
            ReleaseThrow();
        }

        // 2. 누르고 있는 동안에는 손을 놓기 직전에서 모션을 멈춰 둔다.
        if (_isHolding)
        {
            HoldBeforeRelease();
        }
    }

    // 버튼을 누르면 던지는 모션을 시작하고 힘을 모은다.
    private void StartThrow()
    {
        // 이미 던지는 중이면 이번 입력은 무시한다. (트리거가 쌓여 동작이 두 번 재생되지 않게)
        if (_isThrowing) return;

        if (_ammo <= 0)
        {
            GameLogger.Log("폭탄이 없습니다.");
            return;
        }

        // 1. 힘 모으기 시작
        _isChaging = true;
        _powerGauge = 0f;

        // 2. 던지는 애니메이션 재생 (ThrowBomb Layer : Empty → ThrowBomb → Empty)
        _isThrowing = true;
        _isHolding = true;
        _isReleaseReached = false;
        _animator.SetFloat(ThrowSpeedHash, 1f);
        _animator.SetTrigger(ThrowBombHash);
    }

    // 버튼을 떼면 모은 힘을 기억해 두고 멈춰 있던 모션을 이어서 재생한다. 폭탄은 손을 놓는 ThrowEvent 에서 날아간다.
    private void ReleaseThrow()
    {
        if (_isHolding == false) return;

        // 1. 모은 힘을 기억해 두고 게이지를 초기화한다.
        _isChaging = false;
        _isHolding = false;
        _throwGauge = _powerGauge;
        _powerGauge = 0f;

        // 2. 모션을 이어서 재생한다.
        _animator.SetFloat(ThrowSpeedHash, 1f);

        // 3. 프레임이 튀어 누르는 동안 손을 놓는 프레임을 이미 지나쳤다면 바로 던진다.
        if (_isReleaseReached)
        {
            LaunchBomb();
        }
    }

    // 던지기 모션이 멈출 지점에 다다르면 재생 속도를 0 으로 만든다.
    private void HoldBeforeRelease()
    {
        AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(_throwLayerIndex);
        if (info.shortNameHash == ThrowBombStateHash && info.normalizedTime >= _holdNormalizedTime)
        {
            _animator.SetFloat(ThrowSpeedHash, 0f);
        }
    }

    // 던지기 애니메이션에서 손을 놓는 프레임에 PlayerHitEvent 가 호출한다.
    public void ThrowEvent()
    {
        if (_isThrowing == false) return;

        // 아직 버튼을 누르고 있으면 떼는 순간 던지도록 미뤄 둔다.
        if (_isHolding)
        {
            _isReleaseReached = true;
            return;
        }

        LaunchBomb();
    }

    // 실제로 폭탄을 꺼내 날린다.
    private void LaunchBomb()
    {
        _isThrowing = false;
        _isReleaseReached = false;

        // 1. 폭탄 갯수 감소
        _ammo--;
        OnAmmoChanged?.Invoke();

        // 2. 풀에서 수류탄을 꺼내 발사 위치에 둔다.
        Bomb bomb = _bombPool.Get(_firePoint.position, Quaternion.identity);

        // 3. 꺼낸 수류탄을 지금 바라보는 방향으로 던지기
        Vector3 force = Camera.main.transform.forward * (_throwPower * _throwGauge);
        bomb.Launch(_bombPool, force);
    }
}