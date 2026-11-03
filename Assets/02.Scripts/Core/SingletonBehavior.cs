using UnityEngine;

public class SingletonBehavior<T> : MonoBehaviour where T : class
{
    private static T _instance = null;
    public static T Instance => _instance;


    protected virtual void Awake()
    {
        if (_instance != null)
        {
            // 씬을 다시 불러와 같은 매니저가 또 생겼다. 살아 있는 인스턴스에 새 씬의 것을 넘겨준 뒤 자기는 지운다.
            // (새 씬의 UI 같은 참조는 새로 생긴 쪽만 갖고 있기 때문)
            (_instance as SingletonBehavior<T>).OnSceneInstanceAwake(this as T);
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    // 지금 인스턴스가 살아 있는데 새로 불러온 씬에도 같은 매니저가 있을 때, 살아 있는 쪽에서 불린다.
    // 새 씬의 참조(sceneInstance 의 직렬화 필드)를 넘겨받고 상태를 처음으로 되돌릴 때 재정의한다.
    protected virtual void OnSceneInstanceAwake(T sceneInstance)
    {
    }

    // 지금 인스턴스가 아닌(곧 지워질) 복제본인지
    protected bool IsDuplicate => ReferenceEquals(_instance, this) == false;

    // 씬을 다시 불러오면 예전 인스턴스는 파괴되지만, T 가 class 라서 Unity 의 null 검사가 아니라
    // C# 의 null 검사로 비교되어 _instance 가 null 이 되지 않는다. (새 인스턴스가 자기를 지워 버린다)
    // 그래서 파괴될 때 자기가 등록된 인스턴스면 직접 비워 준다.
    protected virtual void OnDestroy()
    {
        if (_instance == this as T)
        {
            _instance = null;
        }
    }
}