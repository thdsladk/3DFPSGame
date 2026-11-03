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

public class GameManager : SingletonBehavior<GameManager>
{
    private GameState _state = GameState.Ready;
    public GameState State => _state;

    [SerializeField] private TextMeshProUGUI _stateText;

    private int _maxZombieCount = 0; // 이번 판에 나올 전체 좀비 수 (씬에 놓인 좀비 + 스포너가 만들 좀비)
    private int _killedZombieCount = 0; // 지금까지 죽은 좀비 수

    public int MaxZombieCount => _maxZombieCount;
    public int ZombieCount => _maxZombieCount - _killedZombieCount; // 남은 좀비 수 (아직 스폰되지 않은 좀비 포함)

    // 남은 좀비 수나 최대 좀비 수가 바뀔 때마다 알린다. (UI_ZombieCount 가 구독)
    public event Action OnZombieCountChanged;

    // 게임 상태(준비·플레이·일시정지·게임오버)가 바뀔 때마다 알린다. (UI_PausePopup, UI_GameOverPopup 이 구독)
    public event Action OnStateChanged;


    private void Start()
    {
        // 씬을 다시 불러와 생긴 복제본은 곧 지워지므로 시작하지 않는다. (살아 있는 쪽이 OnSceneInstanceAwake 에서 새 판을 시작한다)
        if (IsDuplicate) return;

        StartCoroutine(StartSequence_Coroutine());
    }

    // 다시 시작으로 씬을 새로 불러오면 불린다. 새 씬의 UI 를 넘겨받고, 이전 판의 상태·좀비 수를 지운 뒤 새 판을 시작한다.
    protected override void OnSceneInstanceAwake(GameManager sceneInstance)
    {
        // 1. 이전 씬의 UI 는 지워졌으므로 새 씬의 UI 로 바꾼다.
        _stateText = sceneInstance._stateText;

        // 2. 이전 판에서 하던 일과 값을 처음으로 되돌린다.
        StopAllCoroutines();
        Time.timeScale = 1f;
        _maxZombieCount = 0;
        _killedZombieCount = 0;
        _state = GameState.Ready;

        // 3. 준비 → 게임 시작 순서를 다시 진행한다.
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

    // 플레이어가 쓰러지면 호출한다. 화면 표시는 게임 오버 팝업(UI_GameOverPopup)이 맡는다.
    public void GameOver()
    {
        ChangeState(GameState.GameOver);
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