using System;
using UnityEngine;

// 역할 : 플레이어의 체력. 맞으면 체력이 줄고, 화면 효과를 내고, 구독자(UI)에게 알린다.
public class Player : MonoBehaviour, IDamageable
{
    [Header("스탯")]
    [SerializeField] private float _maxHealth = 100f;        // 최대 체력
    [SerializeField] private float _dangerHealthRatio = 0.3f; // 이 비율 아래면 위험 상태

    [Header("피격 효과")]
    [SerializeField] private UI_HitEffect _uiHitEffect;  // 맞았을 때 화면 효과
    [SerializeField] private float _hitEffectAlpha = 0.7f; // 번쩍이는 진하기

    private float _health; // 현재 체력

    // UI가 읽어 가는 값 (읽기만 할 수 있다)
    public float MaxHealth => _maxHealth;
    public float Health => _health;

    // Action 자료형 : 함수(들)를 저장할 수 있다. 맞으면 체력바(UI_PlayerStats)에 알린다.
    public event Action OnDamaged;

    private void Awake()
    {
        // 구독자(UI)가 Start 에서 처음 값을 읽을 수 있도록 Awake 에서 최대 체력으로 채운다.
        _health = _maxHealth;
    }

    // 대미지를 받는다.
    public void TakeDamage(DamageInfo damageInfo)
    {
        if (_health <= 0f) return; // 이미 쓰러졌으면 받지 않는다.

        // 1. 체력을 깎는다.
        _health = Mathf.Max(0f, _health - damageInfo.Value);

        // 2. 화면을 붉게 번쩍였다가 서서히 사라지게 한다. 체력이 낮으면 옅게 붉은 채로 남긴다.
        _uiHitEffect.SetDanger(_health < _maxHealth * _dangerHealthRatio);
        _uiHitEffect.Fade(_hitEffectAlpha);

        // 3. 체력바에 알린다.
        OnDamaged?.Invoke();

        GameLogger.Log($"플레이어가 맞았다. 대미지 {damageInfo.Value}, 남은 체력 {_health}");
        if (_health <= 0f)
        {
            GameLogger.Log("플레이어가 쓰러졌다!");
            GameManager.Instance.GameOver();
        }
    }
}
