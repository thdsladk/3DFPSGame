// 역할 : 좀비 상태 하나가 해야 할 일. 상태마다 이 인터페이스를 구현한 클래스를 하나씩 만든다. (상태 패턴)
// - Enter  : 이 상태로 들어올 때 한 번
// - Update : 이 상태인 동안 매 프레임
// - Exit   : 다른 상태로 나갈 때 한 번
public interface IZombieState
{
    void Enter();
    void Update();
    void Exit();
}
