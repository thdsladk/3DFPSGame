using System;
using UnityEngine;
using UnityEngine.Serialization;

// 역할 : 무기 하나의 설정값. PlayerWeapons 의 Inspector 에서 무기마다 채운다.
// 좌클릭·우클릭 행동을 여기서 고르므로, 값만 바꿔 끼우면 같은 버튼이 다른 일을 한다.
[Serializable]
public class WeaponData
{
    [SerializeField] private WeaponType _type = WeaponType.Rifle;
    [SerializeField] private Sprite _icon; // UI 에 보일 무기 이미지
    [SerializeField] private GameObject _model; // 손에 들 무기 모델 (이 무기를 들 때만 켠다)
    [SerializeField] private Transform _muzzle; // 총구 (총구 섬광과 총선 흔적이 여기서 나온다, 비우면 그대로 둔다)

    [Header("버튼 행동")]
    [SerializeField] private WeaponActionType _primaryAction = WeaponActionType.Shoot; // 좌클릭

    [SerializeField] private WeaponActionType _secondaryAction = WeaponActionType.None; // 우클릭

    [Header("총")]
    [SerializeField] private float _damage = 10f; // 한 발(산탄은 한 알)의 대미지

    [SerializeField] private float _range = 100f; // 사거리
    [SerializeField] private float _fireInterval = 0.1f; // 발사 간격
    [SerializeField] private ReloadType _reloadType = ReloadType.Magazine; // 장전 방식 (한 번에 다 채우기 / 한 발씩 넣기)

    [FormerlySerializedAs("_reloadInterval")]
    [SerializeField]
    private float _reloadTime = 2f; // 장전 시간 (탄창식은 다 채우는 데, 한 발씩은 한 발 넣는 데 걸리는 시간)

    [SerializeField] private int _maxAmmo = 50; // 탄창 크기 (0 이면 총이 아니다)
    [SerializeField] private int _pelletCount = 1; // 한 번 쏠 때 나가는 탄 수 (산탄총은 여러 발)
    [SerializeField] private float _spreadAngle = 0f; // 탄이 퍼지는 최대 각도 (도)
    [SerializeField] private float _explosionRadius = 0f; // 맞은 곳에서 터지는 범위 (0 이면 터지지 않는다)

    [Header("줌")]
    [SerializeField] private float _zoomFov = 15f; // 줌인 했을 때 시야각

    [SerializeField] private bool _useScope = false; // 줌인 할 때 조준경 화면을 띄울지

    public WeaponType Type => _type;
    public Sprite Icon => _icon;
    public GameObject Model => _model;
    public Transform Muzzle => _muzzle;
    public WeaponActionType PrimaryAction => _primaryAction;
    public WeaponActionType SecondaryAction => _secondaryAction;
    public float Damage => _damage;
    public float Range => _range;
    public float FireInterval => _fireInterval;
    public ReloadType ReloadType => _reloadType;
    public float ReloadTime => _reloadTime;
    public int MaxAmmo => _maxAmmo;
    public int PelletCount => Mathf.Max(1, _pelletCount);
    public float SpreadAngle => _spreadAngle;
    public float ExplosionRadius => _explosionRadius;
    public float ZoomFov => _zoomFov;
    public bool UseScope => _useScope;
}
