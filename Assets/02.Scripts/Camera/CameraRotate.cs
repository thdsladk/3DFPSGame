using UnityEngine;

// 역할 : 마우스 위아래 입력을 누적해서 카메라의 위아래 회전 각도(Pitch)를 계산한다.
// 실제 카메라 위치·회전 적용은 CameraViewChanger가 담당한다.
public class CameraRotate : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 200f;
    private float _accumulationY = 0; // 위아래 회전 각도
    private bool _isFirstFrame = true; // 커서 잠금 직후의 튀는 입력을 무시하기 위한 플래그

    // 카메라 eulerAngles.x 에 그대로 넣을 수 있는 위아래 각도 (양수 = 아래를 봄)
    public float Pitch => -_accumulationY;


    // CameraViewChanger가 LateUpdate에서 읽기 전에 계산이 끝나도록 Update에서 처리한다.
    private void Update()
    {
        // 1. 마우스 입력을 받는다.
        var mouseY = Input.GetAxis("Mouse Y");

        GameLogger.Log($"mouseY: {mouseY}");

        // 커서가 잠기면서 화면 가운데로 이동할 때 생기는 첫 프레임의 큰 입력값은 무시한다.
        if (_isFirstFrame)
        {
            _isFirstFrame = false;
            return;
        }

        // 2. 마우스 입력 만큼 회전값을 누적한다.
        _accumulationY += mouseY * _rotationSpeed * Time.deltaTime;

        // 3. 누적하기 전에 내 변수에서 먼저 -90 ~ 90도로 제한
        _accumulationY = Mathf.Clamp(_accumulationY, -90, 90);

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