using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// 역할 : 로딩 씬. SceneLoader 가 정해 둔 씬을 비동기로 불러 두고, 연출 시간과 실제 로딩 진행도 중 느린 쪽에 맞춰 점을 하나씩 찍은 뒤 넘어간다.
public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private CanvasGroup _loadingPanel; // 씬이 열릴 때 서서히 나타나는 로딩 화면
    [SerializeField] private TextMeshProUGUI _loadingDots; // 점이 하나씩 늘어나는 글자
    [SerializeField] private int _loadingDotCount = 8; // 점 개수
    [SerializeField] private float _loadingSeconds = 8f; // 로딩 화면을 보여 주는 최소 시간 (초)
    [SerializeField] private float _fadeDuration = 0.25f; // 로딩 화면이 나타나는 시간 (초)
    [SerializeField] private string _defaultSceneName = "GameScene"; // SceneLoader 를 거치지 않고 로딩 씬을 바로 실행했을 때 갈 씬

    private const float LoadedProgress = 0.9f; // allowSceneActivation 이 false 면 progress 가 여기서 멈춘다 (= 다 불러옴)

    private void Start()
    {
        StartCoroutine(Loading_Coroutine());
    }

    private IEnumerator Loading_Coroutine()
    {
        // 1. 다음 씬을 비동기로 불러 두되, 다 불러도 연출이 끝날 때까지는 넘어가지 않게 한다.
        AsyncOperation loading = SceneManager.LoadSceneAsync(SceneLoader.NextSceneName ?? _defaultSceneName);
        loading.allowSceneActivation = false;

        // 2. 로딩 화면을 서서히 띄우고, 연출 시간과 로딩 진행도 중 느린 쪽에 맞춰 점을 하나씩 늘린다.
        float progress = 0f;
        for (float elapsed = 0f; progress < 1f; elapsed += Time.unscaledDeltaTime)
        {
            _loadingPanel.alpha = elapsed / _fadeDuration;
            progress = Mathf.Min(elapsed / _loadingSeconds, loading.progress / LoadedProgress);
            _loadingDots.text = new string('.', Mathf.FloorToInt(progress * _loadingDotCount));
            yield return null;
        }

        // 3. 다음 씬으로 넘어간다.
        loading.allowSceneActivation = true;
    }
}
