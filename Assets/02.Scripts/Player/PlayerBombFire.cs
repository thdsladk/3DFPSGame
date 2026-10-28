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
    private const float MaxChagingTime = 2f;
    private static readonly int ThrowBombHash = Animator.StringToHash("ThrowBomb"); // 던지는 애니메이션 트리거
    private bool _isChaging = false;
    private bool _isThrowing = false; // 던지는 애니메이션이 재생 중이고 아직 손을 놓지 않았는지
    private float _throwGauge; // 버튼을 뗄 때 모은 게이지 (손을 놓는 순간 이 힘으로 던진다)
    private ObjectPool<Bomb> _bombPool;
    private Animator _animator;
    private int _maxAmmo; // 처음 가진 폭탄 수

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
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        if (_isChaging)
        {
            _powerGauge = Mathf.Min(MaxChagingTime, _powerGauge + Time.deltaTime);
            //GameLogger.Log($"PowerGauge : {_powerGauge}");
        }

        // 1. 마우스 오른쪽 버튼 입력 받기
        if (Input.GetButtonDown("Fire2"))
        {
            _isChaging = true;
        }
        else if (Input.GetButtonUp("Fire2"))
        {
            _isChaging = false;
            Shoot();
        }
    }

    // 버튼을 떼면 던지는 애니메이션만 시작한다. 실제로 폭탄이 날아가는 것은 손을 놓는 프레임의 ThrowEvent 에서 한다.
    private void Shoot()
    {
        if (_ammo <= 0)
        {
            GameLogger.Log("폭탄이 없습니다.");
            _powerGauge = 0f;
            return;
        }

        // 이미 던지는 중이면 이번 입력은 무시한다. (트리거가 쌓여 동작이 두 번 재생되지 않게)
        if (_isThrowing)
        {
            _powerGauge = 0f;
            return;
        }

        // 1. 모은 힘을 기억해 두고 게이지를 초기화한다.
        _throwGauge = _powerGauge;
        _powerGauge = 0f;

        // 2. 던지는 애니메이션 재생 (ThrowBomb Layer : Empty → ThrowBomb → Empty)
        _isThrowing = true;
        _animator.SetTrigger(ThrowBombHash);
    }

    // 던지기 애니메이션에서 손을 놓는 프레임에 PlayerHitEvent 가 호출한다.
    public void ThrowEvent()
    {
        if (_isThrowing == false) return;
        _isThrowing = false;

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