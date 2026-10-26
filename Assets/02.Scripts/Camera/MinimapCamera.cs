using System;
using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    private Camera _camera;

    [SerializeField] private Transform _target;
    [SerializeField] private float _height = 8f;
    [SerializeField] private float _sizeChangeOffset = 1f;
    private Vector2 _cameraZoomLimits;

    private void Start()
    {
        _camera = GetComponent<Camera>();
        _cameraZoomLimits = new Vector2(1f, 30f);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ZoomIn();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            ZoomOut();
        }
    }

    // 직교 크기를 줄여서 더 가깝게 보여준다.
    public void ZoomIn()
    {
        _camera.orthographicSize = Mathf.Max(_camera.orthographicSize - _sizeChangeOffset, _cameraZoomLimits.x);
    }

    // 직교 크기를 늘려서 더 넓게 보여준다.
    public void ZoomOut()
    {
        _camera.orthographicSize = Mathf.Min(_camera.orthographicSize + _sizeChangeOffset, _cameraZoomLimits.y);
    }

    private void LateUpdate()
    {
        // 1. 대상의 머리 위로 위치를 옮긴다.
        transform.position = _target.position + (Vector3.up * _height);

        // 2. 대상이 쳐다보는 방향으로 카메라도 회전한다.
        transform.eulerAngles = new Vector3(90f, _target.eulerAngles.y, 0f);
    }
}