using System.Collections;
using UnityEngine;

public partial class Zombie
{
    // 피격 : 잠깐 멈칫하고 Hit 애니메이션을 재생한 뒤, 맞기 전 상태와 애니메이션으로 돌아간다.
    private class HitState : IZombieState
    {
        private readonly Zombie _zombie;

        public HitState(Zombie zombie)
        {
            _zombie = zombie;
        }

        public void Enter()
        {
            // 1. 맞기 전 상태를 기억한다. 연속으로 맞는 중이면 처음 맞기 전 상태를 그대로 둔다.
            if (_zombie._previousState != ZombieState.Hit)
            {
                _zombie._stateBeforeHit = _zombie._previousState;
            }

            // 2. 멈춰서 Hit 애니메이션을 재생한다.
            _zombie.StopAgent();
            _zombie._animator.SetTrigger(HitHash);
            _zombie.StartCoroutine(Hit_Coroutine());
        }

        public void Update()
        {
        }

        public void Exit()
        {
        }

        private IEnumerator Hit_Coroutine()
        {
            yield return new WaitForSeconds(_zombie._hitDuration);

            // 1. 맞기 전에 걸려 남은 트리거가 돌아가자마자 엉뚱한 전이를 일으키지 않게 지운다.
            Animator animator = _zombie._animator;
            animator.ResetTrigger(IdleToMoveHash);
            animator.ResetTrigger(MoveToIdleHash);
            animator.ResetTrigger(MoveToAttackDelayHash);
            animator.ResetTrigger(StartAttackHash);
            animator.ResetTrigger(HitHash);

            // 2. 맞기 전 상태에 맞는 애니메이션으로 섞어서 돌아간다.
            animator.CrossFadeInFixedTime(GetRecoverAnimationHash(_zombie._stateBeforeHit), _zombie._hitRecoverBlendTime, 0);

            // 3. 맞기 전 상태로 돌아간다.
            _zombie.ChangeState(_zombie._stateBeforeHit);
        }

        // 돌아갈 애니메이션. 공격 중이었으면 휘두르던 모션을 다시 하지 않고 공격 대기로 돌아간다.
        private static int GetRecoverAnimationHash(ZombieState state)
        {
            switch (state)
            {
                case ZombieState.Idle:
                {
                    return IdleStateHash;
                }
                case ZombieState.Attack:
                {
                    return AttackDelayStateHash;
                }
                default:
                {
                    // Patrol, Trace, Comeback 은 모두 걷는 애니메이션
                    return MoveStateHash;
                }
            }
        }
    }
}
