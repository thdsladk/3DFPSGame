using UnityEngine;

// 역할 : 무언가에 부딪히면 터지면서 주변의 드럼통·좀비에게 범위 대미지를 준다.
// 오브젝트 풀에서 꺼내 쓰고, 터지면 삭제하지 않고 풀에 돌아간다.
public class Bomb : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;

    [Header("스탯")]
    [SerializeField] private float _explosionRange = 4.5f; // 폭발 범위 (반지름)
    [SerializeField] private float _damage = 100f;         // 폭발 대미지

    private Rigidbody _rigidbody;
    private ObjectPool<Bomb> _pool; // 돌아갈 풀 (없으면 터질 때 삭제한다)
    private bool _isExploded = false; // 한 번에 여러 곳과 부딪혀도 한 번만 터지게 한다

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    // 풀에서 꺼낸 직후 호출한다. 이전에 던졌을 때 남은 속도를 지우고 시선 방향으로 던진다.
    public void Launch(ObjectPool<Bomb> pool, Vector3 force)
    {
        _pool = pool;
        _isExploded = false;

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.AddForce(force, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isExploded) return;
        _isExploded = true;

        // 1. 폭발 이펙트를 만든다.
        ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        // 2. 주위에 대미지를 준다.
        ApplyAreaDamage();

        // 3. 폭탄을 풀에 돌려놓는다. (풀 없이 씬에 놓인 폭탄이면 삭제)
        if (_pool != null)
        {
            _pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ApplyAreaDamage()
    {
        Collider[] colliders =
            Physics.OverlapSphere(transform.position, _explosionRange, LayerMask.GetMask("Drum", "Zombie"));

        foreach (Collider col in colliders)
        {
            // 폭발 중심에서 가장 가까운 콜라이더 표면 위치와, 그 자리에서 폭발 중심을 향하는 방향
            Vector3 explosionCenter = transform.position;
            Vector3 hitPoint = col.ClosestPoint(explosionCenter);

            DamageInfo damageInfo = new DamageInfo
            {
                Value = _damage,
                HitPoint = hitPoint,
                HitNormal = (explosionCenter - hitPoint).normalized,
            };

            if (col.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(damageInfo);
            }
        }
    }
}
