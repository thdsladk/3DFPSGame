using UnityEngine;

// 역할 : 플레이어의 수평(앞뒤·좌우) 속도만 계산한다. (입력, 카메라 기준 방향, 달리기)
// 실제 이동은 PlayerMotor가 이 값을 받아서 CharacterController.Move 로 처리한다.
public class PlayerMove : MonoBehaviour
{
    [Header("Stat")]
    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _sprintSpeed = 14f;
    [SerializeField] private float _sprintStaminaCost = 10f; // 초당 소모량

    [Header("가속과 감속 : 조금씩 빨라지고 조금씩 멈춤")]
    [SerializeField] private bool _useAcceleration = true;
    [SerializeField] private float _acceleration = 60f;
    [SerializeField] private float _deceleration = 50f;

    [Header("공중 제어 : 공중에서는 방향 전환을 약하게")]
    [SerializeField] private bool _useAirControl = true;
    [SerializeField] private float _airControlMultiplier = 0.4f;

    [SerializeField] private Transform _cameraTransform;

    private PlayerStamina _playerStamina;
    private Vector3 _horizontalVelocity = Vector3.zero;

    public Vector3 HorizontalVelocity => _horizontalVelocity;

    private void Awake()
    {
        _playerStamina = GetComponent<PlayerStamina>();
    }

    // PlayerMotor가 매 프레임 Move 하기 전에 호출한다.
    public Vector3 CalculateHorizontalVelocity(bool isGrounded, float deltaTime)
    {
        // 1. 키보드 입력을 받는다.
        var h = Input.GetAxisRaw("Horizontal"); // -1,0,1
        var v = Input.GetAxisRaw("Vertical"); // -1,0,1

        // 2. 키보드 입력에 따라 방향을 구한다.
        var direction = new Vector3(h, 0, v);
        direction.Normalize();
        // 카메라의 좌우 방향(yaw)만 기준으로 삼는다. 위아래 각도까지 쓰면 위를 볼 때 하늘로, 탑다운에서 땅으로 이동하려 한다.
        direction = Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f) * direction;

        // 3. 움직이는 중에 달리기 입력이 있고 스태미나가 충분하면 달린다.
        var currentSpeed = _moveSpeed;
        var isMoving = direction.sqrMagnitude > 0f;
        if (isMoving && Input.GetButton("Sprint") && TryConsumeSprintStamina(deltaTime))
        {
            currentSpeed = _sprintSpeed;
        }

        var targetVelocity = direction * currentSpeed;

        // 4. 가속·감속을 쓰지 않으면 바로 목표 속도가 된다.
        if (_useAcceleration == false)
        {
            _horizontalVelocity = targetVelocity;
            return _horizontalVelocity;
        }

        // 5. 입력이 있으면 가속, 없으면 감속하면서 목표 속도로 다가간다. 공중에서는 더 약하게 바뀐다.
        var rate = isMoving ? _acceleration : _deceleration;
        if (_useAirControl && isGrounded == false)
        {
            rate *= _airControlMultiplier;
        }

        _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, rate * deltaTime);
        return _horizontalVelocity;
    }

    private bool TryConsumeSprintStamina(float deltaTime)
    {
        if (_playerStamina == null) return true;
        return _playerStamina.TryConsume(_sprintStaminaCost * deltaTime);
    }
}
