using UnityEngine;
using UnityEngine.UI;

// 역할 : ESC 로 일시정지하면 팝업을 띄우고, 버튼으로 계속하기 · 다시 시작 · 게임 종료를 GameManager 에 요청한다.
// 게임 상태가 바뀔 때만 GameManager 의 이벤트를 받아 팝업을 켜고 끈다. (옵저버 패턴)
// 이 컴포넌트는 늘 켜져 있는 오브젝트에 두고, 팝업(_popup)만 켜고 끈다.
public class UI_PausePopup : MonoBehaviour
{
    [SerializeField] private GameObject _popup; // 일시정지 팝업 (어두운 배경 + 창)
    [SerializeField] private Button _continueButton; // 계속하기
    [SerializeField] private Button _restartButton; // 다시 시작
    [SerializeField] private Button _quitButton; // 게임 종료

    private void Start()
    {
        // 1. 버튼마다 GameManager 에 할 일을 맡긴다.
        _continueButton.onClick.AddListener(GameManager.Instance.Continue);
        _restartButton.onClick.AddListener(GameManager.Instance.Restart);
        _quitButton.onClick.AddListener(GameManager.Instance.Quit);

        // 2. 게임 상태 변경 이벤트를 구독하고, 시작 상태에 맞게 한 번 맞춰 둔다.
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
        bool isPaused = GameManager.Instance.State == GameState.Pause;
        if (_popup.activeSelf == isPaused) return;

        _popup.SetActive(isPaused);

        // 커서 위치가 곧 시선이라 평소에는 화면 안에 가둬 두고, 팝업이 떠 있는 동안만 풀어 준다.
        // CursorManager 는 씬을 다시 불러와도 살아 있는 싱글톤이라, 씬에 직접 연결하지 않고 Instance 로 찾는다.
        if (isPaused)
        {
            CursorManager.Instance.UnlockCursor();
        }
        else
        {
            CursorManager.Instance.ConfineCursor();
        }
    }
}
