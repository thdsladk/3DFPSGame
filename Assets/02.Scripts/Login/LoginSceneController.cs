using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 역할 : 로그인 씬의 화면 흐름. 로그인 ⇄ 회원가입 패널을 바꾸고, 입력을 검사해 안내 문구를 띄우며, 로그인하면 로딩 화면을 보여 준 뒤 게임 씬으로 넘어간다.
// 계정 저장·확인은 AccountStorage 가 맡는다.
public class LoginSceneController : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private CanvasGroup _loginPanel;

    [SerializeField] private CanvasGroup _signUpPanel;
    [SerializeField] private CanvasGroup _loadingPanel;
    [SerializeField] private float _fadeDuration = 0.25f; // 패널이 바뀔 때 흐려졌다 나타나는 시간 (초)

    [Header("로그인")]
    [SerializeField] private TMP_InputField _loginIdInput;

    [SerializeField] private TMP_InputField _loginPasswordInput;
    [SerializeField] private TextMeshProUGUI _loginMessage;
    [SerializeField] private Button _loginButton;
    [SerializeField] private Button _openSignUpButton;

    [Header("회원가입")]
    [SerializeField] private TMP_InputField _signUpIdInput;

    [SerializeField] private TMP_InputField _signUpPasswordInput;
    [SerializeField] private TMP_InputField _signUpConfirmInput;
    [SerializeField] private TextMeshProUGUI _signUpMessage;
    [SerializeField] private Button _signUpButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private float _signUpDoneDelay = 1f; // 가입 완료 문구를 보여 준 뒤 로그인으로 돌아가기까지 (초)

    [Header("로딩")]
    [SerializeField] private TextMeshProUGUI _loadingDots; // 점이 하나씩 늘어나는 글자

    [SerializeField] private int _loadingDotCount = 8; // 점 개수
    [SerializeField] private float _loadingSeconds = 8f; // 로딩 화면을 보여 주는 시간 (초)
    [SerializeField] private string _gameSceneName = "GameScene";

    [Header("안내 문구 색")]
    [SerializeField] private Color _infoColor = new Color(1f, 0.82f, 0.25f); // 안내 (노랑)

    [SerializeField] private Color _errorColor = new Color(1f, 0.35f, 0.3f); // 잘못됨 (빨강)
    [SerializeField] private Color _okColor = new Color(0.4f, 0.95f, 0.45f); // 맞음 (초록)

    [Header("흔들림")]
    [SerializeField] private float _shakeDuration = 0.3f; // 잘못 입력했을 때 문구가 흔들리는 시간 (초)

    [SerializeField] private float _shakeStrength = 10f; // 흔들리는 폭 (픽셀)

    private const string EnterIdAndPasswordText = "아이디와 비밀번호를 입력해 주세요.";
    private const string InvalidIdText = "아이디는 영문자(대/소문자 가능)랑 숫자만 가능합니다.";
    private const string InvalidPasswordText = "비밀번호는 영문자(대/소문자 가능)랑 숫자만 가능합니다.";
    private const string PasswordMismatchText = "비밀번호가 확인과 일치하지 않습니다.";
    private const string PasswordMatchText = "비밀번호가 일치합니다.";
    private const string DuplicateIdText = "이미 사용 중인 아이디입니다.";
    private const string SignUpDoneText = "회원가입이 완료되었습니다!";
    private const string LoginFailText = "아이디 또는 비밀번호가 맞지 않습니다.";

    private Coroutine _shakeCoroutine;
    private bool _isBusy = false; // 가입 완료 대기나 로딩 중이라 버튼 입력을 막는지

    private void Start()
    {
        // 1. 버튼마다 할 일을 연결한다.
        _loginButton.onClick.AddListener(Login);
        _openSignUpButton.onClick.AddListener(OpenSignUp);
        _signUpButton.onClick.AddListener(SignUp);
        _backButton.onClick.AddListener(OpenLogin);

        // 2. 회원가입 입력이 바뀔 때마다 바로 검사해 안내 문구를 고친다.
        _signUpIdInput.onValueChanged.AddListener(_ => RefreshSignUpMessage());
        _signUpPasswordInput.onValueChanged.AddListener(_ => RefreshSignUpMessage());
        _signUpConfirmInput.onValueChanged.AddListener(_ => RefreshSignUpMessage());

        // 3. 로그인 패널만 보이게 시작한다.
        SetPanel(_signUpPanel, false);
        SetPanel(_loadingPanel, false);
        SetPanel(_loginPanel, true);
        _loginMessage.text = string.Empty;
    }

    private void Update()
    {
        // Enter 로 지금 패널의 확인 버튼을 누른다.
        if (_isBusy || Input.GetKeyDown(KeyCode.Return) == false) return;

        if (_loginPanel.interactable) Login();
        else if (_signUpPanel.interactable) SignUp();
    }

    private void Login()
    {
        if (_isBusy) return;

        string id = _loginIdInput.text;
        string password = _loginPasswordInput.text;

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            ShowMessage(_loginMessage, EnterIdAndPasswordText, _infoColor, true);
            return;
        }

        if (AccountStorage.Verify(id, password) == false)
        {
            ShowMessage(_loginMessage, LoginFailText, _errorColor, true);
            return;
        }

        StartCoroutine(Loading_Coroutine());
    }

    private void SignUp()
    {
        if (_isBusy) return;

        // 1. 형식·일치 여부를 다시 확인한다. 틀렸으면 문구를 흔들어 알린다.
        if (RefreshSignUpMessage() == false)
        {
            Shake(_signUpMessage);
            return;
        }

        // 2. 이미 있는 아이디면 가입하지 않는다.
        string id = _signUpIdInput.text;
        if (AccountStorage.TryRegister(id, _signUpPasswordInput.text) == false)
        {
            ShowMessage(_signUpMessage, DuplicateIdText, _errorColor, true);
            return;
        }

        StartCoroutine(SignUpDone_Coroutine(id));
    }

    // 회원가입 입력을 검사해 안내 문구를 고치고, 가입할 수 있는 상태인지 돌려준다.
    // 순서 : 비어 있음(노랑) → 아이디 형식 → 비밀번호 형식 → 비밀번호 확인 불일치(빨강) → 일치(초록)
    private bool RefreshSignUpMessage()
    {
        string id = _signUpIdInput.text;
        string password = _signUpPasswordInput.text;
        string confirm = _signUpConfirmInput.text;

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            ShowMessage(_signUpMessage, EnterIdAndPasswordText, _infoColor, false);
            return false;
        }

        if (AccountStorage.IsValidFormat(id) == false)
        {
            ShowMessage(_signUpMessage, InvalidIdText, _errorColor, false);
            return false;
        }

        if (AccountStorage.IsValidFormat(password) == false)
        {
            ShowMessage(_signUpMessage, InvalidPasswordText, _errorColor, false);
            return false;
        }

        if (password != confirm)
        {
            ShowMessage(_signUpMessage, PasswordMismatchText, _errorColor, false);
            return false;
        }

        ShowMessage(_signUpMessage, PasswordMatchText, _okColor, false);
        return true;
    }

    private void OpenSignUp()
    {
        if (_isBusy) return;

        _signUpIdInput.text = string.Empty;
        _signUpPasswordInput.text = string.Empty;
        _signUpConfirmInput.text = string.Empty;
        RefreshSignUpMessage();
        StartCoroutine(SwitchPanel_Coroutine(_loginPanel, _signUpPanel));
    }

    private void OpenLogin()
    {
        if (_isBusy) return;

        _loginMessage.text = string.Empty;
        StartCoroutine(SwitchPanel_Coroutine(_signUpPanel, _loginPanel));
    }

    // 가입 완료 문구를 잠시 보여 준 뒤, 가입한 아이디를 채워 로그인 패널로 돌아간다.
    private IEnumerator SignUpDone_Coroutine(string id)
    {
        _isBusy = true;
        ShowMessage(_signUpMessage, SignUpDoneText, _okColor, false);
        yield return new WaitForSeconds(_signUpDoneDelay);

        _loginIdInput.text = id;
        _loginPasswordInput.text = string.Empty;
        _loginMessage.text = string.Empty;
        yield return SwitchPanel_Coroutine(_signUpPanel, _loginPanel);
        _isBusy = false;
    }

    // 로딩 화면 : 게임 씬을 미리 불러 두고, 정한 시간 동안 점을 하나씩 찍은 뒤 넘어간다.
    private IEnumerator Loading_Coroutine()
    {
        _isBusy = true;
        _loadingDots.text = string.Empty;
        yield return SwitchPanel_Coroutine(_loginPanel, _loadingPanel);

        // 1. 게임 씬을 뒤에서 불러 두되, 로딩 연출이 끝날 때까지 넘어가지 않게 한다.
        AsyncOperation loading = SceneManager.LoadSceneAsync(_gameSceneName);
        loading.allowSceneActivation = false;

        // 2. 점을 하나씩 늘려 가운데에 찍는다. (점 개수만큼 시간을 나눈다)
        var wait = new WaitForSeconds(_loadingSeconds / _loadingDotCount);
        for (int i = 1; i <= _loadingDotCount; i++)
        {
            yield return wait;
            _loadingDots.text = new string('.', i);
        }

        // 3. 게임 씬으로 넘어간다.
        loading.allowSceneActivation = true;
    }

    // 지금 패널을 흐리게 지우고 다음 패널을 나타나게 한다.
    private IEnumerator SwitchPanel_Coroutine(CanvasGroup from, CanvasGroup to)
    {
        from.interactable = false;
        for (float t = 0f; t < _fadeDuration; t += Time.unscaledDeltaTime)
        {
            from.alpha = 1f - t / _fadeDuration;
            yield return null;
        }

        SetPanel(from, false);

        to.alpha = 0f;
        to.gameObject.SetActive(true);
        for (float t = 0f; t < _fadeDuration; t += Time.unscaledDeltaTime)
        {
            to.alpha = t / _fadeDuration;
            yield return null;
        }

        SetPanel(to, true);
    }

    private static void SetPanel(CanvasGroup panel, bool isVisible)
    {
        panel.gameObject.SetActive(isVisible);
        panel.alpha = isVisible ? 1f : 0f;
        panel.interactable = isVisible;
        panel.blocksRaycasts = isVisible;
    }

    private void ShowMessage(TextMeshProUGUI message, string text, Color color, bool isShake)
    {
        message.text = text;
        message.color = color;
        if (isShake) Shake(message);
    }

    // 잘못 입력했을 때 문구를 좌우로 흔들어 눈에 띄게 한다.
    private void Shake(TextMeshProUGUI message)
    {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(Shake_Coroutine(message.rectTransform));
    }

    private IEnumerator Shake_Coroutine(RectTransform target)
    {
        Vector2 origin = target.anchoredPosition;
        for (float t = 0f; t < _shakeDuration; t += Time.unscaledDeltaTime)
        {
            float strength = _shakeStrength * (1f - t / _shakeDuration);
            target.anchoredPosition = origin + Vector2.right * (Mathf.Sin(t * 60f) * strength);
            yield return null;
        }

        target.anchoredPosition = origin;
        _shakeCoroutine = null;
    }
}
