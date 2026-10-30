using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 역할 : 지금 든 무기(Weapon)의 스탯으로 바라보는 방향에 총을 쏘고, 무기마다 따로 총알을 장전한다.
// 방아쇠는 좌클릭 행동(ShootAction)이 당기고 놓으며, 실제 발사는 Player 의 발사 상태가 애니메이션 이벤트로 TryShoot 을 불러서 한다.
// 산탄총은 여러 발을 퍼뜨려 쏘고, 대포는 맞은 곳에서 터져 범위 대미지를 준다.
public class PlayerGunFire : MonoBehaviour
{
    [SerializeField] private ParticleSystem _hitEffectPrefab;

    [Header("폭발 (대포)")]
    [SerializeField] private ParticleSystem _explosionEffectPrefab; // 터지는 무기가 맞은 곳에 낼 이펙트

    [SerializeField] private float _explosionEffectScale = 1f; // 폭발 이펙트 크기 배율
    [SerializeField] private LayerMask _explosionMask; // 폭발 범위 대미지를 받을 레이어 (Drum, Zombie)

    [Header("총구 섬광")]
    [SerializeField] private GameObject[] _muzzleFlashes;

    [SerializeField] private float _flashTime = 0.05f;

    [Header("레이저")]
    [SerializeField] private LineRenderer _laserLine; // 총을 쏠 때 총구에서 맞은 곳까지 그릴 선

    [SerializeField] private Transform _muzzle; // 레이저가 시작되는 총구 (무기를 바꾸면 그 무기의 총구로 옮긴다, 비우면 카메라 조금 아래에서 시작)
    [SerializeField] private float _laserDuration = 0.15f; // 총선 흔적이 서서히 사라지는 데 걸리는 시간 (초)

    private const float LaserFallbackDropHeight = 0.2f; // 총구가 없을 때 카메라보다 이만큼 아래에서 레이저를 시작한다
    private const int MaxExplosionHitCount = 32; // 한 번 터질 때 판정할 최대 콜라이더 수

    private readonly Collider[] _explosionBuffer = new Collider[MaxExplosionHitCount]; // 매번 새 배열을 만들지 않도록 재사용한다
    private readonly HashSet<IDamageable> _damagedTargets = new HashSet<IDamageable>(); // 콜라이더가 여러 개인 대상이 두 번 맞지 않게
    private Weapon _weapon; // 지금 든 무기 (스탯과 남은 총알)
    private bool _isTriggerHeld = false; // 방아쇠를 당기고 있는지 (좌클릭 행동이 정한다)
    private Coroutine _reloadCoroutine; // 장전 중이면 실행 중인 코루틴, 아니면 null
    private Coroutine _laserCoroutine; // 레이저를 보이고 있으면 실행 중인 코루틴, 아니면 null
    private Color _laserStartColor; // 처음 색 (흐려졌다가 다음 발사에서 되돌릴 때 쓴다)
    private Color _laserEndColor;

    public bool IsReloading => _reloadCoroutine != null;
    public int Ammo => _weapon != null ? _weapon.Ammo : 0;
    public int MaxAmmo => _weapon != null ? _weapon.Data.MaxAmmo : 0;
    public float FireInterval => _weapon != null ? _weapon.Data.FireInterval : 0f;
    public int ShotCount { get; private set; } // 지금까지 쏜 횟수 (발사 애니메이션이 이벤트로 제대로 쐈는지 확인할 때 쓴다)

    // 쏘고 싶은 상태인지. 방아쇠를 당기고 있고, 총알이 있고, 게임 중이며, Alt로 커서를 푼 상태가 아니어야 한다.
    public bool WantsToFire =>
        GameManager.Instance.State == GameState.Playing
        && CursorManager.IsCursorFree == false
        && _isTriggerHeld
        && Ammo > 0;

    // 총알 수나 장전 여부가 바뀔 때마다 알린다. (UI_Ammo 가 구독)
    public event Action OnAmmoChanged;

