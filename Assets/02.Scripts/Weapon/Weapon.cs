using UnityEngine;

// 역할 : 플레이어가 가진 무기 하나. 설정값(WeaponData), 남은 총알, 좌클릭·우클릭 행동(IWeaponAction)을 함께 가진다.
// 버튼 입력은 끼워 둔 행동 객체에 그대로 넘기므로, 무기를 바꿔 끼우면 버튼이 하는 일도 바뀐다. (전략 패턴)
public class Weapon
{
    private readonly WeaponData _data;
    private readonly IWeaponAction _primaryAction; // 좌클릭 (없으면 null)
    private readonly IWeaponAction _secondaryAction; // 우클릭 (없으면 null)
    private int _ammo; // 무기마다 따로 남은 총알 (바꿔 들어도 그대로 남는다)

    public Weapon(WeaponData data, IWeaponAction primaryAction, IWeaponAction secondaryAction)
    {
        _data = data;
        _primaryAction = primaryAction;
        _secondaryAction = secondaryAction;
        _ammo = data.MaxAmmo;
    }

    public WeaponData Data => _data;
    public int Ammo => _ammo;
    public bool IsAmmoFull => _ammo >= _data.MaxAmmo;

    public void PressPrimary() => _primaryAction?.Press();
    public void ReleasePrimary() => _primaryAction?.Release();
    public void PressSecondary() => _secondaryAction?.Press();
    public void ReleaseSecondary() => _secondaryAction?.Release();

    // 다른 무기로 바꿀 때 진행 중인 행동(발사·줌·던지기·휘두르기)을 멈춘다.
    public void CancelActions()
    {
        _primaryAction?.Cancel();
        _secondaryAction?.Cancel();
    }

    // 손에 든 모델을 보이거나 숨긴다.
    public void SetModelVisible(bool isVisible)
    {
        if (_data.Model != null) _data.Model.SetActive(isVisible);
    }

    // 총알을 한 발 쓴다. 남은 총알이 없으면 false.
    public bool TryUseAmmo()
    {
        if (_ammo <= 0) return false;

        _ammo--;
        return true;
    }

    // 장전으로 한 발 넣는다.
    public void AddAmmo()
    {
        if (IsAmmoFull == false) _ammo++;
    }

    // 장전으로 탄창을 가득 채운다.
    public void FillAmmo()
    {
        _ammo = _data.MaxAmmo;
    }
}
