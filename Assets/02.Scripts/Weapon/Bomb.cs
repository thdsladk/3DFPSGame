using UnityEngine;

// 역할 : 무언가에 부딪히면 터지면서 주변의 드럼통·좀비에게 범위 대미지를 준다.
public class Bomb : MonoBehaviour
{
    [SerializeField] private ParticleSystem _explosionEffectPrefab;

    [Header("스탯")]
    [SerializeField] private float _explosionRange = 4.5f; // 폭발 범위 (반지름)
    [SerializeField] private float _damage = 100f;         // 폭발 대미지

    private void OnCollisionEnter(Collision collision)
    {
        // 1. 폭발 이펙트를 만든다.
        ParticleSystem effect = Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        // 2. 주위에 대미지를 준다.
        ApplyAreaDamage();

        // Todo. 오브젝트 풀링 적용
        // 3. 폭탄을 삭제한다.
        Destroy(gameObject);
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
