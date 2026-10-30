using System;
using UnityEngine;

// 역할 : 플레이어가 가진 무기(주 무기들·보조 무기·근접 무기)를 들고 바꾼다.
// 1 / 2 / 3 키로 주·보조·근접 무기를 들고, C 키로 다음 주 무기(소총 → 저격총 → 산탄총 → 대포)로 바꾼다.
// 좌클릭·우클릭은 지금 든 무기에 끼워 둔 행동(IWeaponAction)에 맡긴다. 무기만 갈아 끼우면 버튼이 하는 일이 바뀐다. (전략 패턴)
public class PlayerWeapons : MonoBehaviour
{
    [SerializeField] private WeaponData[] _mainWeaponDatas; // 1번 주 무기들 (C 키로 차례로 바꾼다)
    [SerializeField] private WeaponData _subWeaponData; // 2번 보조 무기
    [SerializeField] private WeaponData _meleeWeaponData; // 3번 근접 무기
    [SerializeField] private KeyCode _nextMainWeaponKey = KeyCode.C; // 다음 주 무기로 바꾸는 키 (임시)

    private Weapon[] _mainWeapons;
    private Weapon _subWeapon;
    private Weapon _meleeWeapon;
    private int _mainWeaponIndex = 0; // 지금 고른 주 무기
    private WeaponMode _weaponMode = WeaponMode.Main;
    private Weapon _currentWeapon;

    // 버튼 행동이 일을 맡길 컴포넌트
    private PlayerGunFire _gunFire;
    private PlayerBombFire _bombFire;
    private PlayerZoom _zoom;
    private PlayerMeleeAttack _meleeAttack;

    public WeaponMode WeaponMode => _weaponMode;
    public Weapon CurrentWeapon => _currentWeapon;

    // 든 무기가 바뀌면 알린다. (UI_Ammo 가 구독해 무기 이미지와 이름을 바꾼다)
    public event Action OnWeaponChanged;

    private void Awake()
    {
        _gunFire = GetComponent<PlayerGunFire>();
        _bombFire = GetComponent<PlayerBombFire>();
        _zoom = GetComponent<PlayerZoom>();
        _meleeAttack = GetComponent<PlayerMeleeAttack>();

        // 1. 설정값마다 버튼 행동을 끼운 무기를 만들고, 모델은 모두 숨겨 둔다.
        _mainWeapons = new Weapon[_mainWeaponDatas.Length];
        for (int i = 0; i < _mainWeaponDatas.Length; i++)
        {
            _mainWeapons[i] = CreateWeapon(_mainWeaponDatas[i]);
        }

        _subWeapon = CreateWeapon(_subWeaponData);
        _meleeWeapon = CreateWeapon(_meleeWeaponData);

        // 2. 첫 주 무기를 든다. (UI 가 Start 에서 지금 무기를 읽을 수 있도록 Awake 에서 든다)
        ChangeWeapon(WeaponMode.Main, _mainWeapons[_mainWeaponIndex]);
    }

    private void Update()
    {
        // 1. 버튼을 떼는 입력은 언제나 받는다. (게임이 멈췄거나 커서를 푼 사이에 떼도 눌린 채로 남지 않게)
        if (Input.GetButtonUp("Fire1")) _currentWeapon.ReleasePrimary();
        if (Input.GetButtonUp("Fire2")) _currentWeapon.ReleaseSecondary();

        if (GameManager.Instance.State != GameState.Playing) return;

        // Alt로 커서를 풀어 UI 버튼을 누르는 중이면 무기를 쓰지 않는다.
        if (CursorManager.IsCursorFree) return;

        // 2. 무기 바꾸기
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChangeWeapon(WeaponMode.Main, _mainWeapons[_mainWeaponIndex]);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChangeWeapon(WeaponMode.Sub, _subWeapon);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ChangeWeapon(WeaponMode.Melee, _meleeWeapon);
        }
        else if (Input.GetKeyDown(_nextMainWeaponKey))
        {
            _mainWeaponIndex = (_mainWeaponIndex + 1) % _mainWeapons.Length;
            ChangeWeapon(WeaponMode.Main, _mainWeapons[_mainWeaponIndex]);
        }

        // 3. 버튼을 누르면 지금 무기의 행동에 맡긴다.
        if (Input.GetButtonDown("Fire1")) _currentWeapon.PressPrimary();
        if (Input.GetButtonDown("Fire2")) _currentWeapon.PressSecondary();
    }

    // 무기를 바꿔 든다. 같은 무기면 무시한다.
    private void ChangeWeapon(WeaponMode weaponMode, Weapon nextWeapon)
    {
        if (_currentWeapon == nextWeapon) return;

        // 1. 들고 있던 무기의 행동을 멈추고 모델을 숨긴다.
        if (_currentWeapon != null)
        {
            _currentWeapon.CancelActions();
            _currentWeapon.SetModelVisible(false);
        }

        // 2. 새 무기를 들고, 총 쏘기에 새 무기의 총알·스탯을 넘긴다.
        _weaponMode = weaponMode;
        _currentWeapon = nextWeapon;
        _currentWeapon.SetModelVisible(true);
        _gunFire.Equip(_currentWeapon);

        GameLogger.Log($"[PlayerWeapons] {_weaponMode} : {_currentWeapon.Data.Type}");
        OnWeaponChanged?.Invoke();
    }

    // 설정값에 적힌 행동 종류대로 좌클릭·우클릭 행동을 만들어 끼운다.
    private Weapon CreateWeapon(WeaponData data)
    {
        Weapon weapon = new Weapon(data, CreateAction(data.PrimaryAction, data), CreateAction(data.SecondaryAction, data));
        weapon.SetModelVisible(false);
        return weapon;
    }

    private IWeaponAction CreateAction(WeaponActionType actionType, WeaponData data)
    {
        return actionType switch
        {
            WeaponActionType.Shoot => new ShootAction(_gunFire),
            WeaponActionType.ThrowBomb => new BombThrowAction(_bombFire),
            WeaponActionType.Zoom => new ZoomAction(_zoom, data.ZoomFov, data.UseScope),
            WeaponActionType.Swing => new SwingAction(_meleeAttack),
            _ => null,
        };
    }
}
