using UnityEngine;

// 역할 : 마우스를 움직이면 카메라를 그 방향으로 회전하고 싶다.    
public class CameraRotate : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 200f;
    private float _accumulationX = 0; // 좌우   회전 각도
    private float _accumulationY = 0; // 위아래 회전 각도

    private void Update()
    {
        // 1. 마우스 입력을 받는다.
        var mouseX = Input.GetAxis("Mouse X");
        var mouseY = Input.GetAxis("Mouse Y");
        Debug.Log($"mouseX: {mouseX}, mouseY: {mouseY}");

        // 2. 마우스 입력 만큼 회전값을 누적한다. 
        //Vector3 direction = new Vector3(-mouseY, mouseX, 0);
        _accumulationX += mouseX * _rotationSpeed * Time.deltaTime;
        _accumulationY += -mouseY * _rotationSpeed * Time.deltaTime;

        // 3. 누적하기 전에 내 변수에서 먼저 -90 ~ 90도로 제한
        _accumulationY = Mathf.Clamp(_accumulationY, -90, 90);

        // 4. 누적한 회전값을 카메라에 적용한다. 
        transform.eulerAngles = new Vector3(-_accumulationY, _accumulationX, 0);


        // // 3. 그 방향으로 회전 시킨다. 
        // transform.eulerAngles += direction * _rotationSpeed * Time.deltaTime;
        // 
        // // 4. 위 아래 회전 값을 -90 ~ 90도로 제한한다.
        // Vector3 rotation = transform.eulerAngles;
        // // 정면 : 0도 
        // // 하늘을 살짝 1도 만큼 쳐다보면 : -1 -> 359 -> 90
        // // 유니티는 0~360 체계를 쓰기 때문에 음수는 자동으로 변환되서 반환된다.
        // 
        // rotation.x = Mathf.Clamp(rotation.x, -90, 90);
        // transform.eulerAngles = rotation;
    }
}