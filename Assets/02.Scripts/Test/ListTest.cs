using UnityEngine;
using System.Collections.Generic;

public class ListTest : MonoBehaviour
{
    // 자료구조(Data Structure)
    // 이런 데이터들을 "효율적"으로 관리(CRUDS) 하기 위해 저장하는 구조 방식
    // C - Creat  : 생성
    // R - Read   : 읽기
    // U - Update : 수정
    // D - Delete : 삭제
    // S - Search : 검색
    // 선형 자료구조 : 배열, 리스트, 스택, 큐
    // 비선형 자료구조 : 그래프, 트리, 해쉬테이블 

    private void Start()
    {
        int[] ages = new int[] { 22, 21, 20, 35 }; //  배열의 선언과 정의 

        // 1. 배열은 선언할 때 지정한 배열의 크기를 변경할 수 없다.
        // -> 배열에 데이터를 추가하거나 삭제하려면 새로운 배열을 생성해야 한다.
        // -> 배열은 데이터를 중간에 추가하거나 삭제하는 경우 비효율적인 과정이 필요하다.

        // 이 배열의 단점을 보완하기 위해 자료구조는 동적 배열 혹은 연결 리스트를 사용하는데...
        // C#은 이것을 "콜렉션" 네임스페이스 안에 List로 구현해놨다.
        // 콜렉션(Collection) : 컴퓨터공학의 자료구조를 C#에서 구조화하여 클래스로 구현해 놓은 것들

        // List -> 배열과 똑같은데 필요에 따라 크기가 동적으로 증감하는 배열이다.
        // 배열 사용은 크기가 안변하고 단순히 나란히 데이터를 나열할때 쓴다. ( 1% 거의 적음 ) 
        // using System.Collections.Generic;
        List<int> ages2 = new List<int>() { 22, 21, 20, 35 };

        // 2. 
    }

    private void Update()
    {
    }
}