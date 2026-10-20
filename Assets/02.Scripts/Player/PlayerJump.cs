using UnityEngine;

// 역할 : 플레이어의 위아래 속도(중력, 점프)만 계산한다.
// 실제 이동은 PlayerMotor가 이 값을 받아서 CharacterController.Move 로 처리한다.
// 게임필 기능은 각각 켜고 끌 수 있고, 하나를 꺼도 다른 기능에 영향을 주지 않는다.
public class PlayerJump : MonoBehaviour
{
    [Header("기본 점프")]
    [SerializeField] private float _gravity = -20f;
    [SerializeField] private float _jumpPower = 12f;
    [SerializeField] private int _maxJumps = 2; // 2 = 2단 점프
    [SerializeField] private float _jumpStaminaCost = 20f;
    [SerializeField] private float _groundStickVelocity = -2f; // 바닥에 붙어 있도록 아래로 누르는 속도

    [Header("코요테 타임 : 턱에서 떨어진 직후 잠깐 점프 허용")]
    [SerializeField] private bool _useCoyoteTime = true;
    [SerializeField] private float _coyoteTime = 0.1f;

    [Header("점프 버퍼 : 착지 직전에 누른 점프를 기억")]
    [SerializeField] private bool _useJumpBuffer = true;
    [SerializeField] private float _jumpBufferTime = 0.1f;

    [Header("가변 점프 높이 : 일찍 떼면 낮게 점프")]
    [SerializeField] private bool _useVariableJumpHeight = true;
    [SerializeField] private float _jumpCutMultiplier = 0.5f;

    [Header("빠른 낙하 : 떨어질 때 중력을 더 세게")]
    [SerializeField] private bool _useFastFall = true;
    [SerializeField] private float _fallGravityMultiplier = 2f;

    [Header("최대 낙하 속도")]
    [SerializeField] private bool _useMaxFallSpeed = true;
    [SerializeField] private float _maxFallSpeed = 30f;

    [Header("천장 부딪힘 : 머리가 닿으면 바로 떨어짐")]
    [SerializeField] private bool _useCeilingHit = true;

    private PlayerStamina _playerStamina;

    private float _yVelocity = 0f;
    private int _jumpCount = 0;
    private bool _isGrounded = false;
    private bool _isJumpRising = false; // 점프로 올라가는 중인지 (가변 점프 높이 판정용)
    private float _lastGroundedTime = float.NegativeInfinity;
    private float _lastJumpPressedTime = float.NegativeInfinity;

    public float YVelocity => _yVelocity;

    private void Awake()
    {
        _playerStamina = GetComponent<PlayerStamina>();
    }

    // PlayerMotor가 매 프레임 Move 하기 전에 호출한다.
    public float CalculateVerticalVelocity(bool isGrounded, float deltaTime)
    {
        var now = Time.time;
        _isGrounded = isGrounded;

        // 1. 바닥에 있으면 점프 횟수를 초기화하고, 공중이면 중력을 더한다.
        if (isGrounded)
        {
            _lastGroundedTime = now;
            _jumpCount = 0;

            if (_yVelocity < 0f)
            {
                _yVelocity = _groundStickVelocity;
                _isJumpRising = false;
            }
        }
        else
        {
            ApplyGravity(deltaTime);

            // 점프하지 않고 떨어졌는데 코요테 시간도 지났다면, 바닥 점프는 쓴 것으로 친다.
            if (_jumpCount == 0 && CanUseGroundJump(now) == false)
            {
                _jumpCount = 1;
            }
        }

        // 2. 점프 입력을 확인하고 가능하면 점프한다.
        var isJumpPressed = Input.GetButtonDown("Jump");
        if (isJumpPressed)
        {
            _lastJumpPressedTime = now;
        }

        var hasBufferedJump = _useJumpBuffer && now - _lastJumpPressedTime <= _jumpBufferTime;
        if (isJumpPressed || hasBufferedJump)
        {
            TryJump();
        }

        // 3. 올라가는 중에 점프키를 떼면 상승 속도를 줄인다.
        if (_useVariableJumpHeight && _isJumpRising && _yVelocity > 0f && Input.GetButtonUp("Jump"))
        {
            _yVelocity *= _jumpCutMultiplier;
            _isJumpRising = false;
        }

        if (_yVelocity <= 0f)
        {
            _isJumpRising = false;
        }

        // 4. 떨어지는 속도가 너무 빨라지지 않게 막는다.
        if (_useMaxFallSpeed)
        {
            _yVelocity = Mathf.Max(_yVelocity, -_maxFallSpeed);
        }

        return _yVelocity;
    }

    // PlayerMotor가 Move 한 뒤 충돌 결과를 넘겨준다.
    public void OnMoved(CollisionFlags collisionFlags)
    {
        var isHitCeiling = (collisionFlags & CollisionFlags.Above) != 0;
        if (_useCeilingHit && isHitCeiling && _yVelocity > 0f)
        {
            _yVelocity = 0f;
            _isJumpRising = false;
        }
    }

    private void ApplyGravity(float deltaTime)
    {
        var gravity = _gravity;
        if (_useFastFall && _yVelocity < 0f)
        {
            gravity *= _fallGravityMultiplier;
        }

        _yVelocity += gravity * deltaTime;
    }

    // 바닥에 있거나, 코요테 시간 안이면 바닥 점프를 할 수 있다.
    private bool CanUseGroundJump(float now)
    {
        if (_isGrounded) return true;
        return _useCoyoteTime && now - _lastGroundedTime <= _coyoteTime;
    }

    private void TryJump()
    {
        if (_jumpCount >= _maxJumps) return;
        if (_playerStamina != null && _playerStamina.TryConsume(_jumpStaminaCost) == false) return;

        _yVelocity = _jumpPower;
        _jumpCount++;
        _isJumpRising = true;

        // 같은 입력·같은 바닥 기록으로 다시 점프하지 않도록 지운다.
        _lastJumpPressedTime = float.NegativeInfinity;
        _lastGroundedTime = float.NegativeInfinity;
    }
}
