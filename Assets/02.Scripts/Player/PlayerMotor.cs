using UnityEngine;

// 역할 : 플레이어 이동의 중재자. 수평 속도(PlayerMove)와 수직 속도(PlayerJump)를 받아서
// CharacterController.Move 를 프레임마다 딱 한 번만 호출하고, 충돌 결과를 다시 알려준다.
// isGrounded·천장 판정은 Move 결과로만 갱신되므로 Move 는 반드시 여기서만 호출한다.
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerMove))]
[RequireComponent(typeof(PlayerJump))]
[RequireComponent(typeof(PlayerStamina))]
public class PlayerMotor : MonoBehaviour
{
    private CharacterController _characterController;
    private PlayerMove _playerMove;
    private PlayerJump _playerJump;

    public bool IsGrounded => _characterController.isGrounded;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _playerMove = GetComponent<PlayerMove>();
        _playerJump = GetComponent<PlayerJump>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;

        var deltaTime = Time.deltaTime;

        // 1. 직전 Move 결과로 바닥에 있는지 확인한다.
        var isGrounded = _characterController.isGrounded;

        // 2. 수평 속도와 수직 속도를 각각 계산한다.
        var velocity = _playerMove.CalculateHorizontalVelocity(isGrounded, deltaTime);
        velocity.y = _playerJump.CalculateVerticalVelocity(isGrounded, deltaTime);

        // 3. 한 번만 이동하고, 충돌 결과(천장 등)를 점프 쪽에 알려준다.
        var collisionFlags = _characterController.Move(velocity * deltaTime);
        _playerJump.OnMoved(collisionFlags);
    }
}
