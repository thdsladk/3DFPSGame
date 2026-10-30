using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 역할 : AmmoPanel 에 지금 무기 이미지·이름, 총알 수, 폭탄 수를 보여 준다.
// 값은 쏘거나·던지거나·장전하거나·무기를 바꿀 때만 바뀌므로, 체력 막대처럼 이벤트를 구독(옵저버 패턴)해서 바뀔 때만 고친다.
public class UI_Ammo : MonoBehaviour
{
    [Header("총알")]
    [SerializeField] private TextMeshProUGUI _ammoText; // 총알 수 글자 (AmmoPanel/AmmoText)

    [SerializeField] private PlayerGunFire _playerGunFire;
    [SerializeField] private Image _reloadGauge; // 총알 아이콘 뒤의 원형 장전 게이지 (AmmoPanel/ReloadGauge, Filled · Radial 360)

    [Header("폭탄")]
    [SerializeField] private TextMeshProUGUI _bombText; // 폭탄 수 글자 (AmmoPanel/BombText)

    [SerializeField] private PlayerBombFire _playerBombFire;

    [Header("무기")]
    [SerializeField] private Image _weaponImage; // 지금 무기 이미지 (AmmoPanel/WeaponImage, 무기에 이미지가 없으면 숨긴다)

    [SerializeField] private TextMeshProUGUI _weaponNameText; // 지금 무기 이름 (AmmoPanel/WeaponNameText, 비워 두면 쓰지 않는다)
    [SerializeField] private PlayerWeapons _playerWeapons;

    private const string NoAmmoText = "-"; // 총이 아닌 무기(근접 무기)를 들었을 때 총알 수 대신 보일 글자

    private void Start()
    {
        // 1. 총알·폭탄 수와 무기 변경 이벤트를 구독한다.
        _playerGunFire.OnAmmoChanged += RefreshAmmo;
        _playerBombFire.OnAmmoChanged += RefreshBomb;
        _playerWeapons.OnWeaponChanged += RefreshWeapon;

        // 2. 시작 값을 한 번 그려 준다.
        RefreshAmmo();
        RefreshBomb();
        RefreshWeapon();
    }

    private void OnDestroy()
    {
        if (_playerGunFire != null) _playerGunFire.OnAmmoChanged -= RefreshAmmo;
        if (_playerBombFire != null) _playerBombFire.OnAmmoChanged -= RefreshBomb;
        if (_playerWeapons != null) _playerWeapons.OnWeaponChanged -= RefreshWeapon;
    }

    // 장전 게이지는 매 프레임 차오르므로 이벤트가 아니라 여기서 채운다. 장전 중일 때만 갱신한다.
    private void Update()
    {
        if (_playerGunFire.IsReloading == false) return;

        _reloadGauge.fillAmount = _playerGunFire.ReloadProgress;
    }

    private void RefreshAmmo()
    {
        _ammoText.text = _playerGunFire.MaxAmmo > 0 ? _playerGunFire.Ammo.ToString() : NoAmmoText;

        // 장전이 시작되면 게이지를 보이고, 끝나거나(쏘기·무기 교체로) 취소되면 숨긴다.
        _reloadGauge.enabled = _playerGunFire.IsReloading;
        _reloadGauge.fillAmount = _playerGunFire.ReloadProgress;
    }

    private void RefreshBomb()
    {
        _bombText.text = _playerBombFire.Ammo.ToString();
    }

    private void RefreshWeapon()
    {
        WeaponData data = _playerWeapons.CurrentWeapon.Data;

        _weaponImage.sprite = data.Icon;
        _weaponImage.enabled = data.Icon != null;

        if (_weaponNameText != null) _weaponNameText.text = data.Type.ToString();
    }
}
