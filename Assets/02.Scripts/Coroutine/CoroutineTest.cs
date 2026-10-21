using System.Collections;
using UnityEngine;


public class CoroutineTest : MonoBehaviour
{
    // 유니티의 코루틴이란
    // 비동기 실행과 제어를 효율적으로 제공하는 기능 

    // 동기 vs 비동기 
    // - 동기 : 어떤 함수(작업)을 실행할 때 그 작업이 끝나기를 기다리는 방식 ( 즉, 이전 작업이 완료될 때까지 대기 -> 다음 코드 실행 )
    // - 비동기:                     "" 그 작업이 완료되지 않더라도 다음 코드(함수)를 실행하는 방식이다. 

    // 1. 비동기 실행
    // 2. 시간/프레임에 따른 실행 제어
    // - IEnumerator : 코루틴 함수의 반환형
    // - StartCoroutine(함수()) : 유니티에서 코루틴 함수를 실행하는 방법 
    // - yield : 정해진 시간 / 프레임 동안 쉼 

    private void Start()
    {
        // Countdown이라는 메서드를 비동기 방식으로 실행한다. 
        // 유니티에서는 비동기로 처리해야 하는 로직을 위해 코루틴을 제공한다. Co + Routine
        StartCoroutine(Countdown_Coroutine());
    }

    private IEnumerator Countdown_Coroutine()
    {
        yield return new WaitForSeconds(1f);
    }
}