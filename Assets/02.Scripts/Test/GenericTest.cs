using UnityEngine;

public class Cup<T>
{
    public T Drink;

    public Cup(T drink)
    {
        Drink = drink;
    }
}

public class GenericTest : MonoBehaviour
{
    private void Start()
    {
    }

    private void Update()
    {
    }
}