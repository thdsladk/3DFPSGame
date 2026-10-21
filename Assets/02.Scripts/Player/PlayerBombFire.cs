using UnityEngine;

public class PlayerBombFire : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Bomb _bombPrepab;
    [SerializeField] private float _throwPower;
    [SerializeField] private int _ammo = 10;
    [SerializeField] private float _powerGauge = 0f;
    private const float MaxChagingTime = 2f;
    private bool _isChaging = false;

    private void Update()
    {
        if (_isChaging)
        {
            _powerGauge = Mathf.Min(MaxChagingTime, _powerGauge + Time.deltaTime);
            //GameLogger.Log($"PowerGauge : {_powerGauge}");
        }

        // 1. 마우스 오른쪽 버튼 입력 받기
        if (Input.GetButtonDown("Fire2"))
        {
            _isChaging = true;
        }
        else if (Input.GetButtonUp("Fire2"))
        {
            _isChaging = false;
            Shoot();
        }
    }

    private void Shoot()
    {
        if (_ammo > 0)
        {
            // 1. 폭탄 갯수 감소
            _ammo--;
            // 2. 발사 위치에서 수류탄 생성하기 
            Bomb bomb = Instantiate(_bombPrepab, _firePoint.position, Quaternion.identity);

            // 3. 생성된 수류탄을 시선 방향으로 던지기
            Rigidbody rigidbody = bomb.GetComponent<Rigidbody>();
            Vector3 force = Camera.main.transform.forward * (_throwPower * _powerGauge);
            rigidbody.AddForce(force, ForceMode.Impulse);

            // 4. 게이지 초기화
            _powerGauge = 0f;
        }
        else
        {
            GameLogger.Log("폭탄이 없습니다.");
        }
    }
}