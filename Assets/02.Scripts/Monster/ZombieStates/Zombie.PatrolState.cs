using UnityEngine;

public partial class Zombie
{
    // 순찰 : 처음 자리와 주변의 랜덤 지점을 오간다. 목표 지점은 대기 상태에서 PreparePatrol 로 정해 둔다.
    private class PatrolState : IZombieState
    {
        private readonly Zombie _zombie;
        private float _patrolTimer = 0f;

        public PatrolState(Zombie zombie)
        {
            _zombie = zombie;
        }

        public void Enter()
        {
            _patrolTimer = 0f;
        }

        public void Update()
        {
            // Todo. Walk 애니메이션 실행

            // 1. 순찰 중에도 플레이어가 가까워지면 추격한다.
            // 이미 Move 애니메이션이므로 트리거를 걸지 않는다. (안 쓰인 트리거가 남아 있으면 나중에 엉뚱하게 전이된다)
            if (_zombie.IsPlayerWithin(_zombie._detectDistance))
            {
                _zombie.ChangeState(ZombieState.Trace);
                return;
            }

            // 2. 목표에 도착했거나, 벽 등에 막혀 너무 오래 걸리면 대기하고 다음 지점으로 간다.
            Vector3 direction = _zombie._patrolTarget - _zombie.transform.position;
            direction.y = 0f; // 높이 차이는 무시하고 바닥 기준 거리로 판단한다.

            _patrolTimer += Time.deltaTime;
            float patrolTimeLimit = _zombie._patrolDistance / _zombie._patrolSpeed * 2f + 1f;
            if (direction.magnitude <= _zombie._arriveDistance || _patrolTimer >= patrolTimeLimit)
            {
                _zombie._animator.SetTrigger(MoveToIdleHash);
                _zombie.ChangeState(ZombieState.Idle);
                return;
            }

            // 3. 목표 지점으로 걸어간다.
            _zombie._agent.SetDestination(_zombie._patrolTarget);
        }

        public void Exit()
        {
        }
    }
}
