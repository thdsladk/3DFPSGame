using UnityEngine;

// 역할 : 캐릭터 모델(Animator 가 있는 자식)에서 일어난 애니메이션 이벤트를 부모의 Player·PlayerBombFire 로 전달한다.
// 애니메이션 이벤트는 Animator 가 붙은 오브젝트의 컴포넌트만 부를 수 있어서 이 중계 스크립트가 필요하다.
public class PlayerHitEvent : MonoBehaviour
{
    private Player _player;
    private PlayerBombFire _bombFire;

    private void Start()
    {
        _player = GetComponentInParent<Player>();
        _bombFire = GetComponentInParent<PlayerBombFire>();
    }

    // 발사 애니메이션에서 총을 쏘는 프레임에 걸어 둔 이벤트
    public void HitEvent()
    {
        _player.AttackAction();
    }

    // 던지기 애니메이션에서 손을 놓는 프레임에 걸어 둔 이벤트
    public void ThrowEvent()
    {
        _bombFire.ThrowEvent();
    }
}