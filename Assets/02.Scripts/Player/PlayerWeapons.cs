using UnityEngine;

// 역할 : 무기 모드에 맞는 무기 모델만 손에 보이게 한다. (근접 모드는 쿠크리, 나머지는 총)
// 무기 모드는 바뀔 때만 이벤트로 알려 오므로, 구독해서 그때만 켜고 끈다.
public class PlayerWeapons : MonoBehaviour
{
    [SerializeField] private GameObject _gunModel; // 오른손의 총 (Bip001 R Hand/Weapon)
    [SerializeField] private GameObject _meleeModel; // 오른손의 쿠크리 (Bip001 R Hand/KnifePoint)

    private Player _player;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void Start()
    {
        // 1. 무기 모드 변경 이벤트를 구독한다.
        _player.OnWeaponModeChanged += RefreshModel;

        // 2. 시작 모드에 맞게 한 번 맞춰 둔다.
        RefreshModel();
    }

    private void OnDestroy()
    {
        if (_player != null) _player.OnWeaponModeChanged -= RefreshModel;
    }

    private void RefreshModel()
    {
        bool isMelee = _player.WeaponMode == WeaponMode.Melee;
        _gunModel.SetActive(isMelee == false);
        _meleeModel.SetActive(isMelee);
    }
}
