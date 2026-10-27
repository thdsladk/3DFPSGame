using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public partial class Zombie
{
    // 점프 : 추격 중 길이 끊긴 곳에서, 추격 상태가 골라 둔 착지점(_jumpLanding)까지 포물선으로 뛴다.
    private class JumpState : IZombieState
    {
        private readonly Zombie _zombie;
        private bool _isInAir = false; // 공중에 있는 동안 true. 도중에 다른 상태로 끊기면 Exit 에서 바닥에 내려놓는다.

        public JumpState(Zombie zombie)
        {
            _zombie = zombie;
        }

        public void Enter()
        {
            _isInAir = true;
            _zombie._animator.SetTrigger(JumpHash);
            _zombie.StartCoroutine(Jump_Coroutine(_zombie._jumpLanding));
        }

        public void Update()
        {
            // 점프 이동은 Jump_Coroutine 이 처리한다.
        }

        public void Exit()
        {
            // 공중에서 끊기면(점프 중 사망) 바로 아래 NavMesh(없으면 착지점)로 내려놓고 에이전트를 다시 켠다.
            if (_isInAir)
            {
                _isInAir = false;
                _zombie.EndJump(_zombie.GetGroundBelowOrLanding());
            }
        }

        private IEnumerator Jump_Coroutine(Vector3 landing)
        {
            NavMeshAgent agent = _zombie._agent;
            Transform transform = _zombie.transform;

            // 1. 에이전트가 위치·회전을 끌고 가지 않게 한다. (transform 을 직접 움직인다)
            // 끊긴 길로 가던 기존 경로는 버린다. (남겨 두면 착지 후 그 경로를 따라 다시 내려가려 한다)
            agent.ResetPath();
            agent.isStopped = true;
            agent.updatePosition = false;
            agent.updateRotation = false;

            // 2. 출발점·도착점과 점프 시간을 정하고, 도착 방향을 바라본다.
            Vector3 start = transform.position;
            Vector3 end = landing + Vector3.up * agent.baseOffset;

            Vector3 flatDirection = end - start;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(flatDirection);
            }

            float duration = Mathf.Max(MinJumpDuration, Vector3.Distance(start, end) / _zombie._jumpSpeed);

            // 직선(start→end)의 가운데 높이에 더해서, 높은 쪽보다 _jumpArcHeight 만큼 더 솟게 한다.
            float arcHeight = Mathf.Abs(end.y - start.y) * 0.5f + _zombie._jumpArcHeight;

            // 3. 포물선을 따라 이동한다.
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 position = Vector3.Lerp(start, end, t);
                position.y += 4f * arcHeight * t * (1f - t);
                transform.position = position;

                yield return null;
            }

            // 4. 착지 : 실제로 내려앉은 자리가 이동 가능 지역(NavMesh)이면 그 자리로, 아니면 처음 고른 착지점으로 에이전트를 옮긴다.
            Vector3 landedPosition = landing;
            if (NavMesh.SamplePosition(_zombie.GetFeetPosition(), out NavMeshHit landedHit, LandingSampleRadius, agent.areaMask))
            {
                landedPosition = landedHit.position;
            }

            _isInAir = false;
            _zombie.EndJump(landedPosition);

            _zombie._animator.ResetTrigger(JumpHash);
            _zombie._animator.CrossFadeInFixedTime(MoveStateHash, _zombie._jumpLandBlendTime, 0);

            // 5. 착지한 자리에서 플레이어까지 경로를 새로 잡고 추격을 이어간다. 바로 다시 뛰지 않도록 쿨다운을 건다.
            GameLogger.Log($"[Zombie] 착지 : {landedPosition}");
            agent.SetDestination(_zombie._player.transform.position);
            _zombie._nextJumpCheckTime = Time.time + _zombie._jumpCooldown;
            _zombie.ChangeState(ZombieState.Trace);
        }
    }
}
