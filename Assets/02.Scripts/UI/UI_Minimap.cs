using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 역할 : 미니맵 패널 위쪽에 지역 이름을 보여 주고, +/- 버튼을 누르면 미니맵 카메라를 확대/축소한다.
public class UI_Minimap : MonoBehaviour
{
    [SerializeField] private MinimapCamera _minimapCamera;
    [SerializeField] private TextMeshProUGUI _mapNameText; // 지역 이름 글자
    [SerializeField] private Button _zoomInButton; // + 버튼
    [SerializeField] private Button _zoomOutButton; // - 버튼

    private void Awake()
    {
        // 지금 씬의 이름을 지역 이름으로 보여 준다.
        _mapNameText.text = SceneManager.GetActiveScene().name;
    }

    private void OnEnable()
    {
        _zoomInButton.onClick.AddListener(_minimapCamera.ZoomIn);
        _zoomOutButton.onClick.AddListener(_minimapCamera.ZoomOut);
    }

    private void OnDisable()
    {
        _zoomInButton.onClick.RemoveListener(_minimapCamera.ZoomIn);
        _zoomOutButton.onClick.RemoveListener(_minimapCamera.ZoomOut);
    }
}
