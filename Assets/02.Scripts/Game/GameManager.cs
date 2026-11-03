using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 역할 : 게임의 진행 상태(준비 → 플레이 ⇄ 일시정지 → 게임오버)와 남은 좀비 수를 관리한다.
public enum GameState
{
    Ready,
    Playing,
    GameOver,
    Pause
}

public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance => _instance;

    private GameState _state = GameState.Ready;
    public GameState State => _state;

    [SerializeField] private TextMeshProUGUI _stateText;

    private int _maxZombieCount = 0; // 이번 판에 나올 전체 좀비 수 (씬에 놓인 좀비 + 스포너가 만들 좀비)
    private int _killedZombieCount = 0; // 지금까지 죽은 좀비 수

    public int MaxZombieCount => _maxZombieCount;
    public int ZombieCount => _maxZombieCount - _killedZombieCount; // 남은 좀비 수 (아직 스폰되지 않은 좀비 포함)

    // 남은 좀비 수나 최대 좀비 수가 바뀔 때마다 알린다. (UI_ZombieCount 가 구독)
    public event Action OnZombieCountChanged;

    // 게임 상태(준비·플레이·일시정지·게임오버)가 바뀔 때마다 알린다. (UI_PausePopup 이 구독)
    public event Action OnStateChanged;


    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        StartCoroutine(StartSequence_Coroutine());
    }

    private void Update()
    {
        // ESC 로 일시정지하고, 일시정지 중에 다시 누르면 이어서 한다.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_state == GameState.Playing)
            {
                Pause();
            }
            else if (_state == GameState.Pause)
            {
                Continue();
            }
        }
    }

    // 시간을 멈추고 일시정지 상태로 간다. (UI_PausePopup 이 팝업을 띄운다)
    public void Pause()
    {
        if (_state != GameState.Playing) return;

        Time.timeScale = 0f;
        ChangeState(GameState.Pause);
    }

    // 일시정지를 풀고 이어서 한다. (팝업의 계속하기 버튼, ESC)
    public void Continue()
    {
        if (_state != GameState.Pause) return;

        Time.timeScale = 1f;
        ChangeState(GameState.Playing);
    }

    // 지금 씬을 처음부터 다시 불러온다. 멈춘 시간은 되돌린다.
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 게임을 끝낸다. 에디터에서는 플레이 모드를 멈춘다.
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ChangeState(GameState nextState)
    {
        _state = nextState;
        OnStateChanged?.Invoke();
    }

    private IEnumerator StartSequence_Coroutine()
    {
        _stateText.gameObject.SetActive(true);
        _stateText.text = "준비";
        yield return new WaitForSeconds(1.5f);
        _stateText.text = "게임 시작!";
        yield return new WaitForSeconds(0.5f);
        _stateText.gameObject.SetActive(false);

        ChangeState(GameState.Playing);
    }

    public void GameOver()
    {
        ChangeState(GameState.GameOver);

        _stateText.gameObject.SetActive(true);
        _stateText.text = "게임오버";
        _stateText.color = Color.red;
    }

    // 이번 판에 나올 좀비 수를 더한다. 씬에 놓인 좀비는 1, 스포너는 만들 전체 수를 넘긴다.
    public void RegisterZombie(int count = 1)
    {
        _maxZombieCount += count;
        OnZombieCountChanged?.Invoke();
    }

    // 좀비가 죽으면 호출한다.
    public void UnregisterZombie()
    {
        _killedZombieCount = Mathf.Min(_maxZombieCount, _killedZombieCount + 1);
        OnZombieCountChanged?.Invoke();
    }
}