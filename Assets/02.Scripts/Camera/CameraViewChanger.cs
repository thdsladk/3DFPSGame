using System;
using UnityEngine;

public enum CameraView
{
    FirstPerson, // 1인칭
    ThirdPerson, // 3인칭
    Shoulder, // 숄더뷰
    Quarter, // 쿼터뷰
    TopDown, // 탑다운뷰
}

[Serializable]
public class CameraViewSettings
{
    public CameraView View;
    public bool UseMouseLook; // true : 플레이어 방향 + 마우스 위아래, false : 고정 각도
    public Vector3 Offset; // 카메라 회전 기준으로 기준점에서 떨어진 거리
    public Vector3 FixedEulerAngles; // UseMouseLook 이 false 일 때 사용할 카메라 각도
}

// 역할 : V키를 누르면 1인칭 → 3인칭 → 숄더뷰 → 쿼터뷰 → 탑다운뷰 순서로 카메라 시점을 바꾼다.
public class CameraViewChanger : MonoBehaviour
{
    [SerializeField] private Transform _player; // 비워두면 PlayerRotate를 자동으로 찾는다
    [SerializeField] private Transform _eyePosition; // 비워두면 플레이어의 자식 CameraPosition을 찾는다
    [SerializeField] private float _transitionDuration = 0.3f;

    [SerializeField]
    private CameraViewSettings[] _views =
    {
        new CameraViewSettings { View = CameraView.FirstPerson, UseMouseLook = true, Offset = Vector3.zero },
        new CameraViewSettings
            { View = CameraView.ThirdPerson, UseMouseLook = true, Offset = new Vector3(0f, 0.5f, -4f) },
        new CameraViewSettings
            { View = CameraView.Shoulder, UseMouseLook = true, Offset = new Vector3(0.8f, 0.3f, -2f) },
        new CameraViewSettings
        {
            View = CameraView.Quarter, UseMouseLook = false, Offset = new Vector3(0f, 0f, -12f),
            FixedEulerAngles = new Vector3(45f, 45f, 0f)
        },
        new CameraViewSettings
        {
            View = CameraView.TopDown, UseMouseLook = false, Offset = new Vector3(0f, 0f, -15f),
            FixedEulerAngles = new Vector3(90f, 0f, 0f)
        },
    };

    private CameraRotate _cameraRotate;
    private PlayerRotate _playerRotate;
    private int _currentIndex = 0;

    // 전환 연출용 : 전환을 시작한 순간의 카메라 위치·회전
    private Vector3 _fromPosition;
    private Quaternion _fromRotation;
    private float _transitionElapsed;

    public CameraView CurrentView => _views[_currentIndex].View;

    private void Awake()
    {
        _cameraRotate = GetComponent<CameraRotate>();
        _playerRotate = FindFirstObjectByType<PlayerRotate>();

        if (_player == null && _playerRotate != null) _player = _playerRotate.transform;
        if (_eyePosition == null && _player != null) _eyePosition = _player.Find("CameraPosition");

        if (_player == null) Debug.LogWarning("[CameraViewChanger] No player found");
        if (_eyePosition == null) Debug.LogWarning("[CameraViewChanger] No CameraPosition found");

        // 시작할 때는 전환 연출 없이 바로 첫 번째 시점으로 둔다.
        _transitionElapsed = _transitionDuration;
        ApplyMouseControl();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            ChangeToNextView();
        }
    }

    // 플레이어와 CameraRotate가 Update에서 회전값을 계산한 뒤에 카메라를 옮긴다.
    private void LateUpdate()
    {
        if (_player == null || _eyePosition == null) return;

        // 1. 현재 시점에서 카메라가 있어야 할 위치·회전을 구한다.
        CalculateTargetPose(_views[_currentIndex], out var targetPosition, out var targetRotation);

        // 2. 전환 중이면 시작 위치에서 목표 위치로 부드럽게 옮긴다.
        if (_transitionElapsed < _transitionDuration)
        {
            _transitionElapsed += Time.deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_transitionElapsed / _transitionDuration));
            targetPosition = Vector3.Lerp(_fromPosition, targetPosition, t);
            targetRotation = Quaternion.Slerp(_fromRotation, targetRotation, t);
        }

        // 3. 카메라에 적용한다.
        transform.SetPositionAndRotation(targetPosition, targetRotation);
    }

    private void ChangeToNextView()
    {
        _currentIndex = (_currentIndex + 1) % _views.Length;

        _fromPosition = transform.position;
        _fromRotation = transform.rotation;
        _transitionElapsed = 0f;

        ApplyMouseControl();
        GameLogger.Log($"[CameraViewChanger] View : {CurrentView}");
    }

    // 마우스를 쓰는 시점에서만 플레이어 좌우 회전과 카메라 위아래 회전을 켠다.
    private void ApplyMouseControl()
    {
        var useMouseLook = _views[_currentIndex].UseMouseLook;
        if (_cameraRotate != null) _cameraRotate.enabled = useMouseLook;
        if (_playerRotate != null) _playerRotate.enabled = useMouseLook;
    }

    private void CalculateTargetPose(CameraViewSettings settings, out Vector3 position, out Quaternion rotation)
    {
        if (settings.UseMouseLook)
        {
            // 좌우는 플레이어 방향, 위아래는 마우스로 정한 각도를 쓰고 눈 위치를 기준으로 오프셋만큼 떨어진다.
            var pitch = _cameraRotate != null ? _cameraRotate.Pitch : 0f;
            rotation = Quaternion.Euler(pitch, _player.eulerAngles.y, 0f);
            position = _eyePosition.position + rotation * settings.Offset;
        }
        else
        {
            // 고정 각도로 플레이어를 기준으로 오프셋만큼 떨어진다.
            rotation = Quaternion.Euler(settings.FixedEulerAngles);
            position = _player.position + rotation * settings.Offset;
        }
    }
}