    private void Awake()
    {
        // 레이저는 점 두 개(총구, 맞은 곳)로 그리고, 쏘기 전에는 숨겨 둔다.
        if (_laserLine != null)
        {
            _laserLine.positionCount = 2;
            _laserLine.useWorldSpace = true;
            _laserLine.enabled = false;
            _laserStartColor = _laserLine.startColor;
            _laserEndColor = _laserLine.endColor;
        }
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // Alt로 커서를 풀어 UI 버튼을 누르는 중이면 재장전하지 않는다.
        if (CursorManager.IsCursorFree) return;

        // 재장전 입력 받기. (발사는 Player 의 발사 상태가 애니메이션 이벤트로 TryShoot 을 불러서 한다)
        if (Input.GetButtonDown("Reload") && _reloadCoroutine == null)
        {
            StartReload();
        }
    }

    // 무기를 바꿔 든다. 하던 장전과 방아쇠는 멈추고, 총구를 새 무기의 총구로 옮긴다.
    public void Equip(Weapon weapon)
    {
        // 1. 이전 무기의 장전·방아쇠를 멈춘다. (장전은 무기마다 따로 해야 하므로)
        StopReload();
        _isTriggerHeld = false;

        // 2. 새 무기로 바꾸고, 총구(섬광이 붙어 있다)를 새 무기의 총구에 붙인다.
        _weapon = weapon;
        if (_muzzle != null && weapon.Data.Muzzle != null)
        {
            _muzzle.SetParent(weapon.Data.Muzzle, false);
            _muzzle.localPosition = Vector3.zero;
            _muzzle.localRotation = Quaternion.identity;
        }

        OnAmmoChanged?.Invoke();

        // 3. 다 쏘고 바꿔 둔 총을 다시 들면 자동으로 장전한다. (총이 아닌 무기는 StartReload 가 무시한다)
        if (Ammo == 0)
        {
            StartReload();
        }
    }

    // 좌클릭 행동(ShootAction)이 방아쇠를 당기고 놓는다.
    public void PullTrigger() => _isTriggerHeld = true;
    public void ReleaseTrigger() => _isTriggerHeld = false;

    // 총알이 있으면 한 발 쏜다. 발사 애니메이션의 총을 쏘는 프레임에서 불린다. (발사 간격은 애니메이션 재생 속도가 정한다)
    public void TryShoot()
    {
        if (Ammo <= 0) return;

        // 발사하면 재장전 꺼짐
        StopReload();
        Shoot();
    }

