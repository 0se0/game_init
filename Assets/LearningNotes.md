# MyGame 학습 노트

이 프로젝트로 유니티 기초를 이해하기 위한 정리. 코드 파일의 주석과 같이 보면 좋다.

---

## 0. 유니티 개발 = 코드 + GUI, 둘 다

**질문: 유니티는 VS Code로 다 손코딩? 아니면 유니티 화면 GUI로?**
→ **둘을 나눠서** 한다. 어느 쪽에 얼마나 의존할지는 사람마다/프로젝트마다 다르다.

| 보통 GUI(에디터)로 하는 것 | 보통 코드로 하는 것 |
|---|---|
| 씬에 오브젝트 배치, 크기/위치/색 조정 | 규칙·로직 (움직임, 점수, AI, 상태 전환) |
| 머티리얼·조명·카메라 세팅 | 입력 처리, 물리 계산 |
| 애니메이터 상태 연결(Animator 창) | "언제 무엇을" 실행할지 |
| 프리팹 만들기, 컴포넌트 붙이기 | 데이터 저장/로드, 네트워크 |
| 파티클, 지형(Terrain), UI 레이아웃 | 절차적 생성(코드로 맵/오브젝트 만들기) |

- **코드는 "동작"**, **GUI는 "구성과 겉모습"** 이라고 보면 대체로 맞다.
- 이 프로젝트는 특이하게 **씬 구성까지 코드로** 했다(`SceneBootstrap.cs`, `CharacterSetup.cs`).
  이유: 재현 가능하고, 손으로 클릭하다 실수할 여지가 없기 때문. 보통은 GUI로 배치한다.
- 스크립트를 GameObject에 붙이면, `public` 변수는 **인스펙터 GUI에 슬라이더/칸으로** 나온다.
  → 코드로 뼈대를 만들고, 숫자 튜닝은 GUI에서. 이게 유니티의 기본 작업 흐름.

---

## 1. GameObject 와 Component

- **GameObject**: 씬 안의 "물건" 하나. 그 자체로는 이름 + Transform(위치/회전/크기)만 있는 빈 그릇.
- **Component**: 그 그릇에 붙이는 "능력". 예:
  - `MeshRenderer` → 화면에 보이게
  - `CharacterController` → 이동/충돌
  - `PlayerMovement`(우리 스크립트) → 조작 로직
  - `Animator` → 애니메이션 재생
- Hierarchy 창에서 `Player` 를 펼치면 자식 `Model` 이 있다.
  Player = 콜라이더 + 로직, Model = 눈에 보이는 로봇. **역할을 분리**한 것.

---

## 2. MonoBehaviour 생명주기 (호출 순서)

우리 스크립트가 `: MonoBehaviour` 를 상속하면 Unity가 특정 이름의 함수를 자동 호출한다.

```
Awake()      // 생성 직후 1번. 참조 세팅(GetComponent 등)
OnEnable()   // 켜질 때마다. 이벤트/입력 구독
Start()      // 첫 프레임 직전 1번 (이 프로젝트는 안 씀)
─ 매 프레임 반복 ─
Update()        // 입력 읽기, 이동 계산
LateUpdate()    // 모든 Update 후. 카메라 배치에 사용
─────────────
OnDisable()   // 꺼질 때. 정리(입력 Disable)
```

- **왜 `Update`에서 이동, `LateUpdate`에서 카메라?**
  플레이어가 먼저 최종 위치로 움직이고(Update), 그걸 보고 카메라가 따라붙어야(LateUpdate) 화면이 안 떨린다.

---

## 3. Transform / Vector3 / Quaternion

- `transform.position` : 월드 좌표 (Vector3: x, y, z). y가 위.
- `transform.rotation` : 회전. 내부적으로 **Quaternion**(4개 숫자).
  사람은 이해 못 하니 `Quaternion.Euler(pitch, yaw, roll)` 로 각도에서 변환해 쓴다.
- **Vector3 연산**
  - `a + b` : 두 방향을 합침
  - `v.normalized` / `v.Normalize()` : 길이를 1로 (방향만)
  - `v.magnitude` : 길이(속력 구할 때), `v.sqrMagnitude` : 길이의 제곱(비교용, 더 빠름)
  - `transform.forward` / `.right` : 그 오브젝트 기준 앞/오른쪽 방향
- **왜 대각선 이동에서 `Normalize`?**
  W+D를 같이 누르면 (1,0,1) → 길이 √2 ≈ 1.41 이라 더 빨라진다. 1로 잘라 같은 속도로 만든다.

---

## 4. `Time.deltaTime` — 프레임 독립성

- `Update()`는 컴퓨터가 빠르면 초당 200번, 느리면 60번 호출된다.
- `position += speed;` 라고 쓰면 빠른 PC에서 3배 빨리 간다. ❌
- `position += speed * Time.deltaTime;` → `deltaTime` = 지난 프레임 걸린 시간(초).
  다 곱하면 **어느 PC에서든 1초에 `speed` 만큼** 이동. ✅
- 규칙: "매 프레임 누적되는 값(이동, 회전, 중력)"에는 항상 `Time.deltaTime` 을 곱한다.

---

## 5. CharacterController vs Rigidbody

이동 구현하는 두 가지 방식:

