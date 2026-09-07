# MyGame — 3인칭 캐릭터 컨트롤러 & 씬 연출

> Unity로 처음 만든 학습 프로젝트. 3인칭 캐릭터의 이동·점프·카메라·애니메이션을 직접 구현하고,
> URP 포스트프로세싱과 HDRI 스카이박스로 "저녁 항구" 톤의 씬을 연출했다.

**기간** 2026.09 · **역할** 개인 · **분류** 학습 프로젝트
**저장소** https://github.com/0se0/game_init

---

## 사용 기술

`Unity 6.5` · `C#` · `URP` · `New Input System` · `CharacterController` · `Mixamo / Humanoid Animator` · `URP Volume(Post-processing)` · `HDRI Skybox`

---

## 구현 내용

### 게임플레이

- **캐릭터 이동** — 카메라 기준 방향으로 이동, 이동 방향으로 부드럽게 회전 (`Quaternion.RotateTowards`)
- **점프 / 중력** — CharacterController에 중력을 직접 적용, 목표 높이에서 초기 속도를 물리 공식으로 역산 (`v = √(2gh)`)
- **3인칭 카메라** — 마우스 오빗(yaw/pitch) 팔로우 카메라, `Physics.Linecast` 로 벽 뚫림 방지, 커서 잠금 토글
- **애니메이션** — Animator BlendTree(Idle→Walk→Run)를 속도 값으로 블렌딩, 점프는 Trigger, Mixamo 클립을 Humanoid 리타게팅으로 재사용

### 씬 연출 (아트 파이프라인)

- **포스트프로세싱** — URP Volume에 Tonemapping·Bloom·Color Adjustments·Vignette·White Balance 스택 구성
- **스카이박스** — Blockade Labs(AI 생성)로 컨셉 실험 → 무료 플랜 다운로드 제약을 확인하고 Poly Haven CC0 HDRI로 최종 채택, `Skybox/Panoramic` 머티리얼에 매핑
- **조명 매칭** — 저녁 하늘에 맞춰 Directional Light 각도·색·세기, 환경광, 안개, 바닥 머티리얼을 일관되게 조정

### 개발 편의

- **에디터 자동화** — `[MenuItem]` 스크립트로 샘플 씬 생성 / 캐릭터(모델+애니메이터) 셋업 / 포스트FX / 스카이박스 / 조명을 각각 원클릭·재현 가능하게 구성 (씬 파일 수동 편집 없이 코드로 씬 구성, 몇 번 실행해도 같은 결과)

---

## 배운 점

- GameObject–Component 구조와 MonoBehaviour 생명주기(`Awake` / `Update` / `LateUpdate`)
- `Time.deltaTime` 을 이용한 프레임 독립적 이동
- 물리(Rigidbody)와 CharacterController의 차이, 접지 판정(`isGrounded`) 처리
- Input System에서 InputAction을 코드로 정의하고 조합 바인딩(2DVector) 구성
- Animator 파라미터(float / bool / trigger)와 BlendTree, Humanoid 애니메이션 리타게팅
- URP 렌더링 파이프라인에서 Volume 기반 포스트프로세싱과 스카이박스 / 환경광의 관계
- AI 생성 에셋의 라이선스·제약을 검토하고 대안을 선택하는 판단

---

## 조작

`WASD` 이동 · `Mouse` 시점 · `Space` 점프 · `Shift` 달리기 · `Esc` 커서 잠금 토글

---

## 기획 방향

이동 프로토타입을 바탕으로 **"해질 무렵, 조용한 항구 마을을 걷는 짧은 워킹 심"** 으로 확장 계획.

- **로그라인** — 오랜만에 고향 항구로 돌아온 인물이, 어둠이 내리기 전까지 마을을 돌며 작은 일들을 마무리한다. 전투도 실패도 없이 시간과 분위기만.
- **코어 루프** — 항구 도착 → 마을 산책 → 표시된 지점 3곳에서 상호작용 → 선착장 도착 → 배가 떠나며 마무리 (한 판 5~10분)
- **1차 확장** — 회색 박스를 CC0 프롭(부두·나무통·벤치·가로등·배)으로 교체, 상호작용 지점 + UI 프롬프트, 선착장 엔딩 연출
- **레퍼런스** — *A Short Hike*, *Journey* 의 이동감 / 풍경이 주인공인 워킹 심

> 컨셉·시나리오 초안은 AI(Claude)와 대화로 정리하고 직접 검토·수정했다.

---

## 스크린샷

<!-- Play 모드에서 캡처 (Cmd+Shift+4). 캐릭터가 움직이는 컷 1~2장 + 저녁 항구 전경 1장 -->

---

## 코드 (github.com/0se0/game_init)

- `Assets/Scripts/PlayerMovement.cs` — 이동 / 점프 / 회전, 애니메이터 연동
- `Assets/Scripts/ThirdPersonCamera.cs` — 오빗 카메라
- `Assets/Editor/` — SceneBootstrap · CharacterSetup · PostFXSetup · SkyboxSetup · LightingSetup (에디터 자동화)
- `Assets/LearningNotes.md` — 유니티 개념 학습 정리
- `Docs/GameConcept.md` — 게임 컨셉 상세

---

*학습 프로젝트입니다. 스크립트 작성·디버깅에 AI 페어프로그래밍을 활용했고, 코드 내용은 직접 이해·검토했습니다.*
