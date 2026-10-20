using System;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    private const float Gravity = -20f;
    private float _yVelocity = 0f;

    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _jumpPower = 12f;

    [SerializeField] private CharacterController _characterController;
    [SerializeField] private Transform _cameraTransform;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
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

        // 4. 점프키를 누르면 점프 
        if (Input.GetButtonDown("Jump") && _characterController.isGrounded) // 중재자 패턴 
        {
            _yVelocity = _jumpPower;
        }

        // 5. 속도를 구한다.
        Vector3 velocity = direction * _moveSpeed;
        velocity.y = _yVelocity;

        // 6. 속도(방향과 속력)에 따라 이동한다. 
        //transform.position += direction * _moveSpeed * Time.deltaTime;
        _characterController.Move(velocity * Time.deltaTime);
    }
}