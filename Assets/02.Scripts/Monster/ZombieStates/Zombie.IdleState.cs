using UnityEngine;

public partial class Zombie
{
    // 대기 : 제자리에서 플레이어를 기다리다가, 잠깐 쉬고 나면 순찰을 나간다.
    private class IdleState : IZombieState
    {
        private readonly Zombie _zombie;
        private float _idleTimer = 0f;

        public IdleState(Zombie zombie)
        {
            _zombie = zombie;
        }

        public void Enter()
        {
            _idleTimer = 0f;
        }

        public void Update()
        {
            // 1. 제자리에 멈춰 있는다.
            _zombie.StopAgent();

            // 2. 플레이어가 가까워지면 추격한다.
            if (_zombie.IsPlayerWithin(_zombie._detectDistance))
            {
                _zombie._animator.SetTrigger(IdleToMoveHash);
                _zombie.ChangeState(ZombieState.Trace);
                return;
            }

            // 3. 잠깐 쉬었다가 다음 순찰 지점으로 출발한다.
            _idleTimer += Time.deltaTime;
            if (_idleTimer >= _zombie._idleWaitTime)
            {
                _zombie.PreparePatrol();
                _zombie._animator.SetTrigger(IdleToMoveHash);
                _zombie.ChangeState(ZombieState.Patrol);
            }
        }

        public void Exit()
        {
        }
    }
}