    // 재장전을 시작한다. 이미 장전 중이거나 탄창이 가득 차 있으면(총이 아닌 무기도) 무시한다.
    private void StartReload()
    {
        if (IsReloading || _weapon == null || _weapon.IsAmmoFull) return;

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

    // 무기의 장전 방식대로 장전하고, 탄창이 가득 차면 끝난다.
    private IEnumerator Reload_Coroutine()
    {
        var wait = new WaitForSeconds(_weapon.Data.ReloadTime);

        if (_weapon.Data.ReloadType == ReloadType.Magazine)
        {
            // 탄창식 : 장전 시간이 다 지나야 한 번에 가득 채운다. (중간에 쏘면 한 발도 들어가지 않는다)
            yield return wait;

            _weapon.FillAmmo();
        }
        else
        {
            // 한 발씩 : 장전 시간마다 한 발씩 넣는다.
            while (_weapon.IsAmmoFull == false)
            {
                yield return wait;

                _weapon.AddAmmo();
                OnAmmoChanged?.Invoke();
            }
        }

        _reloadCoroutine = null;
        OnAmmoChanged?.Invoke(); // 장전이 끝났음을 알린다
    }

    private void Shoot()
    {
        // 1. 총알 소모
        if (_weapon.TryUseAmmo() == false) return;

        ShotCount++;
        OnAmmoChanged?.Invoke();
        StartCoroutine(MuzzleFlash_Coroutine());

        // 2. 한 번에 나가는 탄 수만큼(산탄총은 여러 발) 퍼뜨려 쏜다. 총선 흔적은 첫 탄만 그린다.
        WeaponData data = _weapon.Data;
        Transform cameraTransform = Camera.main.transform;
        for (int i = 0; i < data.PelletCount; i++)
        {
            Vector3 direction = GetSpreadDirection(cameraTransform, data.SpreadAngle);
            Vector3 end = FireRay(new Ray(cameraTransform.position, direction), data);
            if (i == 0) ShowLaser(end);
        }

        // 3. 총알이 없으면 재장전 자동으로
        if (_weapon.Ammo == 0)
        {
            StartReload();
        }
    }

    // 카메라 정면에서 최대 퍼짐 각도 안의 아무 방향을 고른다. (각도가 0 이면 정면)
    private Vector3 GetSpreadDirection(Transform cameraTransform, float spreadAngle)
    {
        if (spreadAngle <= 0f) return cameraTransform.forward;

        Quaternion tilt = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, spreadAngle), cameraTransform.right);
        Quaternion roll = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), cameraTransform.forward);
        return roll * tilt * cameraTransform.forward;
    }

    // 레이 한 줄을 쏘아 맞은 대상에 대미지를 주고, 레이저가 끝날 위치(맞은 곳 또는 사거리 끝)를 돌려준다.
    private Vector3 FireRay(Ray ray, WeaponData data)
    {
        if (Physics.Raycast(ray, out RaycastHit hitInfo, data.Range) == false)
        {
            return ray.origin + ray.direction * data.Range;
        }

        // 터지는 무기(대포)는 맞은 곳 주변에 범위 대미지를, 나머지는 맞은 대상에게만 대미지를 준다.
        if (data.ExplosionRadius > 0f)
        {
            Explode(hitInfo.point, data);
        }
        else
        {
            ParticleSystem effect = Instantiate(_hitEffectPrefab);
            effect.transform.position = hitInfo.point; //  맞은 위치
            effect.transform.forward = hitInfo.normal; //  수직 위치로 회전
            effect.Play();

            AttackAction(hitInfo, data.Damage);
        }

        return hitInfo.point;
    }

    public void AttackAction(RaycastHit hitInfo, float damage)
    {
        DamageInfo damageInfo = new DamageInfo()
        {
            Value = damage,
            HitPoint = hitInfo.point,
            HitNormal = hitInfo.normal,
        };
        // 좀비 코처럼 자식 콜라이더에 맞아도 대미지가 들어가도록 부모까지 찾는다.
        hitInfo.collider.GetComponentInParent<IDamageable>()?.TakeDamage(damageInfo);
    }

    // 맞은 곳에서 터져 범위 안 대상마다 한 번씩 대미지를 준다.
    private void Explode(Vector3 center, WeaponData data)
    {
        // 1. 폭발 이펙트를 낸다.
        if (_explosionEffectPrefab != null)
        {
            ParticleSystem effect = Instantiate(_explosionEffectPrefab, center, Quaternion.identity);
            effect.transform.localScale = Vector3.one * _explosionEffectScale;
            effect.Play();
        }

        // 2. 범위 안의 대상을 찾아 한 번씩만 대미지를 준다.
        int count = Physics.OverlapSphereNonAlloc(center, data.ExplosionRadius, _explosionBuffer, _explosionMask);
        _damagedTargets.Clear();
        for (int i = 0; i < count; i++)
        {
            Collider hit = _explosionBuffer[i];
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || _damagedTargets.Add(target) == false) continue;

            Vector3 hitPoint = hit.ClosestPoint(center);
            DamageInfo damageInfo = new DamageInfo
            {
                Value = data.Damage,
                HitPoint = hitPoint,
                HitNormal = (center - hitPoint).normalized,
            };
            target.TakeDamage(damageInfo);
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

        // 2. 보이는 시간 동안 점점 투명해지며 흔적이 사라진다.
        float elapsed = 0f;
        while (elapsed < _laserDuration)
        {
            float alpha = 1f - elapsed / _laserDuration;
            _laserLine.startColor = new Color(_laserStartColor.r, _laserStartColor.g, _laserStartColor.b, _laserStartColor.a * alpha);
            _laserLine.endColor = new Color(_laserEndColor.r, _laserEndColor.g, _laserEndColor.b, _laserEndColor.a * alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3. 다 사라지면 숨긴다.
        _laserLine.enabled = false;
        _laserCoroutine = null;
    }

    private IEnumerator MuzzleFlash_Coroutine()
    {
        // 플래시를 연결하지 않았으면 건너뛴다. (빈 배열에서 꺼내면 예외가 나 발사가 중간에 끊긴다)
        if (_muzzleFlashes.Length == 0) yield break;

        int randomIndex = UnityEngine.Random.Range(0, _muzzleFlashes.Length);
        GameObject flash = _muzzleFlashes[randomIndex];
        flash.SetActive(true);

        yield return new WaitForSeconds(_flashTime);

        flash.SetActive(false);
    }
}
