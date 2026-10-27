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
    private bool _isChaging = false;
    private ObjectPool<Bomb> _bombPool;
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

    private void Shoot()
    {
        if (_ammo > 0)
        {
            // 1. 폭탄 갯수 감소
            _ammo--;
            OnAmmoChanged?.Invoke();
            // 2. 풀에서 수류탄을 꺼내 발사 위치에 둔다.
            Bomb bomb = _bombPool.Get(_firePoint.position, Quaternion.identity);

            // 3. 꺼낸 수류탄을 시선 방향으로 던지기
            Vector3 force = Camera.main.transform.forward * (_throwPower * _powerGauge);
            bomb.Launch(_bombPool, force);

            // 4. 게이지 초기화
            _powerGauge = 0f;
        }
        else
        {
            GameLogger.Log("폭탄이 없습니다.");
        }
    }
}