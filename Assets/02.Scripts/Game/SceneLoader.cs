using UnityEngine;
using UnityEngine.SceneManagement;

// 역할 : 씬 이동 창구. 가려는 씬 이름을 기억해 두고 로딩 씬을 먼저 연다. 로딩 씬이 NextSceneName 을 비동기로 불러 넘어간다.
public static class SceneLoader
{
    private const string LoadingSceneName = "LoadingScene";

    public static string NextSceneName { get; private set; }

    public static void Load(string sceneName)
    {
        // 빌드 설정에 없는 씬이면 로딩 화면에서 멈춰 버리므로 미리 막는다.
        if (Application.CanStreamedLevelBeLoaded(sceneName) == false)
        {
            GameLogger.LogError($"[SceneLoader] 빌드 설정에 없는 씬입니다 : {sceneName}");
            return;
        }

        NextSceneName = sceneName;
        SceneManager.LoadScene(LoadingSceneName);
    }
}
