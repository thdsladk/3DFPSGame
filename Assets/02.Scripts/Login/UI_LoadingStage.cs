using UnityEngine;

// 역할 : 로딩 화면의 2.5D 연출. 원근이 없는(직교) 카메라로 옆에서 본 3D 장면을 보여 준다.
// 좀비들이 왼쪽에서 플레이어를 쫓아오고, 오른쪽의 플레이어는 계속 총을 쏜다. 맞은 좀비는 쓰러져 땅속으로 가라앉아 사라진 뒤 다시 왼쪽에서 나온다. (계속 반복)
// 게임 로직 없이 모델과 애니메이터만 쓰므로, 애니메이션 이벤트는 꺼 둔다.
public class UI_LoadingStage : MonoBehaviour
{
    [Header("플레이어")]
    [SerializeField] private Animator _playerAnimator;

    [SerializeField] private Transform _muzzle; // 총구 (총선이 여기서 나간다)
    [SerializeField] private GameObject[] _muzzleFlashes; // 쏠 때마다 하나를 잠깐 켠다
    [SerializeField] private LineRenderer _tracer; // 총선
    [SerializeField] private float _fireInterval = 0.12f; // 발사 간격 (초)
    [SerializeField] private float _flashTime = 0.05f; // 섬광·총선이 보이는 시간 (초)

    [Header("좀비")]
    [SerializeField] private Animator[] _zombies; // 좀비 모델의 애니메이터 (부모가 좀비 루트)

    [SerializeField] private float _spawnX = -5.5f; // 좀비가 나오는 왼쪽 끝
    [SerializeField] private float _spawnSpread = 1.8f; // 좀비끼리 띄우는 간격
    [SerializeField] private float _killMinX = -2.5f; // 이 구간 어딘가에서 총에 맞아 쓰러진다
    [SerializeField] private float _killMaxX = 1.2f;
    [SerializeField] private float _walkSpeed = 1.1f; // 쫓아오는 속도 (m/s)
    [SerializeField] private float _deathTime = 1.6f; // 쓰러지는 모션을 보여 주는 시간 (초)
    [SerializeField] private float _sinkTime = 0.8f; // 땅속으로 가라앉는 시간 (초)
    [SerializeField] private float _sinkDepth = 2f; // 가라앉는 깊이

    private const float ChestHeight = 0.4f; // 좀비 루트 기준 가슴 높이 (총선이 닿는 곳)

    private static readonly int MoveMotionHash = Animator.StringToHash("MoveMotion");
    private static readonly int IsFireHash = Animator.StringToHash("IsFire");
    private static readonly int FireSpeedHash = Animator.StringToHash("FireSpeed");
    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int ZombieMoveStateHash = Animator.StringToHash("Base Layer.Move");
    private const string InjuredLayerName = "Injured Layer";

    private enum Phase
    {
        Walk, // 쫓아온다
        Dying, // 맞아서 쓰러진다
        Sinking // 땅속으로 사라진다
    }

    private Phase[] _phases;
    private float[] _timers;
    private float[] _killX; // 좀비마다 쓰러질 위치
    private float _groundY; // 좀비 루트의 원래 높이
    private float _fireTimer = 0f;
    private float _flashTimer = 0f;
    private GameObject _currentFlash;

    private void Awake()
    {
        _phases = new Phase[_zombies.Length];
        _timers = new float[_zombies.Length];
        _killX = new float[_zombies.Length];
        _groundY = _zombies.Length > 0 ? _zombies[0].transform.parent.position.y : 0f;

        // 1. 게임용 애니메이션 이벤트(발사·공격 판정)를 받을 스크립트가 없으므로 이벤트를 끈다.
        _playerAnimator.fireEvents = false;
        foreach (Animator zombie in _zombies)
        {
            zombie.fireEvents = false;
        }
    }

