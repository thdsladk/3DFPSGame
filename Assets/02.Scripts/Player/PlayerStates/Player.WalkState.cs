public partial class Player
{
    // 걷기 : 움직이는 중이다. 쏘면 발사, 멈추면 대기로 간다.
    private class WalkState : IPlayerState
    {
        private readonly Player _player;

        public WalkState(Player player)
        {
            _player = player;
        }

        public void Enter()
        {
        }

        public void Update()
        {
            // 발사가 걷기보다 우선이다. (Fire > Walk > Idle)
            if (_player.WantsToFire)
            {
                _player.ChangeState(PlayerState.Fire);
            }
            else if (_player.IsMoving == false)
            {
                _player.ChangeState(PlayerState.Idle);
            }
        }

        public void Exit()
        {
        }
    }
}
