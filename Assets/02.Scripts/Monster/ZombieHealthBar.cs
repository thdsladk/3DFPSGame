using UnityEngine;
using UnityEngine.UI;

public class ZombieHealthBar : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private Zombie _zombie;

    private void Awake()
    {
        _zombie = GetComponentInParent<Zombie>();
    }

    private void LateUpdate()
    {
        _slider.value = _zombie.Health / _zombie.MaxHealth;

        _slider.transform.forward = Camera.main.transform.forward;
    }
}