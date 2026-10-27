using UnityEngine;

public partial class Zombie
{
    // 추격 : 플레이어를 쫓아간다. 너무 멀어지면 복귀하고, 가까우면 공격하고, 길이 끊기면 점프한다.
    private class TraceState : IZombieState
    {
        private readonly Zombie _zombie;

        public TraceState(Zombie zombie)
        {
            _zombie = zombie;
        }

        public void Enter()
        {
        }

        public void Update()
        {
            // Todo. Run 애니메이션 실행

            // 1. 처음 자리에서 너무 멀어지면 돌아간다.
            if (Vector3.Distance(_zombie.transform.position, _zombie._startPosition) > _zombie._comebackDistance)
            {
                StartComeback();
                return;
            }

            // 2. 플레이어가 공격 거리 안이면 공격한다.
            if (_zombie.IsPlayerWithin(_zombie._attackRange))
            {
                _zombie._animator.SetTrigger(MoveToAttackDelayHash);
                _zombie.ChangeState(ZombieState.Attack);
                return;
            }

            // 3. 플레이어까지 걸어서 가는 길이 끊겼으면, 점프로 건너갈 수 있는 이동 가능 지역을 찾아 뛴다.
            // 에이전트 내부 경로 상태는 매 프레임 목적지를 다시 정하면 믿기 어려우므로, 경로를 직접 계산해서 판단한다.
            if (Time.time >= _zombie._nextJumpCheckTime)
            {
                _zombie._nextJumpCheckTime = Time.time + _zombie._jumpCheckInterval;

                if (_zombie.HasCompletePath(_zombie.GetFeetPosition(), _zombie.GetPlayerFeetPosition()) == false
                    && _zombie.TryFindJumpLanding(out Vector3 landing))
                {
                    _zombie._jumpLanding = landing;
                    _zombie.ChangeState(ZombieState.Jump);
                    return;
                }
            }

            // 4. 플레이어를 향해 간다.
            // SetDestination은 목적지 설정
            // - 호출할 때마다 목적지를 향해 조금씩 이동 (X)
            // - 한 번 세팅하면 내부에서 계속 이동 (O)
            _zombie._agent.SetDestination(_zombie._player.transform.position);
        }

        public void Exit()
        {
        }

        private void StartComeback()
        {
            // 처음 자리로 돌아갈 길이 없으면 (절벽 아래로 뛰어내린 경우 등) 지금 자리를 새 처음 자리로 삼고 대기한다.
            if (_zombie.HasCompletePath(_zombie.GetFeetPosition(), _zombie._startPosition) == false)
            {
                _zombie.SetNewStartPosition();
                _zombie._animator.SetTrigger(MoveToIdleHash);
                _zombie.ChangeState(ZombieState.Idle);
                return;
            }

            _zombie.ChangeState(ZombieState.Comeback);
        }
    }
}
