// 역할 : 누르면 던지는 모션을 시작해 힘을 모으고, 떼면 폭탄을 던진다.
public class BombThrowAction : IWeaponAction
{
    private readonly PlayerBombFire _bombFire;

    public BombThrowAction(PlayerBombFire bombFire)
    {
        _bombFire = bombFire;
    }

    public void Press() => _bombFire.StartThrow();
    public void Release() => _bombFire.ReleaseThrow();

    // 무기를 바꿔도 손에 든 폭탄은 그대로 던진다. (던지는 모션이 멈춘 채 남지 않게)
    public void Cancel() => _bombFire.ReleaseThrow();
}
