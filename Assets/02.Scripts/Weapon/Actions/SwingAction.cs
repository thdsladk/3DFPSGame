// 역할 : 누르면 근접 무기를 한 번 휘두른다.
public class SwingAction : IWeaponAction
{
    private readonly PlayerMeleeAttack _meleeAttack;

    public SwingAction(PlayerMeleeAttack meleeAttack)
    {
        _meleeAttack = meleeAttack;
    }

    public void Press() => _meleeAttack.StartAttack();

    public void Release()
    {
    }

    public void Cancel() => _meleeAttack.CancelAttack();
}