    private void OnEnable()
    {
        // 2. 플레이어는 제자리에서 계속 쏜다. (다친 모션은 섞지 않는다)
        _playerAnimator.SetFloat(MoveMotionHash, 0f);
        _playerAnimator.SetFloat(FireSpeedHash, 1.5f);
        _playerAnimator.SetBool(IsFireHash, true);
        int injuredLayer = _playerAnimator.GetLayerIndex(InjuredLayerName);
        if (injuredLayer >= 0) _playerAnimator.SetLayerWeight(injuredLayer, 0f);

        // 3. 좀비를 왼쪽에 간격을 두고 줄 세운다.
        for (int i = 0; i < _zombies.Length; i++)
        {
            Respawn(i, _spawnX - i * _spawnSpread);
        }

        _tracer.enabled = false;
    }

    private void Update()
    {
        for (int i = 0; i < _zombies.Length; i++)
        {
            UpdateZombie(i);
        }

        UpdateFire();
    }

    private void UpdateZombie(int index)
    {
        Transform root = _zombies[index].transform.parent;
        _timers[index] += Time.deltaTime;

        switch (_phases[index])
        {
            case Phase.Walk:
                root.position += Vector3.right * (_walkSpeed * Time.deltaTime);
                if (root.position.x >= _killX[index])
                {
                    // 총에 맞아 쓰러진다.
                    _zombies[index].SetTrigger(DeathHash);
                    SetPhase(index, Phase.Dying);
                }

                break;

            case Phase.Dying:
                if (_timers[index] >= _deathTime) SetPhase(index, Phase.Sinking);
                break;

            case Phase.Sinking:
                root.position += Vector3.down * (_sinkDepth / _sinkTime * Time.deltaTime);
                if (_timers[index] >= _sinkTime)
                {
                    // 다 사라지면 줄의 맨 뒤(가장 왼쪽)로 돌아가 다시 쫓아온다.
                    Respawn(index, Mathf.Min(_spawnX, GetLastZombieX() - _spawnSpread));
                }

                break;
        }
    }

    // 정한 간격마다 가장 가까이 온 좀비에게 총선을 긋고 총구 섬광을 잠깐 켠다.
    private void UpdateFire()
    {
        _flashTimer -= Time.deltaTime;
        if (_flashTimer <= 0f)
        {
            _tracer.enabled = false;
            if (_currentFlash != null) _currentFlash.SetActive(false);
        }

        _fireTimer += Time.deltaTime;
        if (_fireTimer < _fireInterval) return;
        _fireTimer = 0f;

        Transform target = GetNearestWalkingZombie();
        Vector3 end = target != null ? target.position + Vector3.up * ChestHeight : _muzzle.position + Vector3.left * 10f;
        _tracer.SetPosition(0, _muzzle.position);
        _tracer.SetPosition(1, end);
        _tracer.enabled = true;

        if (_muzzleFlashes.Length > 0)
        {
            _currentFlash = _muzzleFlashes[Random.Range(0, _muzzleFlashes.Length)];
            _currentFlash.SetActive(true);
        }

        _flashTimer = _flashTime;
    }

    private void Respawn(int index, float x)
    {
        Transform root = _zombies[index].transform.parent;
        root.position = new Vector3(x, _groundY, root.position.z);
        _killX[index] = Random.Range(_killMinX, _killMaxX);

        // 쓰러진 자세를 지우고 걷는 모션부터 다시 시작한다.
        _zombies[index].Rebind();
        _zombies[index].Play(ZombieMoveStateHash, 0, Random.value);
        SetPhase(index, Phase.Walk);
    }

    private void SetPhase(int index, Phase phase)
    {
        _phases[index] = phase;
        _timers[index] = 0f;
    }

    private Transform GetNearestWalkingZombie()
    {
        Transform nearest = null;
        for (int i = 0; i < _zombies.Length; i++)
        {
            if (_phases[i] != Phase.Walk) continue;

            Transform root = _zombies[i].transform.parent;
            if (nearest == null || root.position.x > nearest.position.x) nearest = root;
        }

        return nearest;
    }

    // 걷고 있는 좀비 중 가장 왼쪽 위치 (새로 나올 좀비가 그 뒤에 줄을 선다)
    private float GetLastZombieX()
    {
        float last = float.MaxValue;
        for (int i = 0; i < _zombies.Length; i++)
        {
            if (_phases[i] != Phase.Walk) continue;
            last = Mathf.Min(last, _zombies[i].transform.parent.position.x);
        }

        return last == float.MaxValue ? _spawnX : last;
    }
}
