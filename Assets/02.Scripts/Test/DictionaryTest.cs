using System.Collections.Generic;
using UnityEngine;

public class DictionaryTest : MonoBehaviour
{
    // 지금까지 배웠던 배열이나 List는 인덱스를 이용해 요소에 접근했다.
    // 하지만 Dictionary(사전)는 요소를 저장할 대 "키"와 "값"을 쌍으로 저장한다.
    // HashTable(=Dictionary)은 키와 값이 쌍으로 이루어진 데이터를 관리(CRUDS)할 때 사용하는 자료구조

    private void Start()
    {
        Dictionary<string, int> ages = new Dictionary<string, int>() { { "A", 1 }, { "B", 2 }, { "C", 3 } };
    }
}