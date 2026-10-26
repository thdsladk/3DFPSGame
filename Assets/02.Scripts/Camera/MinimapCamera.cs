using System;
using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    private Camera _camera;

    [SerializeField] private Transform _target;
    [SerializeField] private float _height = 8f;
    [SerializeField] private float _sizeChangeOffset = 1f;

    private void Start()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        // Todo. ZoomIn/ZoomOut 함수로 분리 
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ZoomOut();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            ZoomIn();
        }
    }

    private void ZoomIn()
    {
        _camera.orthographicSize += _sizeChangeOffset;
    }

    private void ZoomOut()
    {
        _camera.orthographicSize -= _sizeChangeOffset;
    }

    private void LateUpdate()
    {
        // 1. 대상의 머리 위로 위치를 옮긴다.
        transform.position = _target.position + (Vector3.up * _height);

        // 2. 대상이 쳐다보는 방향으로 카메라도 회전한다.
        transform.eulerAngles = new Vector3(90f, _target.eulerAngles.y, 0f);
    }

    // Todo. +- 버튼 누르면 ZoomIn / ZoomOut 기능 구현하기 
}