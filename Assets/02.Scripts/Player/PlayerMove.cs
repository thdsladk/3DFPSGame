using System;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 7f;
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

        // 3. 방향과 속력에 따라 이동한다. 
        //transform.position += direction * _moveSpeed * Time.deltaTime;
        _characterController.Move(direction * _moveSpeed * Time.deltaTime);
    }
}