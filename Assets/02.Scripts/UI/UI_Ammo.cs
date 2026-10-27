using TMPro;
using UnityEngine;

// 역할 : AmmoPanel 의 AmmoText 에 지금 총알 수를 보여 준다.
// 총알은 쏘거나 장전할 때만 바뀌므로, 체력 막대처럼 이벤트를 구독(옵저버 패턴)해서 바뀔 때만 글자를 고친다.
public class UI_Ammo : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _ammoText; // 총알 수 글자 (AmmoPanel/AmmoText)
    [SerializeField] private PlayerGunFire _playerGunFire;

    private void Start()
    {
        // 1. 총알 수 변경 이벤트를 구독한다.
        _playerGunFire.OnAmmoChanged += Refresh;

        // 2. 시작 값을 한 번 그려 준다.
        Refresh();
    }

    private void OnDestroy()
    {
        if (_playerGunFire != null)
        {
            _playerGunFire.OnAmmoChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        _ammoText.text = _playerGunFire.Ammo.ToString();
    }
}
