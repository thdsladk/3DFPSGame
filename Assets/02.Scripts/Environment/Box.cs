using UnityEngine;

// 역할 : 공격받으면 산산조각 나듯 부서지는 상자
public class Box : MonoBehaviour, IDamageable
{
    [SerializeField] private GameObject _brokenBoxPrefab; // 조각난 상자 프리팹

    [Header("스탯")]
    [SerializeField] private float _health = 30f;       // 체력
    [SerializeField] private float _breakForce = 300f;  // 조각을 밀어내는 힘
    [SerializeField] private float _breakRadius = 2f;   // 조각을 밀어내는 반경
    [SerializeField] private float _pieceLifeTime = 3f; // 조각이 사라지기까지 (초)

    // 대미지를 받는다. 이미 부서졌으면(체력 0 이하) 받지 않는다.
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f)
        {
            return;
        }

        _health -= damageInfo.Value;

        if (_health <= 0f)
        {
            Break(damageInfo);
        }
    }

    private void Break(DamageInfo damageInfo)
    {
        // 1. 같은 자리에 조각난 상자를 만든다.
        GameObject brokenBox = Instantiate(_brokenBoxPrefab, transform.position, transform.rotation);

        // 2. 조각마다 맞은 자리에서 바깥으로 밀려나는 힘을 준다.
        foreach (Rigidbody piece in brokenBox.GetComponentsInChildren<Rigidbody>())
        {
            piece.AddExplosionForce(_breakForce, damageInfo.HitPoint, _breakRadius);
        }

        // 3. 조각은 잠시 뒤 사라지고, 원래 상자는 바로 사라진다.
        Destroy(brokenBox, _pieceLifeTime);
        Destroy(gameObject);
    }
}
