using UnityEngine;

// 역할 : 정해 둔 스폰 지점 중 한 곳에 일정 간격으로 좀비를 내보낸다. 전체 _totalSpawnCount 마리까지 내보낸다.
// 좀비는 오브젝트 풀에서 꺼내 쓰고, 죽은 좀비는 풀로 돌아와 다음 스폰에 다시 쓰인다.
public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private Zombie _zombiePrefab;
    [SerializeField] private Transform[] _spawnPoints; // 좀비가 나올 위치 (NavMesh 위에 둔다)
    [SerializeField] private int _totalSpawnCount = 10; // 이 스포너가 내보낼 전체 좀비 수
    [SerializeField] private float _spawnInterval = 3f; // 스폰 간격 (초)
    [SerializeField] private int _poolSize = 5; // 미리 만들어 둘 좀비 수

    private ObjectPool<Zombie> _zombiePool;
    private int _spawnedCount = 0;
    private float _spawnTimer = 0f;

    private void Start()
    {
        if (_zombiePrefab == null || _spawnPoints == null || _spawnPoints.Length == 0)
        {
            GameLogger.LogWarning($"[ZombieSpawner] {name} : 좀비 프리팹이나 스폰 지점이 비어 있습니다.");
            enabled = false;
            return;
        }

        // 1. 좀비 풀을 만든다. 풀에서 만든 좀비는 이 스포너 아래에 모아 둔다.
        _zombiePool = new ObjectPool<Zombie>(_zombiePrefab, Mathf.Min(_poolSize, _totalSpawnCount), transform);

        // 2. 이번 판에 나올 좀비 수에 이 스포너가 내보낼 수를 미리 더한다. (남은/최대 좀비 수 표시용)
        GameManager.Instance.RegisterZombie(_totalSpawnCount);

        // 3. 게임이 시작되면 첫 좀비는 바로 나오게 한다.
        _spawnTimer = _spawnInterval;
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing) return;
        if (_spawnedCount >= _totalSpawnCount) return;

        // 스폰 간격마다 한 마리씩 내보낸다.
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;
            Spawn();
        }
    }

    private void Spawn()
    {
        // 1. 스폰 지점 중 하나를 고른다.
        Transform spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];

        // 2. 풀에서 좀비를 꺼내 그 자리에 두고, 처음 상태로 준비시킨다.
        Zombie zombie = _zombiePool.Get(spawnPoint.position, spawnPoint.rotation);
        zombie.Spawn(_zombiePool);

        _spawnedCount++;
        GameLogger.Log($"[ZombieSpawner] 좀비 스폰 {_spawnedCount}/{_totalSpawnCount}");
    }
}
