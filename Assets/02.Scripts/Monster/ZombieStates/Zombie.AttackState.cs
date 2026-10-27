using UnityEngine;

public partial class Zombie
{
    // 공격 : 공격 간격마다 공격 애니메이션을 재생한다. 실제 데미지는 애니메이션 이벤트(AttackAction)에서 준다.
    private class AttackState : IZombieState
    {
        private readonly Zombie _zombie;
        private float _attackTimer = 0f;

        public AttackState(Zombie zombie)
        {
            _zombie = zombie;
        }

        public void Enter()
        {
            _attackTimer = 0f;
        }

        public void Update()
        {
            // 1. 멈춰서 공격한다.
            _zombie.StopAgent();

            // 2. 플레이어가 공격 사정 거리를 벗어나면 다시 쫓아간다.
            // AttackDelay → Move 로 돌아간다. (Animator에 IdleToMove 조건으로 AttackDelay → Move 전이가 있어야 함)
            if (_zombie.IsPlayerWithin(_zombie._attackRange) == false)
            {
                _zombie._animator.SetTrigger(IdleToMoveHash);
                _zombie.ChangeState(ZombieState.Trace);
                return;
            }

            // 3. 공격 간격마다 공격 애니메이션 실행 (AttackDelay → Attack, 끝나면 다시 AttackDelay)
            _attackTimer += Time.deltaTime;
            if (_attackTimer >= _zombie._attackInterval)
            {
                _zombie._animator.SetTrigger(StartAttackHash);
                _attackTimer = 0f;
            }
        }

        public void Exit()
        {
        }
    }
}
