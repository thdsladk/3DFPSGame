// 역할 : 마우스 버튼 하나(좌클릭 또는 우클릭)가 하는 일. (전략 패턴의 전략)
// 무기마다 다른 행동 객체를 끼워 두면, 같은 버튼이라도 든 무기에 따라 하는 일이 바뀐다.
// - Press   : 버튼을 눌렀을 때
// - Release : 버튼을 뗐을 때
// - Cancel  : 무기를 바꿔 이 행동을 그만둘 때
public interface IWeaponAction
{
    void Press();
    void Release();
    void Cancel();
}
