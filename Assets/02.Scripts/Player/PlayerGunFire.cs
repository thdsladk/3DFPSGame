using System;
using System.Collections;
using UnityEngine;

// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;

    [Header("Stat")]
    [SerializeField] private float _range = 100f; // 사거리

    [SerializeField] private float _damage = 10f; // 공격력
    [SerializeField] private float _fireInterval = 0.1f; // 발사 간격
    [SerializeField] private float _reloadInterval = 0.5f; // 장전 간격 (한 발 넣는 데 걸리는 시간)
    [SerializeField] private int _maxAmmo = 50; // 탄창 크기
    [SerializeField] private int _ammo = 0;

    [Header("레이저")]
    [SerializeField] private LineRenderer _laserLine; // 총을 쏠 때 총구에서 맞은 곳까지 그릴 선

    [SerializeField] private Transform _muzzle; // 레이저가 시작되는 총구 (비우면 카메라 조금 아래에서 시작)
    [SerializeField] private float _laserDuration = 0.05f; // 레이저가 보이는 시간 (초)

    private const float LaserFallbackDropHeight = 0.2f; // 총구가 없을 때 카메라보다 이만큼 아래에서 레이저를 시작한다

    private float _fireTimer = 0f;
    private Coroutine _reloadCoroutine; // 장전 중이면 실행 중인 코루틴, 아니면 null
    private Coroutine _laserCoroutine; // 레이저를 보이고 있으면 실행 중인 코루틴, 아니면 null
    private WaitForSeconds _laserWait; // 쏠 때마다 새로 만들지 않도록 재사용한다

    public bool IsReloading => _reloadCoroutine != null;
    public int Ammo => _ammo;
    public int MaxAmmo => _maxAmmo;

    // 총알 수나 장전 여부가 바뀔 때마다 알린다. (UI_Ammo 가 구독)
    public event Action OnAmmoChanged;

    private void Awake()
    {
        _ammo = _maxAmmo;

        // 레이저는 점 두 개(총구, 맞은 곳)로 그리고, 쏘기 전에는 숨겨 둔다.
        _laserWait = new WaitForSeconds(_laserDuration);
        if (_laserLine != null)
        {
            _laserLine.positionCount = 2;
            _laserLine.useWorldSpace = true;
            _laserLine.enabled = false;
        }
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;

        // Alt로 커서를 풀어 UI 버튼을 누르는 중이면 쏘거나 재장전하지 않는다.
        if (CursorManager.IsCursorFree) return;

        // 1. 재장전 입력 받기
        if (Input.GetButtonDown("Reload") && _reloadCoroutine == null)
        {
            StartReload();
        }

        // 2. 마우스 왼쪽 버튼 입력 받기
        if (Input.GetButton("Fire1") && _fireTimer <= 0f && (_ammo > 0))
        {
            // 발사하면 재장전 꺼짐
            StopReload();

            _fireTimer = _fireInterval;
            Shoot();
        }
    }

    // 재장전을 시작한다. 이미 장전 중이거나 탄창이 가득 차 있으면 무시한다.
    private void StartReload()
    {
        if (IsReloading || _ammo >= _maxAmmo) return;

        _reloadCoroutine = StartCoroutine(Reload_Coroutine());
        OnAmmoChanged?.Invoke();
    }

    // 장전 중이면 멈춘다. 그때까지 넣은 총알은 그대로 남는다.
    private void StopReload()
    {
        if (IsReloading == false) return;

        StopCoroutine(_reloadCoroutine);
        _reloadCoroutine = null;
        OnAmmoChanged?.Invoke();
    }

    // 장전 간격마다 한 발씩 넣고, 탄창이 가득 차면 끝난다.
    private IEnumerator Reload_Coroutine()
    {
        var wait = new WaitForSeconds(_reloadInterval);

        while (_ammo < _maxAmmo)
        {
            yield return wait;

            _ammo++;
            GameLogger.Log($"Ammo: {_ammo}");
            OnAmmoChanged?.Invoke();
        }

        _reloadCoroutine = null;
        OnAmmoChanged?.Invoke(); // 장전이 끝났음을 알린다
    }

    private void Shoot()
    {
        // Ray: 레이저 ( 시작위치, 날아갈 방향 )
        // Pysics.RayCast : 레이저를 발사
        // RayCastHit : 레이저가 물체와 부딛히면 그 정보를 저장하는 구조체

        // 1. 총알 소모
        _ammo--;
        GameLogger.Log($"Ammo: {_ammo}");
        OnAmmoChanged?.Invoke();

        // 2. 레이(광선)를 생성하고 발사될 위치와 진행 방향 설정하기
        Transform cameraTransform = Camera.main.transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        // 3. 레이가 부딛힌 대상의 정보를 저장하기
        RaycastHit hitInfo = new RaycastHit();

        // 4. 레이를 발사하고, 만일 부딛힌 물체가 있다면 이펙트 표시하기
        bool isHit = Physics.Raycast(ray, out hitInfo, _range);
        if (isHit)
        {
            //GameLogger.Log($"{hitInfo.collider.name}이 총알을 맞았다.");

            ParticleSystem effect = Instantiate(_hitEffectPrefab);
            effect.transform.position = hitInfo.point; //  맞은 위치
            effect.transform.forward = hitInfo.normal; //  수직 위치로 회전
            effect.Play();

            // 맞은 대상이 좀비라면 TakeDamage
            //if (hitInfo.collider.TryGetComponent(out Zombie zombie))
            //{
            //    zombie.TakeDamage(_damage);
            //}
            DamageInfo damageInfo = new DamageInfo()
            {
                Value = _damage,
                HitPoint = hitInfo.point,
                HitNormal = hitInfo.normal,
            };
            // 좀비 코처럼 자식 콜라이더에 맞아도 대미지가 들어가도록 부모까지 찾는다.
            hitInfo.collider.GetComponentInParent<IDamageable>()?.TakeDamage(damageInfo);
        }

        // 5. 총구에서 맞은 곳까지(안 맞았으면 사거리 끝까지) 레이저를 잠깐 그린다.
        Vector3 laserEnd = isHit ? hitInfo.point : ray.origin + ray.direction * _range;
        ShowLaser(laserEnd);

        // 6. 총알이 없으면 재장전 자동으로
        if (_ammo == 0)
        {
            StartReload();
        }
    }

    private void ShowLaser(Vector3 end)
    {
        if (_laserLine == null) return;

        Transform cameraTransform = Camera.main.transform;
        Vector3 start = _muzzle != null
            ? _muzzle.position
            : cameraTransform.position - cameraTransform.up * LaserFallbackDropHeight;

        // 연사 중이면 이전 레이저를 끄는 코루틴을 멈추고 새로 그린다.
        if (_laserCoroutine != null)
        {
            StopCoroutine(_laserCoroutine);
        }

        _laserCoroutine = StartCoroutine(Laser_Coroutine(start, end));
    }

    private IEnumerator Laser_Coroutine(Vector3 start, Vector3 end)
    {
        // 1. 선의 두 점을 정하고 보인다.
        _laserLine.SetPosition(0, start);
        _laserLine.SetPosition(1, end);
        _laserLine.enabled = true;

        // 2. 잠깐 뒤 숨긴다.
        yield return _laserWait;

        _laserLine.enabled = false;
        _laserCoroutine = null;
    }
}