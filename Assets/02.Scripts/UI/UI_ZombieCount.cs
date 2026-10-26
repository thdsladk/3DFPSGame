using TMPro;
using UnityEngine;

// 역할 : 화면 위쪽에 남은 좀비 수를 표시한다. GameManager 의 이벤트를 구독해서 바뀔 때만 갱신한다.
public class UI_ZombieCount : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _countText;


    private void Start()
    {
        // 1. 남은 좀비 수 변경 이벤트를 구독한다.
        GameManager.Instance.OnZombieCountChanged += Refresh;

        // 2. 이미 등록된 좀비가 있을 수 있으니 시작 값을 한 번 그려 준다.
        Refresh();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnZombieCountChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        _countText.text = $"남은 좀비 : {GameManager.Instance.ZombieCount}";
    }
}