| | CharacterController (이 프로젝트) | Rigidbody (물리) |
|---|---|---|
| 이동 | `controller.Move(벡터)` 직접 | 힘/속도를 주고 물리엔진이 계산 |
| 충돌 | 벽/바닥 자동으로 막힘 | 물리적으로 밀림/튕김 |
| 중력 | **직접** 더해야 함 | 자동 |
| 느낌 | 딱딱하고 정확 (플랫포머/3인칭 액션에 적합) | 사실적이지만 제어 어려움 |

- 그래서 우리 코드에 `velocity.y += gravity * Time.deltaTime;` 가 있는 것.
- `controller.isGrounded` : 직전 `Move()`에서 바닥에 닿았는지. **프레임 맨 앞에서 읽어야** 안정적.

---

## 6. Input System (신형 입력)

- 옛날: `Input.GetKey(KeyCode.W)` — 간단하지만 키가 코드에 박힘.
- 신형: **InputAction** = "행동" 단위. 하나의 액션에 키보드+게임패드+마우스를 다 연결.
- 이 프로젝트는 인스펙터 설정 없이 **코드로** 액션을 만들었다:
  - `AddCompositeBinding("2DVector")` → WASD 4키를 묶어 Vector2 (조이스틱처럼)
  - `WasPressedThisFrame()` → "이번 프레임에 막 눌림"(점프처럼 1회성)
  - `ReadValue<Vector2>()` → 현재 눌린 상태를 값으로 (이동처럼 지속)
  - `Keyboard.current.leftShiftKey.isPressed` → 장치 직접 조회(가장 단순)

---

## 7. Animator (애니메이션)

- **Animator Controller**(`Assets/Animation/PlayerAnimator.controller`) = 애니메이션 상태 기계.
  Unity의 **Animator 창**(GUI)에서 시각적으로 편집한다. 우리는 코드로 생성했지만 열어보면 노드로 보임.
- **파라미터**: 코드 → 애니메이터로 넘기는 값
  - `Speed` (float): 현재 속력. 이 값으로 Idle↔Walk↔Run 을 섞음
  - `Grounded` (bool): 바닥에 있나
  - `Jump` (trigger): 한 번 발동하고 자동으로 꺼지는 신호
- **Blend Tree**: `Speed` 하나로 여러 클립을 부드럽게 섞는 것.
  `Speed 0 → Idle`, `2 → Walk`, `5.5 → Run`. 중간값이면 두 클립을 비율로 혼합.
- **Humanoid 리타게팅**: Mixamo 애니를 X Bot 뼈대에 맞춰 재사용하는 기능.
  캐릭터를 바꿔도 같은 애니가 돌아간다.
- 코드에서: `animator.SetFloat("Speed", 값, 댐핑, deltaTime)` — 댐핑을 주면 값이 확 안 튀고 부드럽게.

---

## 8. 이 프로젝트 파일 지도

| 파일 | 종류 | 역할 |
|---|---|---|
| `Assets/Scripts/PlayerMovement.cs` | 런타임 | 이동·점프·회전, 애니 파라미터 전달. **가장 먼저 읽을 것** |
| `Assets/Scripts/ThirdPersonCamera.cs` | 런타임 | 마우스 오빗 카메라, 벽 뚫림 방지 |
| `Assets/Editor/SceneBootstrap.cs` | 에디터 전용 | `Tools ▸ Build Sample Scene` — 씬을 코드로 생성 |
| `Assets/Editor/CharacterSetup.cs` | 에디터 전용 | `Tools ▸ Setup Character` — FBX 설정 + Animator 생성 + 모델 장착 |

- **런타임 스크립트**: 게임 실행 중(Play) 동작. 빌드에 포함됨.
- **에디터 스크립트**: `Assets/Editor/` 폴더에 있으면 에디터에서만 실행. `#if UNITY_EDITOR` 로 감쌈.
  메뉴 추가는 `[MenuItem("Tools/...")]`. 빌드에는 안 들어감.

---

## 9. 직접 해볼 실험 (숫자만 바꿔보기)

Play를 멈추고, Hierarchy에서 `Player` 클릭 → Inspector의 **Player Movement** 컴포넌트에서:

1. `Jump Height` 를 1.4 → 4 로. Play 후 점프. (높이 공식 체감)
2. `Gravity` 를 -20 → -5 로. 점프가 어떻게 달라지나? (달 중력 느낌)
3. `Rotation Speed` 를 720 → 90 으로. 방향 전환이 굼떠짐.
4. `Move Speed` 를 5 → 2 로. 걷기 애니메이션 비중이 늘어남(Blend Tree 임계값 2 근처).
5. `Main Camera` 클릭 → **Third Person Camera** 의 `Distance` 를 5 → 10, `Sensitivity` 조정.

> 인스펙터에서 바꾼 값은 **Play 중에 바꾸면 Play 끝날 때 원상복구**된다. 영구히 바꾸려면 Play 정지 상태에서 수정.

---

## 10. 다음에 볼 것

- Unity Learn: "Roll-a-Ball", "3D Beginner: John Lemon" (무료, 기초)
- 개념 검색어: `GameObject Component`, `MonoBehaviour lifecycle`, `Time.deltaTime`, `CharacterController.Move`, `Animator BlendTree`, `Quaternion.LookRotation`
