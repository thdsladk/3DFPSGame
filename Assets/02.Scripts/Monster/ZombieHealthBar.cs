using UnityEngine;
using UnityEngine.UI;

// 역할 : 좀비 머리 위 체력바. 좀비의 체력을 막대로 보여 주고, 늘 화면 쪽을 보게 한다.
// 체력은 좀비의 이벤트를 구독해서 바뀔 때만 목표 값을 고치고, 막대는 목표 값까지 부드럽게 줄어든다.
public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;          // 체력 막대
    [SerializeField] private float _smoothTime = 0.2f; // 막대가 목표 값까지 줄어드는 시간 (초)

    private Zombie _zombie;
    private Transform _cameraTransform;

    private float _targetValue = 1f;  // 막대가 다가갈 값 (0 ~ 1)
    private float _changeSpeed = 0f;  // 초당 막대 변화량

    private void Awake()
    {
        // 체력바의 부모(좀비)에서 Zombie를 찾는다.
        _zombie = GetComponentInParent<Zombie>();
    }

    private void Start()
    {
        _cameraTransform = Camera.main.transform;

        // 1. 좀비 체력 변경 이벤트를 구독한다.
        _zombie.OnHealthChanged += Refresh;

        // 2. 시작 값은 부드럽게 하지 않고 바로 맞춘다.
        _targetValue = _zombie.Health / _zombie.MaxHealth;
        _slider.value = _targetValue;
        Refresh();
    }

    private void OnDestroy()
    {
        if (_zombie != null)
        {
            _zombie.OnHealthChanged -= Refresh;
        }
    }

    private void LateUpdate()
    {
        // 1. 막대를 목표 값까지 부드럽게 줄인다.
        _slider.value = Mathf.MoveTowards(_slider.value, _targetValue, _changeSpeed * Time.deltaTime);

        // 2. 빌보드: 체력바가 카메라와 같은 쪽을 보게 한다.
        _slider.transform.forward = _cameraTransform.forward;
    }

    private void Refresh()
    {
        // 1. 목표 값과, _smoothTime 안에 닿을 속도를 정한다.
        _targetValue = _zombie.Health / _zombie.MaxHealth;
        _changeSpeed = Mathf.Abs(_slider.value - _targetValue) / _smoothTime;

        // 2. 체력이 가득 차 있거나 죽었으면 숨기고, 맞았으면 보인다.
        bool isVisible = _zombie.Health > 0f && _zombie.Health < _zombie.MaxHealth;
        _slider.gameObject.SetActive(isVisible);
    }
}
