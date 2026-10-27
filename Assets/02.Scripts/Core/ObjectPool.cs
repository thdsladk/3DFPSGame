using System.Collections.Generic;
using UnityEngine;

// 역할 : 자주 만들고 없애는 오브젝트를 미리 만들어 두고, 꺼내 쓰고(Get) 다 쓰면 돌려받는다(Release). (오브젝트 풀링)
// Instantiate/Destroy 를 반복하면 생성 비용과 가비지(GC)가 쌓여서 끊김이 생기므로, 꺼 두었다가 다시 켜서 재사용한다.
public class ObjectPool<T> where T : Component
{
    private readonly T _prefab;
    private readonly Transform _parent; // 풀에서 만든 오브젝트를 모아 둘 부모 (null 이면 씬 최상위)
    private readonly Queue<T> _pool = new Queue<T>(); // 쉬고 있는(꺼진) 오브젝트

    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        _prefab = prefab;
        _parent = parent;

        // 처음에 몇 개를 미리 만들어 둔다. (게임 중에 만드는 비용을 줄인다)
        for (int i = 0; i < initialSize; i++)
        {
            _pool.Enqueue(Create());
        }
    }

    // 쉬고 있는 오브젝트를 꺼내서 위치를 맞추고 켠다. 모자라면 새로 만든다.
    public T Get(Vector3 position, Quaternion rotation)
    {
        T item = _pool.Count > 0 ? _pool.Dequeue() : Create();

        // 켜기 전에 위치를 맞춘다. (NavMeshAgent·Rigidbody 가 켜지는 순간의 위치를 쓰기 때문)
        item.transform.SetPositionAndRotation(position, rotation);
        item.gameObject.SetActive(true);
        return item;
    }

    // 다 쓴 오브젝트를 끄고 풀에 돌려놓는다.
    public void Release(T item)
    {
        item.gameObject.SetActive(false);
        _pool.Enqueue(item);
    }

    private T Create()
    {
        T item = Object.Instantiate(_prefab, _parent);
        item.gameObject.SetActive(false);
        return item;
    }
}
