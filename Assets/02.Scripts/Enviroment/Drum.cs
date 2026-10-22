using System.Collections;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class Drum : MonoBehaviour
{
    // 체력이 0이 되면 위로 튀어오르며 폭발하는 드럼통
    [SerializeField] private ParticleSystem _explodeEffectPrefab;

    [Header("Stat")]
    [SerializeField] private float _health = 30f;

    [SerializeField] private float _launchPower = 20f;
    [SerializeField] private float _spinPower = 5f;
    [SerializeField] private float _destroyDelay = 3f;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }


    public void Damage(float damage)
    {
        if (_health <= 0f)
        {
            return;
        }

        _health -= damage;
        if (_health <= 0f)
        {
            StartCoroutine(Explode_Coroutine());
        }
    }

    private IEnumerator Explode_Coroutine()
    {
        // 1. 폭발 이펙트를 생성한다. 
        ParticleSystem effect = Instantiate(_explodeEffectPrefab, transform.position, Quaternion.identity);
        effect.Play();

        _rigidbody.AddForce(Vector3.up * _launchPower, ForceMode.Impulse);
        _rigidbody.AddTorque(UnityEngine.Random.insideUnitSphere * _spinPower, ForceMode.Impulse);

        // 3. 딜레이 후 사라진다.
        yield return new WaitForSeconds(_destroyDelay);
        Destroy(effect);
    }
}