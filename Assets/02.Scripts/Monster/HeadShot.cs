using UnityEngine;

// 역할 : 좀비 머리 판정. 총알(레이)이 이 콜라이더를 지나가면 대미지를 배율만큼 늘린다. (대미지는 한 번만 준다)
// 머리 콜라이더가 몸 캡슐 안에 들어 있어 레이가 몸에 먼저 맞으므로, 몸에 맞은 레이가 머리도 지나가는지 따로 검사한다.
[RequireComponent(typeof(Collider))]
public class HeadShot : MonoBehaviour
{
    [SerializeField] private float _damageMultiplier = 2f; // 헤드샷 대미지 배율

    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    // 레이가 머리를 지나가면 배율을 곱한 대미지를, 아니면 그대로 돌려준다.
    public float GetDamage(Ray ray, float maxDistance, float damage)
    {
        bool isHeadShot = _collider.enabled && _collider.Raycast(ray, out _, maxDistance);
        if (isHeadShot == false) return damage;

        GameLogger.Log("[HeadShot] 헤드샷!");
        return damage * _damageMultiplier;
    }
}
