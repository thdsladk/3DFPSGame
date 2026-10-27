using System.Collections;
using UnityEngine;

public partial class Zombie
{
    // 죽음 : 쓰러지는 애니메이션을 재생하고 잠시 뒤 사라진다. (풀에서 나왔으면 풀로 돌아간다)
    private class DeathState : IZombieState
    {
        private readonly Zombie _zombie;

        public DeathState(Zombie zombie)
        {
            _zombie = zombie;
        }

        public void Enter()
        {
            _zombie.StopAgent();
            _zombie._animator.SetTrigger(DeathHash);
            _zombie.StartCoroutine(Death_Coroutine());
        }

        public void Update()
        {
        }

        public void Exit()
        {
        }

        private IEnumerator Death_Coroutine()
        {
            // 1. 죽는 순간 남은 좀비 수에서 뺀다.
            GameManager.Instance.UnregisterZombie();

            // 2. 쓰러진 몸에 길이 막히지 않게 충돌을 끈다.
            _zombie._collider.enabled = false;

            // 3. 잠시 뒤 사라진다.
            yield return new WaitForSeconds(_zombie._deathRemoveDelay);

            _zombie.Despawn();
        }
    }
}
