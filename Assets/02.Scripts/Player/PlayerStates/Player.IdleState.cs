public partial class Player
{
    // 대기 : 가만히 서 있다. 쏘면 발사, 움직이면 걷기로 간다.
    private class IdleState : IPlayerState
    {
        private readonly Player _player;

        public IdleState(Player player)
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
            else if (_player.IsMoving)
            {
                _player.ChangeState(PlayerState.Walk);
            }
        }

        public void Exit()
        {
        }
    }
}
