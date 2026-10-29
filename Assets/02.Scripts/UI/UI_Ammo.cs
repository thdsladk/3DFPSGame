using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 역할 : AmmoPanel 에 지금 무기 이미지, 총알 수, 폭탄 수를 보여 준다.
// 값은 쏘거나·던지거나·장전하거나·무기를 바꿀 때만 바뀌므로, 체력 막대처럼 이벤트를 구독(옵저버 패턴)해서 바뀔 때만 고친다.
public class UI_Ammo : MonoBehaviour
{
    [Header("총알")]
    [SerializeField] private TextMeshProUGUI _ammoText; // 총알 수 글자 (AmmoPanel/AmmoText)

    [SerializeField] private PlayerGunFire _playerGunFire;

    [Header("폭탄")]
    [SerializeField] private TextMeshProUGUI _bombText; // 폭탄 수 글자 (AmmoPanel/BombText)

    [SerializeField] private PlayerBombFire _playerBombFire;

    [Header("무기")]
    [SerializeField] private Image _weaponImage; // 지금 무기 이미지 (AmmoPanel/WeaponImage)

    [SerializeField] private Player _player;
    [SerializeField] private Sprite _normalWeaponSprite; // 일반 모드 무기 (Weapon01_gun)
    [SerializeField] private Sprite _sniperWeaponSprite; // 저격 모드 무기 (Weapon02_sniper)

    private void Start()
    {
        // 1. 총알·폭탄 수와 무기 모드 변경 이벤트를 구독한다.
        _playerGunFire.OnAmmoChanged += RefreshAmmo;
        _playerBombFire.OnAmmoChanged += RefreshBomb;
        _player.OnWeaponModeChanged += RefreshWeapon;

        // 2. 시작 값을 한 번 그려 준다.
        RefreshAmmo();
        RefreshBomb();
        RefreshWeapon();
    }

    private void OnDestroy()
    {
        if (_playerGunFire != null) _playerGunFire.OnAmmoChanged -= RefreshAmmo;
        if (_playerBombFire != null) _playerBombFire.OnAmmoChanged -= RefreshBomb;
        if (_player != null) _player.OnWeaponModeChanged -= RefreshWeapon;
    }

    private void RefreshAmmo()
    {
        _ammoText.text = _playerGunFire.Ammo.ToString();
    }

    private void RefreshBomb()
    {
        _bombText.text = _playerBombFire.Ammo.ToString();
    }

    private void RefreshWeapon()
    {
        _weaponImage.sprite = _player.WeaponMode == WeaponMode.Sniper ? _sniperWeaponSprite : _normalWeaponSprite;
    }
}
