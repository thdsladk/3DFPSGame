using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// 마우스 왼쪽 버튼을 누르면 바라보는 방향으로 총알을 발사하고 싶다.
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;

    private int MaxAmmo = 50;

    [Header("Stat")]
    [SerializeField] private float _range = 100f; // 사거리

    [SerializeField] private float _damage = 10f; // 공격력
    [SerializeField] private float _fireInterval = 0.1f; // 발사 간격
    [SerializeField] private float _reloadInterval = 0.5f; // 장전 간격 (한 발 넣는 데 걸리는 시간)
    [SerializeField] private int _ammo = 0;

    private float _fireTimer = 0f;
    private Coroutine _reloadCoroutine; // 장전 중이면 실행 중인 코루틴, 아니면 null

    public bool IsReloading => _reloadCoroutine != null;

    private void Awake()
    {
        _ammo = MaxAmmo;
    }

    private void Update()
    {
        // 0. 발사 타이머를 지난 시간만큼 줄인다.
        _fireTimer -= Time.deltaTime;

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
        if (IsReloading || _ammo >= MaxAmmo) return;

        _reloadCoroutine = StartCoroutine(Reload_Coroutine());
    }

    // 장전 중이면 멈춘다. 그때까지 넣은 총알은 그대로 남는다.
    private void StopReload()
    {
        if (IsReloading == false) return;

        StopCoroutine(_reloadCoroutine);
        _reloadCoroutine = null;
    }

    // 장전 간격마다 한 발씩 넣고, 탄창이 가득 차면 끝난다.
    private IEnumerator Reload_Coroutine()
    {
        var wait = new WaitForSeconds(_reloadInterval);

        while (_ammo < MaxAmmo)
        {
            yield return wait;

            _ammo++;
            GameLogger.Log($"Ammo: {_ammo}");
        }

        _reloadCoroutine = null;
    }

    private void Shoot()
    {
        // Ray: 레이저 ( 시작위치, 날아갈 방향 )
        // Pysics.RayCast : 레이저를 발사
        // RayCastHit : 레이저가 물체와 부딛히면 그 정보를 저장하는 구조체

        // 1. 총알 소모
        _ammo--;
        GameLogger.Log($"Ammo: {_ammo}");

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
            hitInfo.collider.GetComponent<Zombie>()?.TakeDamage(_damage);
        }

        // 5. 총알이 없으면 재장전 자동으로
        if (_ammo == 0)
        {
            StartReload();
        }
    }
}