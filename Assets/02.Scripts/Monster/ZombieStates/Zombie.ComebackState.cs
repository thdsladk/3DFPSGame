public partial class Zombie
{
    // 복귀 : 처음 자리로 돌아간다. 도착하면 대기한다.
    private class ComebackState : IZombieState
    {
        private readonly Zombie _zombie;

        public ComebackState(Zombie zombie)
        {
            _zombie = zombie;
        }

        public void Enter()
        {
            // 목적지는 들어올 때 한 번만 정한다. 매 프레임 다시 정하면 경로를 계속 새로 계산해서 남은 거리가 흔들린다.
            // (Hit 에서 돌아올 때도 멈출 때 지워진 목적지를 여기서 다시 정한다)
            _zombie._agent.SetDestination(_zombie._startPosition);
        }

        public void Update()
        {
            // pathStatus 는 "도착했다"가 아니라 "경로를 찾았다"는 뜻이므로, 남은 거리로 도착을 판단한다.
            if (_zombie._agent.pathPending == false && _zombie._agent.remainingDistance <= _zombie._arriveDistance)
            {
                _zombie._animator.SetTrigger(MoveToIdleHash);
                _zombie.ChangeState(ZombieState.Idle);
            }
        }

        public void Exit()
        {
        }
    }
}
