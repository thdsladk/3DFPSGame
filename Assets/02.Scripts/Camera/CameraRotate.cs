using UnityEngine;

// 역할 : 마우스 커서의 세로 위치를 카메라의 위아래 회전 각도(Pitch)로 바꾼다.
// 커서가 가운데면 정면, 맨 위면 위로 _maxPitch, 맨 아래면 아래로 _maxPitch 만큼 본다.
// 실제 카메라 위치·회전 적용은 CameraViewChanger가 담당한다.
public class CameraRotate : MonoBehaviour
{
    [SerializeField] private float _maxPitch = 60f; // 커서가 화면 위아래 끝에 있을 때의 각도

    // 카메라 eulerAngles.x 에 그대로 넣을 수 있는 위아래 각도 (양수 = 아래를 봄)
    public float Pitch { get; private set; }

    // CameraViewChanger가 LateUpdate에서 읽기 전에 계산이 끝나도록 Update에서 처리한다.
    private void Update()
    {
        // 커서가 위에 있을수록(+1) 위를 보도록 부호를 뒤집는다. 유니티는 x 각도가 음수일 때 위를 본다.
        var cursorY = CursorManager.GetNormalizedCursorPosition().y;
        Pitch = -cursorY * _maxPitch;

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
