using UnityEngine;
using UnityEngine.UI;

// 역할 : 플레이어가 쓰러져 게임 오버가 되면 팝업을 띄우고, 버튼으로 다시 시작 · 게임 종료를 GameManager 에 요청한다.
// 게임 상태가 바뀔 때만 GameManager 의 이벤트를 받아 팝업을 켠다. (옵저버 패턴)
// 이 컴포넌트는 늘 켜져 있는 오브젝트에 두고, 팝업(_popup)만 켜고 끈다.
public class UI_GameOverPopup : MonoBehaviour
{
    [SerializeField] private GameObject _popup; // 게임 오버 팝업 (어두운 배경 + 창)
    [SerializeField] private Button _restartButton; // 다시 시작
    [SerializeField] private Button _quitButton; // 게임 종료

    private void Start()
    {
        // 1. 버튼마다 GameManager 에 할 일을 맡긴다.
        _restartButton.onClick.AddListener(GameManager.Instance.Restart);
        _quitButton.onClick.AddListener(GameManager.Instance.Quit);

        // 2. 게임 상태 변경 이벤트를 구독하고, 처음에는 숨겨 둔다.
        GameManager.Instance.OnStateChanged += Refresh;
        _popup.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        if (GameManager.Instance.State != GameState.GameOver || _popup.activeSelf) return;

        // 게임 오버 팝업을 띄우고, 버튼을 누를 수 있게 커서를 풀어 준다. (다시 시작하면 씬이 새로 열리며 커서도 처음 상태로 돌아간다)
        _popup.SetActive(true);
        CursorManager.Instance.UnlockCursor();
    }
}
