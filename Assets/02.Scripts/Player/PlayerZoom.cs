using UnityEngine;

// 역할 : 줌 행동(ZoomAction)이 부르면 시야각을 좁혀 확대하고, 조준경을 쓰는 무기면 조준경 화면을 띄운다.
// 우클릭으로 줌을 쓸지는 무기마다 정한다. (저격총·대포)
public class PlayerZoom : MonoBehaviour
{
    [SerializeField] private GameObject _sniperZoom; // 조준경 화면
    [SerializeField] private float _normalPov = 60f; // 줌을 풀었을 때 시야각

    private void Start()
    {
        ZoomOut();
    }

    // 정한 시야각으로 확대한다.
    public void ZoomIn(float fov, bool useScope)
    {
        _sniperZoom.SetActive(useScope);
        Camera.main.fieldOfView = fov;
    }

    // 원래 시야각으로 돌아간다.
    public void ZoomOut()
    {
        _sniperZoom.SetActive(false);
        Camera.main.fieldOfView = _normalPov;
    }
}
