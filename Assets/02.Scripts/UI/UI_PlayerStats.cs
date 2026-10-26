using UnityEngine;
using UnityEngine.UI;

// 역할 : 화면 왼쪽 아래에 플레이어의 체력과 스태미나를 막대(Slider)로 보여 준다.
// 체력은 맞을 때만 바뀌므로 이벤트 구독(옵저버 패턴)으로 목표 값을 고치고, 막대는 부드럽게 줄어든다.
// 스태미나는 거의 매 프레임 바뀌므로 Update 에서 읽는다.
public class UI_PlayerStats : MonoBehaviour
{
    [SerializeField] private Slider _healthSlider;     // 체력 막대
    [SerializeField] private Slider _staminaSlider;    // 스태미나 막대
    [SerializeField] private float _smoothTime = 0.2f; // 체력 막대가 목표 값까지 줄어드는 시간 (초)

    [SerializeField] private Player _player;
    [SerializeField] private PlayerStamina _playerStamina;

    private float _targetHealthValue = 1f; // 체력 막대가 다가갈 값 (0 ~ 1)
    private float _healthChangeSpeed = 0f; // 초당 체력 막대 변화량
    private float _lastStaminaValue = -1f; // 마지막으로 보여 준 스태미나 (-1 이면 첫 프레임에 반드시 고친다)


    private void Start()
    {
        // 1. 플레이어 피격 이벤트를 구독한다.
        _player.OnDamaged += RefreshHealth;

        // 2. 시작 값은 부드럽게 하지 않고 바로 맞춘다.
        _targetHealthValue = _player.Health / _player.MaxHealth;
        _healthSlider.value = _targetHealthValue;
    }

    private void OnDestroy()
    {
        if (_player != null)
        {
            _player.OnDamaged -= RefreshHealth;
        }
    }

    private void Update()
    {
        // 1. 체력 막대를 목표 값까지 부드럽게 줄인다. (이미 같으면 아무것도 바뀌지 않는다)
        if (_healthSlider.value != _targetHealthValue)
        {
            _healthSlider.value = Mathf.MoveTowards(_healthSlider.value, _targetHealthValue,
                _healthChangeSpeed * Time.deltaTime);
        }

        // 2. 스태미나는 값이 바뀌었을 때만 막대를 고친다.
        float staminaValue = _playerStamina.Current / _playerStamina.MaxStamina;
        if (staminaValue != _lastStaminaValue)
        {
            _staminaSlider.value = staminaValue;
            _lastStaminaValue = staminaValue;
        }
    }

    private void RefreshHealth()
    {
        // 목표 값과, _smoothTime 안에 닿을 속도를 정한다.
        _targetHealthValue = _player.Health / _player.MaxHealth;
        _healthChangeSpeed = Mathf.Abs(_healthSlider.value - _targetHealthValue) / _smoothTime;
    }
}
