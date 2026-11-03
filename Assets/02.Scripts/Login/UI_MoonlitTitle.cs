using TMPro;
using UnityEngine;

// 역할 : 제목 글자에 달빛이 비치는 느낌을 준다. 구름이 달을 스치듯 빛의 세기(글로우·반사광)가 천천히 일렁이고, 빛이 오는 각도도 조금씩 흔들린다.
// 그림자(언더레이)·베벨 같은 기본 모양은 머티리얼 프리셋에서 정하고, 이 스크립트는 그 값을 시간에 따라 바꾸기만 한다.
[RequireComponent(typeof(TMP_Text))]
public class UI_MoonlitTitle : MonoBehaviour
{
    [Header("빛이 오는 각도")]
    [SerializeField] private float _lightAngle = 5.6f; // 기본 각도 (라디안, 달이 있는 오른쪽 위)

    [SerializeField] private float _lightSwing = 0.25f; // 좌우로 흔들리는 폭 (라디안)
    [SerializeField] private float _swingPeriod = 9f; // 한 번 흔들리는 시간 (초)

    [Header("달빛 세기")]
    [SerializeField] private float _minGlow = 0.15f; // 구름에 가렸을 때 글로우 세기

    [SerializeField] private float _maxGlow = 0.55f; // 달이 드러났을 때 글로우 세기
    [SerializeField] private float _minSpecular = 0.6f; // 구름에 가렸을 때 반사광 세기
    [SerializeField] private float _maxSpecular = 2.2f; // 달이 드러났을 때 반사광 세기
    [SerializeField] private float _cloudSpeed = 0.25f; // 구름이 지나가는 빠르기

    private static readonly int LightAngleId = Shader.PropertyToID("_LightAngle");
    private static readonly int GlowPowerId = Shader.PropertyToID("_GlowPower");
    private static readonly int SpecularPowerId = Shader.PropertyToID("_SpecularPower");

    private Material _material;

    private void Awake()
    {
        // 공용 머티리얼 프리셋을 건드리지 않도록 이 글자만의 사본을 쓴다.
        _material = GetComponent<TMP_Text>().fontMaterial;
    }

    private void Update()
    {
        float time = Time.unscaledTime;

        // 1. 빛이 오는 각도를 천천히 흔든다.
        float swing = Mathf.Sin(time * Mathf.PI * 2f / _swingPeriod) * _lightSwing;
        _material.SetFloat(LightAngleId, _lightAngle + swing);

        // 2. 구름이 달을 가렸다 드러냈다 하듯 빛의 세기를 불규칙하게 바꾼다.
        float moonlight = Mathf.PerlinNoise(time * _cloudSpeed, 0.37f);
        _material.SetFloat(GlowPowerId, Mathf.Lerp(_minGlow, _maxGlow, moonlight));
        _material.SetFloat(SpecularPowerId, Mathf.Lerp(_minSpecular, _maxSpecular, moonlight));
    }

    private void OnDestroy()
    {
        if (_material != null) Destroy(_material);
    }
}
