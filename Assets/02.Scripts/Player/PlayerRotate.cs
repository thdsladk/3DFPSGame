using UnityEngine;

// 역할 : 마우스 커서의 가로 위치에 맞춰 플레이어의 좌우 방향을 정한다.
// 커서가 가운데면 기준 방향(정면), 좌우 끝이면 ±_maxScreenYaw 만큼 돌아간다.
// 커서가 화면 끝에 닿은 채로 같은 방향으로 더 밀면 기준 방향 자체가 계속 돌아간다.
public class PlayerRotate : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 200f; // 화면 끝에서 더 밀 때의 회전 속도
    [SerializeField] private float _maxScreenYaw = 60f; // 커서가 화면 좌우 끝에 있을 때의 각도
    [SerializeField] private float _edgeThreshold = 0.98f; // 이 값 이상이면 화면 끝에 닿은 것으로 본다 (0 ~ 1)

    private float _baseYaw = 0f; // 화면 끝에서 밀어서 누적된 기준 방향 (커서가 가운데일 때의 방향)
    private float _screenYaw = 0f; // 마지막으로 적용한 커서 위치에 따른 각도
    private bool _isFirstFrame = true; // 커서가 화면 가운데로 옮겨질 때의 튀는 입력을 무시하기 위한 플래그

    // 고정 시점(쿼터뷰·탑다운)에서 꺼졌다가 다시 켜질 때 현재 방향에서 이어서 회전하도록 맞춘다.
    private void OnEnable()
    {
        _baseYaw = transform.eulerAngles.y - _screenYaw;
    }

    private void Update()
    {
        var mouseX = Input.GetAxis("Mouse X");
        var cursorX = CursorManager.GetNormalizedCursorPosition().x;

        if (_isFirstFrame)
        {
            _isFirstFrame = false;
            mouseX = 0f;
        }

        // 1. 화면 끝에 닿은 채로 바깥쪽으로 더 밀면 기준 방향을 돌린다.
        var isPushingRight = cursorX >= _edgeThreshold && mouseX > 0f;
        var isPushingLeft = cursorX <= -_edgeThreshold && mouseX < 0f;
        if (isPushingRight || isPushingLeft)
        {
            _baseYaw += mouseX * _rotationSpeed * Time.deltaTime;
        }

        // 2. 커서의 가로 위치를 각도로 바꿔서 기준 방향에 더한다.
        _screenYaw = cursorX * _maxScreenYaw;
        transform.eulerAngles = new Vector3(0f, _baseYaw + _screenYaw, 0f);
    }
}
