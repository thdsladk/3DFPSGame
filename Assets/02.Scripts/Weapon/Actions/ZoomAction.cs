// 역할 : 누르고 있는 동안 무기에 정해 둔 시야각으로 줌인 한다.
public class ZoomAction : IWeaponAction
{
    private readonly PlayerZoom _zoom;
    private readonly float _fov; // 줌인 했을 때 시야각
    private readonly bool _useScope; // 조준경 화면을 띄울지

    public ZoomAction(PlayerZoom zoom, float fov, bool useScope)
    {
        _zoom = zoom;
        _fov = fov;
        _useScope = useScope;
    }

    public void Press() => _zoom.ZoomIn(_fov, _useScope);
    public void Release() => _zoom.ZoomOut();
    public void Cancel() => _zoom.ZoomOut();
}
