using UnityEngine;

public partial class Player
{
    // 발사 : 마우스 왼쪽 버튼을 누르는 동안 발사 애니메이션을 반복한다. 이동은 그대로 할 수 있다.
    // 실제 발사는 애니메이션의 총을 쏘는 프레임에 걸어 둔 이벤트(PlayerHitEvent → AttackAction)에서 한다.
    private class FireState : IPlayerState
    {
        private readonly Player _player;
        private float _cycleTimer = 0f; // 이번 발사 애니메이션이 시작된 뒤 지난 시간
        private int _shotCountAtCycleStart = 0; // 이번 애니메이션이 시작될 때까지 쏜 총알 수

        public FireState(Player player)
        {
            _player = player;
        }

        public void Enter()
        {
            _player.SetFireAnimationSpeed();
            StartCycle();
        }

        public void Update()
        {
            _cycleTimer += Time.deltaTime;
            if (_cycleTimer < _player.FireCycleDuration) return;

            // 1. 애니메이션 한 번이 끝날 때까지 이벤트가 오지 않았으면(이벤트가 없는 클립 등) 직접 쏜다.
            if (_player._gunFire.ShotCount == _shotCountAtCycleStart)
            {
                GameLogger.LogWarning("[Player] 발사 애니메이션에 HitEvent 가 없어 직접 발사했습니다.");
                _player.AttackAction();
            }

            // 2. 계속 쏘려면 다음 주기로(발사 클립은 IsFire 가 켜져 있는 동안 반복된다), 아니면 이동 여부에 따라 걷기·대기로 돌아간다.
            if (_player.WantsToFire)
            {
                StartCycle();
            }
            else
            {
                _player.ChangeState(_player.IsMoving ? PlayerState.Walk : PlayerState.Idle);
            }
        }

        public void Exit()
        {
        }

        private void StartCycle()
        {
            _cycleTimer = 0f;
            _shotCountAtCycleStart = _player._gunFire.ShotCount;
        }
    }
}
