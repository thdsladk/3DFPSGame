using System;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private const float Gravity = -20f;
    private const int MaxJumps = 2;
    private const float MaxStamina = 100f;

    private float _yVelocity = 0f;
    private int _jumpCount = 0;
    private float _stamina = 0f;

    [Header("Stat")]
    [SerializeField] private float _moveSpeed = 7f;

    [SerializeField] private float _sprintSpeed = 14f;
    [SerializeField] private float _jumpPower = 12f;
    [SerializeField] private float _staminaRecovery = 15f;
    [SerializeField] private float _sprintStaminaCost = 10f;
    [SerializeField] private float _jumpStaminaCost = 20f;


    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Transform _cameraTransform;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _stamina = MaxStamina;
    }

    private void Update()
    {
        // 0. 스태미나 회복 
        _stamina = Mathf.Min(MaxStamina, _stamina + _staminaRecovery * Time.deltaTime);

        // 1. 키보드 입력을 받는다.
        var h = Input.GetAxisRaw("Horizontal"); // -1,0,1
        var v = Input.GetAxisRaw("Vertical"); // -1,0,1

        // 2. 키보드 입력에 따라 방향을 구한다.
        var direction = new Vector3(h, 0, v);
        direction.Normalize();
        // 카메라의 좌우 방향(yaw)만 기준으로 삼는다. 위아래 각도까지 쓰면 위를 볼 때 하늘로, 탑다운에서 땅으로 이동하려 한다.
        direction = Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f) * direction;
        // 로컬 좌표계 , 월드 좌표계

        // 3. 중력을 누적한다.
        if (_characterController.isGrounded == false)
        {
            _yVelocity += Gravity * Time.deltaTime;
        }
        else
        {
            _yVelocity = -1f;
            _jumpCount = 0;
        }

        // 4. 점프키를 누르면 점프 && 점프 카운트가 MaxJumps이하라면 ... 점프를 한다. 
        if (Input.GetButtonDown("Jump") && _jumpCount < MaxJumps && _stamina >= _jumpStaminaCost) // 중재자 패턴 
        {
            _yVelocity = _jumpPower;
            _jumpCount++;
            _stamina -= _jumpStaminaCost;
        }

        // 5. 속도를 구한다.\
        float requiredStamina = _sprintStaminaCost * Time.deltaTime;
        bool wantSprint = Input.GetButton("Sprint") && _stamina > requiredStamina;
        float currentSpeed = _moveSpeed;
        if (wantSprint)
        {
            currentSpeed = _sprintSpeed;
            _stamina -= requiredStamina;
        }

        Vector3 velocity = direction * currentSpeed;
        velocity.y = _yVelocity;

        // 6. 속도(방향과 속력)에 따라 이동한다. 
        //transform.position += direction * _moveSpeed * Time.deltaTime;
        _characterController.Move(velocity * Time.deltaTime);
    }
}