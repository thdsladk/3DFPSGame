using System;
using System.Collections;
using TMPro;
using UnityEngine;

// 역할 : 게임의 진행 상태(준비 → 플레이 → 게임오버)와 남은 좀비 수를 관리한다.
public enum GameState
{
    Ready,
    Playing,
    GameOver
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

    private IEnumerator StartSequence_Coroutine()
    {
        _stateText.gameObject.SetActive(true);
        _stateText.text = "준비";
        yield return new WaitForSeconds(1.5f);
        _stateText.text = "게임 시작!";
        yield return new WaitForSeconds(0.5f);
        _stateText.gameObject.SetActive(false);

        _state = GameState.Playing;
    }

    public void GameOver()
    {
        _state = GameState.GameOver;

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
