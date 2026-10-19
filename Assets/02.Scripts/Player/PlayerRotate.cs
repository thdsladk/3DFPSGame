using UnityEngine;


public class PlayerRotate : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 200f;

    private float _accumulationX = 0f;
    private bool _isFirstFrame = true; // 커서 잠금 직후의 튀는 입력을 무시하기 위한 플래그

    private void Update()
    {
        var mouseX = Input.GetAxis("Mouse X");

        // 커서가 잠기면서 화면 가운데로 이동할 때 생기는 첫 프레임의 큰 입력값은 무시한다.
        if (_isFirstFrame)
        {
            _isFirstFrame = false;
            return;
        }

        _accumulationX += mouseX * _rotationSpeed * Time.deltaTime;

        transform.eulerAngles = new Vector3(0f, _accumulationX, 0f);
    }
}