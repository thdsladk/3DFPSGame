using System;
using System.Threading;
using UnityEngine;

// 코루틴 대신 Awaitable(유니티 6의 async / await)로 같은 일을 하는 예시
// 빈 오브젝트에 붙이고 재생한 뒤 Console 창을 보면 된다.
public class AwaitableTest : MonoBehaviour
{
    // Awaitable 이란
    // 유니티가 제공하는 async / await 용 타입. 코루틴처럼 메인 스레드에서 시간·프레임 단위로 기다릴 수 있다.

    // 코루틴 vs Awaitable
    // - 반환형      : IEnumerator               → async Awaitable
    // - 실행        : StartCoroutine(함수())     → 함수() 를 그냥 호출 (또는 await 함수())
    // - 시간 기다림 : yield return new WaitForSeconds(1f) → await Awaitable.WaitForSecondsAsync(1f)
    // - 다음 프레임 : yield return null          → await Awaitable.NextFrameAsync()
    // - 멈추기      : StopCoroutine(코루틴)       → CancellationTokenSource.Cancel()
    // - 결과값      : 돌려줄 수 없음              → async Awaitable<int> 처럼 값을 돌려줄 수 있음
    // - 주의        : 오브젝트가 꺼지거나 파괴돼도 자동으로 멈추지 않는다 → destroyCancellationToken 을 넘겨서 멈춘다.

    [SerializeField] private int _maxAmmo = 10;
    [SerializeField] private float _reloadInterval = 0.5f; // 한 발 넣는 데 걸리는 시간

    private int _ammo = 0;
    private CancellationTokenSource _reloadCancellation; // 장전 중이면 값이 있고, 아니면 null

    public bool IsReloading => _reloadCancellation != null;

    private async void Start()
    {
        // 1. 카운트다운이 끝날 때까지 기다린 뒤 다음 줄을 실행한다.
        //    오브젝트가 파괴되면 destroyCancellationToken 이 취소되어 기다림도 멈춘다.
        try
        {
            await Countdown_Async(3, destroyCancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // 2. 카운트다운이 끝나면 빈 탄창부터 장전을 시작한다.
        Debug.Log("[AwaitableTest] R : 장전 시작, 마우스 왼쪽 : 발사(장전 중이면 장전 중단)");
        StartReload();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Reload"))
        {
            StartReload();
        }

        if (Input.GetButtonDown("Fire1"))
        {
            // 발사하면 장전 중단
            StopReload();
            Shoot();
        }
    }

    // 1초마다 숫자를 세고 끝난다.
    private async Awaitable Countdown_Async(int seconds, CancellationToken token)
    {
        for (var i = seconds; i > 0; i--)
        {
            Debug.Log($"[AwaitableTest] 카운트다운 : {i}");
            await Awaitable.WaitForSecondsAsync(1f, token);
        }

        Debug.Log("[AwaitableTest] 카운트다운 끝!");
    }

    // 장전을 시작한다. 이미 장전 중이거나 탄창이 가득 차 있으면 무시한다.
    private async void StartReload()
    {
        if (IsReloading || _ammo >= _maxAmmo) return;

        // 오브젝트가 파괴돼도 멈추도록 destroyCancellationToken 과 연결한다.
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        _reloadCancellation = cancellation;

        try
        {
            await Reload_Async(cancellation.Token);
            Debug.Log("[AwaitableTest] 장전 완료");
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"[AwaitableTest] 장전 중단 : {_ammo}/{_maxAmmo}");
        }
        finally
        {
            cancellation.Dispose();

            // 그 사이 새 장전이 시작됐다면 그 장전의 값은 지우지 않는다.
            if (_reloadCancellation == cancellation)
            {
                _reloadCancellation = null;
            }
        }
    }

    // 장전 중이면 멈춘다. 그때까지 넣은 총알은 그대로 남는다.
    private void StopReload()
    {
        if (IsReloading == false) return;

        _reloadCancellation.Cancel();
        _reloadCancellation = null;
    }

    // 장전 간격마다 한 발씩 넣고, 탄창이 가득 차면 끝난다.
    private async Awaitable Reload_Async(CancellationToken token)
    {
        while (_ammo < _maxAmmo)
        {
            await Awaitable.WaitForSecondsAsync(_reloadInterval, token);

            _ammo++;
            Debug.Log($"[AwaitableTest] 장전 : {_ammo}/{_maxAmmo}");
        }
    }

    private void Shoot()
    {
        if (_ammo <= 0) return;

        _ammo--;
        Debug.Log($"[AwaitableTest] 발사 : {_ammo}/{_maxAmmo}");

        // 총알이 없으면 자동으로 장전
        if (_ammo == 0)
        {
            StartReload();
        }
    }
}
