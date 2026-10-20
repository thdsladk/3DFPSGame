using System.Collections;
using UnityEngine;

// 마우스 커서를 보이게 두고 게임 화면 밖으로 나가지 않게 가둔다.
// 커서의 화면 위치가 곧 바라보는 각도이므로, 시작할 때 커서를 화면 가운데로 옮긴다.
public class CursorManager : MonoBehaviour
{
    private IEnumerator Start()
    {
        // 잠금 상태로 한 프레임 두면 커서가 화면 가운데로 이동한다.
        LockCursor();
        yield return null;

        ConfineCursor();
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked; // 화면 가운데에 고정
        Cursor.visible = false; // 커서 숨기기
    }

    public void ConfineCursor()
    {
        Cursor.lockState = CursorLockMode.Confined; // 게임 화면 안에 가두기
        Cursor.visible = true;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 커서의 화면 위치를 가운데 (0, 0), 왼쪽·아래 끝 -1, 오른쪽·위 끝 1 로 바꿔서 돌려준다.
    public static Vector2 GetNormalizedCursorPosition()
    {
        var mousePosition = Input.mousePosition;
        var x = (mousePosition.x / Screen.width - 0.5f) * 2f;
        var y = (mousePosition.y / Screen.height - 0.5f) * 2f;
        return new Vector2(Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(y, -1f, 1f));
    }
}
