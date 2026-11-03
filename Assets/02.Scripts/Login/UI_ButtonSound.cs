using UnityEngine;
using UnityEngine.UI;

// 역할 : 버튼을 누를 때마다 '쿵' 하는 소리를 낸다. 소리는 씬에 하나 둔 AudioSource 로 내고, 높낮이를 조금씩 바꿔 같은 소리가 반복되는 느낌을 줄인다.
[RequireComponent(typeof(Button))]
public class UI_ButtonSound : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource; // UI 소리를 내는 AudioSource (씬에 하나)
    [SerializeField] private AudioClip _clip; // 누를 때 나는 소리
    [SerializeField] private float _volume = 1f;
    [SerializeField] private float _minPitch = 0.92f; // 소리 높낮이 범위
    [SerializeField] private float _maxPitch = 1.05f;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Play);
    }

    private void Play()
    {
        _audioSource.pitch = Random.Range(_minPitch, _maxPitch);
        _audioSource.PlayOneShot(_clip, _volume);
    }
}
