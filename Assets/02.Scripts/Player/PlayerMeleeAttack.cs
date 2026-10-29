using System.Collections.Generic;
using UnityEngine;

// 역할 : 근접 모드에서 좌클릭하면 쿠크리를 휘두르고, 휘두르는 모션이 앞을 지나는 순간(MeleeEvent) 앞쪽 적에게 대미지를 준다.
// 휘두르는 애니메이션은 Swing Layer (Empty → swing → Empty) 가 맡는다.
public class PlayerMeleeAttack : MonoBehaviour
{
    [SerializeField] private float _damage = 30f; // 한 번 휘두를 때 대미지
    [SerializeField] private float _attackRange = 1.2f; // 몸 앞으로 이만큼 떨어진 곳을 중심으로 판정한다
    [SerializeField] private float _attackRadius = 1f; // 판정 구의 반지름
    [SerializeField] private float _attackHeight = 1f; // 판정 구의 높이 (발 기준)
    [SerializeField] private LayerMask _hitMask; // 맞출 수 있는 레이어 (Zombie, Drum 등)

    private const int MaxHitCount = 16; // 한 번에 판정할 최대 콜라이더 수

    private static readonly int SwingHash = Animator.StringToHash("Swing"); // 휘두르기 애니메이션 트리거

    private readonly Collider[] _hitBuffer = new Collider[MaxHitCount]; // 매번 새 배열을 만들지 않도록 재사용한다
    private readonly HashSet<IDamageable> _damagedTargets = new HashSet<IDamageable>(); // 콜라이더가 여러 개인 대상이 두 번 맞지 않게
    private Player _player;
    private Animator _animator;
    private bool _isAttacking = false; // 휘두르는 중이고 아직 타격 순간이 오지 않았는지

    private void Awake()
    {
        _player = GetComponent<Player>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        _player.OnWeaponModeChanged += CancelAttack;
    }

    private void OnDestroy()
    {
        if (_player != null) _player.OnWeaponModeChanged -= CancelAttack;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        // Alt로 커서를 풀어 UI 버튼을 누르는 중이면 휘두르지 않는다.
        if (CursorManager.IsCursorFree) return;

        if (_player.WeaponMode == WeaponMode.Melee && Input.GetButtonDown("Fire1"))
        {
            StartAttack();
        }
    }

    // 휘두르는 애니메이션을 시작한다. 이미 휘두르는 중이면 무시한다.
    private void StartAttack()
    {
        if (_isAttacking) return;

        _isAttacking = true;
        _animator.SetTrigger(SwingHash);
    }

    // 휘두르기 애니메이션에서 칼이 몸 앞을 지나는 프레임에 PlayerHitEvent 가 호출한다.
    public void MeleeEvent()
    {
        if (_isAttacking == false) return;
        _isAttacking = false;

        // 1. 몸 앞쪽 구 안의 대상을 찾는다.
        Vector3 center = transform.position + transform.forward * _attackRange + Vector3.up * _attackHeight;
        int count = Physics.OverlapSphereNonAlloc(center, _attackRadius, _hitBuffer, _hitMask);

        // 2. 대상마다 한 번씩만 대미지를 준다.
        _damagedTargets.Clear();
        for (int i = 0; i < count; i++)
        {
            Collider hit = _hitBuffer[i];
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || _damagedTargets.Add(target) == false) continue;

            Vector3 hitPoint = hit.ClosestPoint(center);
            DamageInfo damageInfo = new DamageInfo
            {
                Value = _damage,
                HitPoint = hitPoint,
                HitNormal = (center - hitPoint).normalized,
            };
            target.TakeDamage(damageInfo);
        }

        GameLogger.Log($"[PlayerMeleeAttack] 휘두르기 : {_damagedTargets.Count}개 맞춤");
    }

    // 무기를 바꾸면 남은 휘두르기 입력을 지운다. (모드를 바꾼 뒤에 휘두르는 동작이 튀어나오지 않게)
    private void CancelAttack()
    {
        _isAttacking = false;
        _animator.ResetTrigger(SwingHash);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * _attackRange + Vector3.up * _attackHeight, _attackRadius);
    }
}
