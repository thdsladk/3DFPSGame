using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_Login : MonoBehaviour
{
    private enum Mode
    {
        Login, // 로그인
        Register, // 회원가입
    }

    [Header("입력 칸")]
    [SerializeField] private TMP_InputField _idInput;

    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private TMP_InputField _passwordConfirmInput; // 회원가입 모드에서만 보인다.

    [Header("버튼")]
    [SerializeField] private Button _loginButton; // 로그인 모드: 로그인

    [SerializeField] private Button _gotoRegisterButton; // 로그인 모드: 회원가입 모드로
    [SerializeField] private Button _registerButton; // 회원가입 모드: 가입하기
    [SerializeField] private Button _gotoLoginButton; // 회원가입 모드: 로그인 모드로

    [Header("기타")]
    [SerializeField] private TextMeshProUGUI _messageText; // 안내 글자

    [SerializeField] private string _gameSceneName = "GameScene"; // 로그인하면 불러올 씬

    private Mode _mode = Mode.Login;

    private void Start()
    {
        Refresh();
    }

    // 지금 모드에 맞는 칸과 버튼만 보여 준다.
    private void Refresh()
    {
        bool isLogin = _mode == Mode.Login;

        _passwordConfirmInput.gameObject.SetActive(!isLogin);
        _loginButton.gameObject.SetActive(isLogin);
        _gotoRegisterButton.gameObject.SetActive(isLogin);
        _registerButton.gameObject.SetActive(!isLogin);
        _gotoLoginButton.gameObject.SetActive(!isLogin);
    }

    private void GotoRegister()
    {
        _mode = Mode.Register;
        _messageText.text = "";
        Refresh();
    }

    private void GotoLogin()
    {
        _mode = Mode.Login;
        _messageText.text = "";
        Refresh();
    }

    // 로그인: 입력을 확인하고, 맞으면 게임 씬을 불러온다.
    private void Login()
    {
        string id = _idInput.text;
        string password = _passwordInput.text;

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            _messageText.text = "아이디와 비밀번호를 입력해 주세요.";
            return;
        }

        if (!AccountStorage.Verify(id, password))
        {
            // 아이디가 없는지 비밀번호가 틀렸는지는 알려 주지 않는다.
            _messageText.text = "아이디 또는 비밀번호가 맞지 않습니다.";
            return;
        }

        SceneLoader.Load(_gameSceneName);
    }

    // 회원가입: 입력을 확인하고, 문제가 없으면 계정을 저장한 뒤 로그인 모드로 돌아간다.
    private void Register()
    {
        string id = _idInput.text;
        string password = _passwordInput.text;
        string passwordConfirm = _passwordConfirmInput.text;


        // 정규표현식 (정규식)
        // 문자열에서 특정한 규칙이나 패턴을 찾기 위해 사용하는 문법 
        // - 데이터 검증 
        // - 문자열 검색
        // - 문자열 치환
        // - 웹 크롤링 / 파싱

        // 규칙이 여기서만 쓰일까 ?
        // 정리 과제
        // - 정규표현식
        // - 명세 패턴 
        // \.text\s*=s*\$?"[^

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            _messageText.text = "아이디와 비밀번호를 입력해 주세요.";
            return;
        }

        if (password != passwordConfirm)
        {
            _messageText.text = "비밀번호 확인이 맞지 않습니다.";
            return;
        }

        if (AccountStorage.Exists(id))
        {
            _messageText.text = "이미 있는 아이디입니다.";
            return;
        }

        AccountStorage.TryRegister(id, password);
        GotoLogin();
        _messageText.text = "가입했습니다. 로그인해 주세요.";
    }


}