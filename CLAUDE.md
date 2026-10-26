# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 6 (`6000.3.22f1`), URP 기반 3D 슈팅 게임 학습 프로젝트. 메인 씬은 `Assets/01.Scenes/GameScene.unity` 하나뿐이며, 게임 코드는 전부 `Assets/02.Scripts/` 아래에 있다 (asmdef 없음 → `Assembly-CSharp`).

- 빌드·실행·테스트는 Unity 에디터에서 한다. 이 저장소에는 CLI 빌드 스크립트나 테스트 코드가 없다 (`Assets/02.Scripts/Test/`의 파일은 C# 문법 연습용 MonoBehaviour이지 단위 테스트가 아님).
- `.unity`, `.prefab`, `.asset` 파일은 Unity가 관리하는 YAML이다. 직접 편집하지 말고, Inspector에서 해야 하는 작업은 사용자에게 안내한다. 스크립트 이름을 바꾸면 `.meta`의 GUID 연결이 끊길 수 있으므로 주의한다.

## 커밋 / 포맷 규칙 (Husky.Net 훅)

`dotnet-tools.json`에 Husky.Net이 등록되어 있다 (최초 1회 `dotnet tool restore` 후 `dotnet husky install`).

- **commit-msg**: 커밋 메시지는 `type: 설명` 형식이어야 한다. 허용 type: `asset|build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test` (선택적 `(scope)`, `!`). 설명은 한국어로 쓰는 것이 기존 관례다. 예: `feat: 연쇄 폭발 추가`
- **pre-commit**: 아래 포맷 검사가 통과해야 커밋된다. 커밋 전에 직접 돌려볼 것.
  ```
  dotnet format 3DFPSGame.sln --verify-no-changes --severity warn --include Assets/02.Scripts/
  ```
  자동 수정은 `--verify-no-changes`를 빼고 실행한다. `.sln`/`.csproj`는 Unity가 생성하므로 없으면 에디터에서 재생성해야 한다.
- `.editorconfig`: 4칸 공백 들여쓰기. `[SerializeField]` 등 필드 속성은 선언이 한 줄일 때만 같은 줄에 두고, 초기화가 여러 줄인 필드는 속성을 별도 줄로 분리한다.

## CI: Gemini 커밋 리뷰

`main`에 `Assets/02.Scripts/**/*.cs` 변경이 push되면 `.github/workflows/gemini-commit-review.yml`이 Gemini로 해당 커밋을 리뷰하고 커밋 코멘트를 단다. 리뷰 기준은 `Docs/CodeReviewGuide.md`(GEMINI.md에서 참조)이며, 코드 작성·리뷰 시 이 기준을 따른다. 요지:
- 실제 발생 가능한 버그 우선, 과도한 추상화·디자인 패턴 강요 금지
- 의미 있는 수치는 매직 넘버 대신 `[SerializeField]` 또는 상수로
- "묻지 말고 시켜라"(상태를 꺼내 판단하지 말고 `TakeDamage()`처럼 행동을 요청)
- `Update`에서의 할당·`Find`/`GetComponent` 반복, 코루틴 생명주기, 문자열 태그/레이어 반복 참조 등 Unity 특유 문제 확인

## 코드 컨벤션

- private 필드는 `_camelCase` + `[SerializeField] private`, 공개는 읽기 전용 프로퍼티(`public float Health => _health;`).
- 파일 상단에 `// 역할 : ...` 형태로 클래스 책임을 한국어 주석으로 적고, 메서드 본문은 `// 1. ... // 2. ...` 단계 주석을 다는 스타일. 주석은 한국어.
- 코루틴 메서드 이름은 `Xxx_Coroutine`.
- 로그는 `Debug.Log` 대신 `GameLogger.Log`를 쓴다. `[Conditional("ENABLE_LOG")]`이라 해당 심볼이 없으면 호출 자체가 컴파일에서 제거된다 (현재 Standalone 정의 심볼에 `ENABLE_LOG` 포함). Warning/Error는 항상 출력된다.
- 입력은 레거시 `Input.GetButton/GetAxis`(Input Manager 축 이름: `Fire1`, `Fire2`, `Jump`, `Sprint`, `Reload` 등)를 사용한다. Active Input Handling이 Both로 설정되어 있고 `InputSystem_Actions.inputactions`는 아직 쓰이지 않는다.
- `// Todo.` 주석은 아직 구현하지 않은 기능을 표시한다.

## 아키텍처

### 플레이어 이동: Motor 중재 구조
`PlayerMotor`가 `CharacterController.Move`를 **프레임당 한 번만** 호출하는 유일한 곳이다. `isGrounded`/천장 판정은 Move 결과로만 갱신되기 때문이다.
- `PlayerMove.CalculateHorizontalVelocity()` → 수평 속도 (카메라 yaw 기준 방향, 달리기, 가속/공중 제어)
- `PlayerJump.CalculateVerticalVelocity()` → 수직 속도 (중력, 다단 점프, 코요테 타임, 점프 버퍼, 가변 점프 등 각각 토글 가능한 게임필 옵션)
- Move 후 `PlayerJump.OnMoved(collisionFlags)`로 충돌 결과 전달
- `PlayerStamina.TryConsume()`을 달리기·점프가 공유

새 이동 요소를 추가할 때는 `Move`를 직접 호출하지 말고 속도 계산 함수로 만들어 Motor에서 합친다.

### 커서 위치 = 시선 방향
마우스 이동량이 아니라 **커서의 화면 위치**로 시선을 정한다. `CursorManager`가 시작 시 커서를 한 프레임 Lock해 가운데로 옮긴 뒤 Confined로 가두고, `CursorManager.GetNormalizedCursorPosition()`(-1~1)을 제공한다.
- `PlayerRotate`: 커서 X → 플레이어 yaw (화면 끝에서 더 밀면 기준 방향 회전)
- `CameraRotate`: 커서 Y → `Pitch` 값만 계산 (Update)
- 좌 Alt를 누르는 동안 커서 Unlock. `PlayerGunFire`는 `Cursor.visible`이면 재장전 입력을 무시한다.

### 카메라 시점 전환
`CameraViewChanger`(V키)가 1인칭/3인칭/숄더/쿼터/탑다운을 순환하며 LateUpdate에서 실제 카메라 위치·회전을 적용한다. 고정 각도 시점(`UseMouseLook == false`)에서는 `PlayerRotate`와 `CameraRotate` 컴포넌트를 비활성화한다. 플레이어 자식 Transform `CameraPosition`, `Nose`를 이름으로 찾는다. Update(회전 계산) → LateUpdate(카메라 적용) 순서에 의존한다.

### 전투 / 데미지
- `PlayerGunFire`: `Camera.main` 정면 Raycast 히트스캔, 코루틴 한 발씩 재장전(발사 시 중단).
- `PlayerBombFire`: 우클릭 차지 → `Bomb` 프리팹을 Impulse로 투척. `Bomb`은 충돌 시 이펙트만 내고 파괴(데미지 없음).
- `Drum`: 체력 0 → 지연 후 폭발, `OverlapSphere`로 `Drum`/`Zombie` 레이어에 범위 데미지 → 연쇄 폭발.
- `IDamageable`/`DamageInfo`가 `Core`에 정의되어 있으나 아직 아무도 구현하지 않는다. 현재는 `GetComponent<Zombie>()?.TakeDamage(float)`, `GetComponent<Drum>()?.Damage(float)`처럼 구체 타입별로 호출한다. 데미지 대상을 추가하면 호출부(`PlayerGunFire.Shoot`, `Drum.ApplyAreaDamage`) 모두를 갱신해야 한다.
- 레이어: `Player`, `Bomb`, `Drum`, `Zombie`, `Minimap`. 플레이어는 `Player` 태그로 찾는다.

### 좀비 AI
`Zombie`는 enum + `switch` 기반 FSM (`Idle → Patrol ↔ Idle`, `Trace`, `Comeback`, `Attack`, `Hit`, `Death`). 이동은 `CharacterController.SimpleMove`(NavMesh 미사용). Hit/Death는 코루틴으로 처리하며 `TakeDamage`에서 `StopAllCoroutines()` 후 재시작한다. `ZombieHealthBar`는 부모의 `Zombie`를 읽어 월드 공간 Slider를 카메라 방향으로 빌보드한다.

### 미니맵
`MinimapCamera`가 대상 머리 위에서 대상 yaw를 따라 회전하는 직교 카메라(9/0 키로 줌), 결과를 RenderTexture + 마스크로 UI에 표시한다 (`Assets/Minimap_Resource`).
