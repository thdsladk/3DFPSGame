// 역할 : 누르고 있는 동안 지금 든 총을 쏜다. (실제 발사는 Player 의 발사 상태와 애니메이션 이벤트가 한다)
public class ShootAction : IWeaponAction
{
    private readonly PlayerGunFire _gunFire;

    public ShootAction(PlayerGunFire gunFire)
    {
        _gunFire = gunFire;
    }

    public void Press() => _gunFire.PullTrigger();
    public void Release() => _gunFire.ReleaseTrigger();
    public void Cancel() => _gunFire.ReleaseTrigger();
}
