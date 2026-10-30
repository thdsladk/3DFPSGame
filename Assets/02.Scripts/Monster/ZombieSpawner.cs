using System.Collections;
using UnityEngine;

// 역할 : 플레이어 주변(반경 _spawnRadius)에 좀비를 무리(웨이브)로 내보낸다. 전체 _totalSpawnCount 마리까지 내보낸다.
// 생성 위치는 NavMesh 중 서로 오고 갈 수 있게 이어진 가장 넓은 영역(NavMeshRegion) 안에서만 뽑는다.
// 한 웨이브에 _minWaveCount ~ _maxWaveCount 마리를 짧은 간격으로 한 마리씩 내보내고, 잠시 쉬었다가 다음 웨이브를 보낸다.
// 좀비는 오브젝트 풀에서 꺼내 쓰고, 죽은 좀비는 풀로 돌아와 다음 스폰에 다시 쓰인다.
public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private Zombie _zombiePrefab;

    [Header("수")]
    [SerializeField] private int _totalSpawnCount = 100; // 이 스포너가 내보낼 전체 좀비 수

    [SerializeField] private int _poolSize = 20; // 미리 만들어 둘 좀비 수 (모자라면 풀이 더 만든다)

    [Header("웨이브")]
    [SerializeField] private int _minWaveCount = 10; // 한 웨이브에 내보낼 좀비 수 (최소~최대 사이 무작위)

    [SerializeField] private int _maxWaveCount = 20;
    [SerializeField] private float _firstWaveDelay = 3f; // 게임이 시작되고 첫 웨이브까지 기다리는 시간 (초)
    [SerializeField] private float _minWaveInterval = 15f; // 웨이브가 끝나고 다음 웨이브까지 쉬는 시간 (최소~최대 사이 무작위, 초)
    [SerializeField] private float _maxWaveInterval = 25f;
    [SerializeField] private float _minSpawnGap = 0.1f; // 한 웨이브 안에서 한 마리씩 나오는 간격 (최소~최대 사이 무작위, 초)
    [SerializeField] private float _maxSpawnGap = 0.4f;

    [Header("위치")]
    [SerializeField] private float _spawnRadius = 10f; // 플레이어로부터 이 거리 안에서 생성한다

    [SerializeField] private float _minSpawnDistance = 3f; // 플레이어 바로 옆에 겹쳐 나오지 않도록 이 거리보다는 떨어뜨린다

    private ObjectPool<Zombie> _zombiePool;
    private NavMeshRegion _spawnRegion; // 서로 이어진 가장 넓은 이동 가능 영역
    private Transform _player;
    private int _spawnedCount = 0;

    private void Start()
    {
        if (_zombiePrefab == null)
        {
            GameLogger.LogWarning($"[ZombieSpawner] {name} : 좀비 프리팹이 비어 있습니다.");
            enabled = false;
            return;
        }

        // 1. 구워 둔 NavMesh 에서 서로 이어진 가장 넓은 영역을 찾아 둔다. (게임 중 NavMesh 는 바뀌지 않으므로 한 번만)
        _spawnRegion = new NavMeshRegion();
        if (_spawnRegion.TriangleCount == 0)
        {
            GameLogger.LogWarning($"[ZombieSpawner] {name} : 씬에 구워 둔 NavMesh 가 없습니다.");
            enabled = false;
            return;
        }

        GameLogger.Log($"[ZombieSpawner] 생성 영역 : 삼각형 {_spawnRegion.TriangleCount}개, 넓이 {_spawnRegion.Area:F0}㎡");

        // 2. 좀비 풀을 만든다. 풀에서 만든 좀비는 이 스포너 아래에 모아 둔다.
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        _zombiePool = new ObjectPool<Zombie>(_zombiePrefab, Mathf.Min(_poolSize, _totalSpawnCount), transform);

        // 3. 이번 판에 나올 좀비 수에 이 스포너가 내보낼 수를 미리 더한다. (남은/최대 좀비 수 표시용)
        GameManager.Instance.RegisterZombie(_totalSpawnCount);

        StartCoroutine(SpawnWaves_Coroutine());
    }

    private IEnumerator SpawnWaves_Coroutine()
    {
        var waitPlaying = new WaitUntil(() => GameManager.Instance.State == GameState.Playing);

        // 1. 게임이 시작되고 잠시 뒤 첫 웨이브를 보낸다.
        yield return waitPlaying;
        yield return new WaitForSeconds(_firstWaveDelay);

        while (_spawnedCount < _totalSpawnCount)
        {
            // 2. 이번 웨이브 수를 정하고(남은 수를 넘지 않게) 한 마리씩 간격을 두고 내보낸다.
            int waveCount = Mathf.Min(Random.Range(_minWaveCount, _maxWaveCount + 1), _totalSpawnCount - _spawnedCount);
            for (int i = 0; i < waveCount; i++)
            {
                yield return waitPlaying; // 게임이 멈춰 있으면 다시 시작될 때까지 기다린다

                if (TrySpawn() == false)
                {
                    GameLogger.LogWarning("[ZombieSpawner] 플레이어 주변에 생성할 수 있는 자리가 없어 이번 웨이브를 멈춥니다.");
                    break;
                }

                yield return new WaitForSeconds(Random.Range(_minSpawnGap, _maxSpawnGap));
            }

            GameLogger.Log($"[ZombieSpawner] 웨이브 끝 : {_spawnedCount}/{_totalSpawnCount}");

            // 3. 쉬었다가 다음 웨이브를 보낸다.
            yield return new WaitForSeconds(Random.Range(_minWaveInterval, _maxWaveInterval));
        }
    }

    // 플레이어 주변의 이동 가능한 자리에 한 마리를 내보낸다. 자리가 없으면 false.
    private bool TrySpawn()
    {
        // 1. 가장 넓은 영역 안에서 플레이어로부터 정한 거리 안의 자리를 고른다.
        if (_spawnRegion.TryGetRandomPoint(_player.position, _minSpawnDistance, _spawnRadius, out Vector3 position) == false)
        {
            return false;
        }

        // 2. 플레이어 쪽을 보게 하고, 풀에서 좀비를 꺼내 처음 상태로 준비시킨다.
        Vector3 toPlayer = _player.position - position;
        toPlayer.y = 0f;
        Quaternion rotation = toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity;

        Zombie zombie = _zombiePool.Get(position, rotation);
        zombie.Spawn(_zombiePool);

        _spawnedCount++;
        return true;
    }
}
