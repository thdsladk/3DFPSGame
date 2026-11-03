using TMPro;
using UnityEngine;

// 역할 : 비밀번호를 입력하다가 정한 시간(5초) 넘게 손을 떼고 있으면, 입력칸 위에 붉은 'HELP ME' 덮개를 씌운다. 다시 입력하면 바로 사라진다.
// 덮개는 보이기만 하고 클릭을 받지 않으므로(CanvasGroup·Graphic 의 Raycast 끔) 입력칸 포커스와 입력은 그대로 된다.
public class UI_PasswordIdleWarning : MonoBehaviour
{
    [SerializeField] private TMP_InputField _input; // 지켜볼 비밀번호 입력칸

    [SerializeField] private CanvasGroup _overlay; // 'HELP ME' 덮개 (불투명 배경이라 아래의 * 를 가린다)
    [SerializeField] private TMP_Text _message; // 깜빡이고 떨리는 'HELP ME' 글자
    [SerializeField] private float _idleSeconds = 5f; // 입력이 없으면 덮개를 씌우기까지 (초)
    [SerializeField] private float _fadeInTime = 0.6f; // 덮개가 나타나는 시간 (초)
    [SerializeField] private float _flickerSpeed = 9f; // 깜빡이는 빠르기
    [SerializeField] private float _minFlickerAlpha = 0.7f; // 깜빡일 때 가장 흐린 정도
    [SerializeField] private float _shakeStrength = 2f; // 글자가 떨리는 폭 (픽셀)

    private float _idleTimer = 0f;
    private Vector2 _messageOrigin;

    private void Awake()
    {
        _messageOrigin = _message.rectTransform.anchoredPosition;
        _overlay.interactable = false;
        _overlay.blocksRaycasts = false;

        // 글자가 바뀔 때마다(입력·지우기) 기다린 시간을 처음부터 다시 잰다.
        _input.onValueChanged.AddListener(_ => HideOverlay());
    }

    private void OnEnable()
    {
        HideOverlay();
    }

    private void Update()
    {
        // 1. 입력한 글자가 없으면 가릴 것도 없다.
        if (string.IsNullOrEmpty(_input.text))
        {
            HideOverlay();
            return;
        }

        // 2. 입력 없이 정한 시간이 지나야 덮개를 씌운다.
        _idleTimer += Time.unscaledDeltaTime;
        if (_idleTimer < _idleSeconds) return;

        // 3. 덮개는 서서히 나타나고, 글자만 불규칙하게 깜빡이며 떨린다. (배경은 그대로라 * 가 비치지 않는다)
        _overlay.alpha = Mathf.Clamp01((_idleTimer - _idleSeconds) / _fadeInTime);
        _message.alpha = Mathf.Lerp(_minFlickerAlpha, 1f, Mathf.PerlinNoise(Time.unscaledTime * _flickerSpeed, 0f));
        _message.rectTransform.anchoredPosition = _messageOrigin + Random.insideUnitCircle * _shakeStrength;
    }

    private void HideOverlay()
    {
        _idleTimer = 0f;
        _overlay.alpha = 0f;
        _message.rectTransform.anchoredPosition = _messageOrigin;
    }
}
