// 역할 : 마우스 버튼(좌클릭·우클릭)에 끼울 수 있는 행동의 종류. Inspector 에서 무기마다 고르면 그 행동(IWeaponAction)이 만들어진다.
public enum WeaponActionType
{
    None, // 아무것도 하지 않는다
    Shoot, // 총을 쏜다
    ThrowBomb, // 폭탄을 던진다
    Zoom, // 줌인 한다
    Swing // 근접 무기를 휘두른다
}
