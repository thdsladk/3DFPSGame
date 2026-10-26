using System.Collections;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class Drum : MonoBehaviour, IDamageable
{
    // 체력이 0이 되면 위로 튀어오르며 폭발하는 드럼통
    [SerializeField] private ParticleSystem _explodeEffectPrefab;

    [Header("Stat")]
    [SerializeField] private float _health = 30f;

    [SerializeField] private float _launchPower = 20f;
    [SerializeField] private float _spinPower = 5f;
    [SerializeField] private float _destroyDelay = 3f;
    [SerializeField] private float _explosionRange = 4.5f;
    [SerializeField] private float _explosionDamage = 100f;
    [SerializeField] private float _explosionDelay = 0.1f;
    [SerializeField] private float _effectScale = 10f;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }


    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f)
        {
            return;
        }

        _health -= damageInfo.Value;
        if (_health <= 0f)
        {
            StartCoroutine(Explode_Coroutine());
        }
    }

    private IEnumerator Explode_Coroutine()
    {
        // 0. 폭발 하기 전에 대기 
        yield return new WaitForSeconds(_explosionDelay);

        // 1. 폭발 이펙트를 생성한다. 
        ParticleSystem effect = Instantiate(_explodeEffectPrefab, transform.position, Quaternion.identity);
        effect.transform.localScale = Vector3.one * _effectScale; // 이펙트가 작아서 폭발 범위에 어울리게 키운다.
        effect.Play();

        _rigidbody.AddForce(Vector3.up * _launchPower, ForceMode.Impulse);
        _rigidbody.AddTorque(UnityEngine.Random.insideUnitSphere * _spinPower, ForceMode.Impulse);

        // 3. 주위에 폭발을 전달한다. ( 데미지를 준다. ) 
        ApplyAreaDamage();

        // 3. 딜레이 후 사라진다.
        yield return new WaitForSeconds(_destroyDelay);

        Destroy(gameObject);
    }

    private void ApplyAreaDamage()
    {
        // 0. 주위에 있는 드럼통들을 모두 찾는다. 
        Collider[] colliders =
            Physics.OverlapSphere(transform.position, _explosionRange, LayerMask.GetMask("Drum", "Zombie"));
        // 1. 드럼통·좀비들에게 데미지를 준다.
        foreach (Collider collider in colliders)
        {
            // 폭발 중심에서 가장 가까운 콜라이더 표면 위치와, 그 자리에서 폭발 중심을 향하는 방향
            Vector3 explosionCenter = transform.position;
            Vector3 hitPoint = collider.ClosestPoint(explosionCenter);

            DamageInfo damageInfo = new DamageInfo()
            {
                Value = _explosionDamage,
                HitPoint = hitPoint,
                HitNormal = (explosionCenter - hitPoint).normalized,
            };
            collider.GetComponent<IDamageable>()?.TakeDamage(damageInfo);
        }
    }
}