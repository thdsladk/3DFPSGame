using UnityEngine;

// 역할 : 플레이어의 스태미나를 관리한다. (회복, 소모 판정)
// 달리기(PlayerMove)와 점프(PlayerJump)가 함께 사용한다.
public class PlayerStamina : MonoBehaviour
{
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _recoveryPerSecond = 15f;

    public float Current { get; private set; }
    public float Max => _maxStamina;

    private void Awake()
    {
        Current = _maxStamina;
    }

    private void Update()
    {
        // 매 프레임 조금씩 회복한다.
        Current = Mathf.Min(_maxStamina, Current + _recoveryPerSecond * Time.deltaTime);
    }

    public bool HasEnough(float amount)
    {
        return Current >= amount;
    }

    // 충분하면 소모하고 true, 부족하면 소모하지 않고 false 를 돌려준다.
    public bool TryConsume(float amount)
    {
        if (HasEnough(amount) == false) return false;

        Current -= amount;
        return true;
    }
}
