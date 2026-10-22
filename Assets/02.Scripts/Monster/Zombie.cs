using System.Collections;
using UnityEngine;

// FSM(Finite State Machine/유한 상태 머신) 유한한 상태가 있고, 상태마다 행동을 다르게 함으로써 AI를 구현하는 유명한 방법 
// - 상태 : 객체가 가질 수 있는 어떤 조건이나 상황
// - 전이 : 조건에 따라 현재 상태에서 다른 상태로 넘어가는 것 
// - 그리고 위 방식에 따라 규칙을 세워둔 것을 "머신" 

// 좀비가 가질 수 있는 상태 
public enum ZombieState
{
    Idle, // 대기 : 제자리에서 플레이어를 기다린다.
    Trace, // 추격 : 플레이어를 쫓아간다.
    Comeback, // 복귀 : 처음 자리로 돌아간다.
    Attack, // 공격 : 일정 시간마다 플레이어를 때린다.
    Hit, // 피격 : 공격(총알, 폭탄)을 받아 잠깐 멈칫한다.
    Death // 죽음 : 쓰러지고 잠시 뒤 사라진다. 
}

public class Zombie : MonoBehaviour
{
    [Header("Stat")]
    [SerializeField] private float _detectDistance = 8f;

    [SerializeField] private float _comebackDistance = 15f;
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackInterval = 1.2f;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _health = 100f;

    private float _attackTimer = 0f;
    private CharacterController _controller;
    private Vector3 _startPosition;
    private GameObject _player;

    private ZombieState _state = ZombieState.Idle;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _startPosition = transform.position;
        _player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        switch (_state)
        {
            case ZombieState.Idle:
            {
                Idle();
                break;
            }
            case ZombieState.Trace:
            {
                Trace();
                break;
            }
            case ZombieState.Comeback:
            {
                Comback();
                break;
            }
            case ZombieState.Attack:
            {
                Attack();
                break;
            }
            default:
            {
                break;
            }
        }
    }


    private void Idle()
    {
        // 대기 : 제자리에서 플레이어를 기다린다.

        // 플레이어 가까워지면 Trace
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance <= _detectDistance)
        {
            GameLogger.Log($"{_state} -> Trace ");
            _state = ZombieState.Trace;
        }
    }

    private void Trace()
    {
        // Todo. Run 애니메이션 실행 

        if (Vector3.Distance(transform.position, _startPosition) > _comebackDistance)
        {
            GameLogger.Log($"{_state} -> Comeback");
            _state = ZombieState.Comeback;
            return;
        }

        // 플레이어가 공격 거리 안이면 공격한다. 
        if (Vector3.Distance(transform.position, _player.transform.position) <= _attackRange)
        {
            _attackTimer = 0f;

            GameLogger.Log($"{_state} -> Attack");
            _state = ZombieState.Attack;
            return;
        }

        Vector3 direction = _player.transform.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    private void Comback()
    {
        if (Vector3.Distance(transform.position, _startPosition) < 1f)
        {
            GameLogger.Log($"{_state} -> Idle");
            _state = ZombieState.Idle;
            return;
        }

        Vector3 direction = _startPosition - transform.position;
        direction.y = 0f;
        direction.Normalize();

        transform.forward = direction;
        _controller.SimpleMove(direction * _moveSpeed);
    }

    private void Attack()
    {
        // 플레이어가 공격 사정 거리를 벗어나면 
        float distance = Vector3.Distance(transform.position, _player.transform.position);
        if (distance > _attackRange)
        {
            GameLogger.Log($"{_state} -> Trace ");
            _state = ZombieState.Trace;
        }

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _attackInterval)
        {
            // Todo. 공격 애니메이션 실행
            // Todo. 플레이러르 때려 !
            //GameLogger.Log("플레이어를 때렸습니다.");
            _attackTimer = 0f;
        }
    }

    public void TakeDamage(float damage)
    {
        if (_state == ZombieState.Death)
        {
            return;
        }

        _health -= damage;

        StopAllCoroutines();

        if (_health > 0f)
        {
            StartCoroutine(Hit_Coroutine());
        }
        else
        {
            StartCoroutine(Death_Coroutine());
        }
    }

    private IEnumerator Hit_Coroutine()
    {
        GameLogger.Log($"{_state} -> Hit ");
        _state = ZombieState.Hit; // 잠깐 멈칫 한다.

        yield return new WaitForSeconds(0.3f);

        _state = ZombieState.Trace;
    }

    private IEnumerator Death_Coroutine()
    {
        GameLogger.Log($"{_state} -> Death ");
        _state = ZombieState.Death; // 사망.

        yield return new WaitForSeconds(0.5f);
    }
}