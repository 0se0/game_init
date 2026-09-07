# MyGame — 3인칭 캐릭터 컨트롤러 & 씬 연출

Unity로 처음 만든 학습 프로젝트. 3인칭 캐릭터의 이동·점프·카메라·애니메이션을 직접 구현하고,
URP 포스트프로세싱과 HDRI 스카이박스로 **저녁 항구** 톤의 씬을 연출했다.

<!-- 스크린샷: Play 모드에서 Cmd+Shift+4 로 캡처 → Docs/images/ 에 저장 후 아래 경로 수정 -->
<!-- ![gameplay](Docs/images/gameplay.png) -->

---

## 주요 구현

| 영역 | 내용 |
|---|---|
| 이동 | `CharacterController` 기반, 카메라 기준 방향 이동, 이동 방향으로 부드럽게 회전 |
| 점프 / 중력 | 중력 직접 적용, 목표 높이에서 초기 속도 역산 `v = √(2gh)`, 접지 판정 처리 |
| 카메라 | 마우스 오빗(yaw/pitch) 3인칭 팔로우, `Physics.Linecast` 로 벽 클리핑 방지 |
| 애니메이션 | Humanoid 리타게팅 + 1D 블렌드트리(Idle→Walk→Run) + Jump 트리거 |
| 씬 연출 | URP Volume 포스트프로세싱(Bloom·색보정·비네트), HDRI 스카이박스, 조명/안개 톤 매칭 |
| 개발 편의 | 씬·캐릭터·포스트FX·스카이박스·조명을 `Tools ▸ …` 에디터 메뉴로 원클릭·재현 가능하게 구성 |

## 조작

| 키 | 동작 |
|---|---|
| `WASD` / 스틱 | 이동 |
| 마우스 | 시점 회전 |
| `Space` | 점프 |
| `Shift` | 달리기 |
| `Esc` | 커서 잠금 토글 |

## 기술 스택

`Unity 6.5` · `C#` · `URP` · `New Input System` · `CharacterController` · `Mixamo / Humanoid` · `URP Post-processing` · `HDRI Skybox`

## 프로젝트 구조

```
Assets/
├─ Scripts/
│  ├─ PlayerMovement.cs        # 이동·점프·회전, 애니메이터 파라미터 전달
│  └─ ThirdPersonCamera.cs     # 오빗 팔로우 카메라
├─ Editor/                     # 에디터 전용 자동화 (빌드 미포함)
│  ├─ SceneBootstrap.cs        # Tools ▸ Build Sample Scene
│  ├─ CharacterSetup.cs        # Tools ▸ Setup Character (Model + Animator)
│  ├─ PostFXSetup.cs           # Tools ▸ Setup Post-processing
│  ├─ SkyboxSetup.cs           # Tools ▸ Apply Skybox From Image
│  └─ LightingSetup.cs         # Tools ▸ Match Lighting (Dusk)
├─ Scenes/Game.unity           # 메인 씬 (코드로 생성됨)
└─ LearningNotes.md            # 유니티 개념 학습 정리

Docs/
├─ GameConcept.md              # 게임 컨셉 (해질 무렵 항구 워킹 심)
└─ Portfolio.md                # 포트폴리오 요약
```

## 열기

1. Unity Hub 에서 `6000.5.10f1` (또는 호환 버전) 으로 이 폴더 열기
2. `Assets/Scenes/Game.unity` 열기 → `▶ Play`
3. 씬을 처음부터 다시 만들려면: `Tools ▸ Build Sample Scene` → `Setup Character` → `Setup Post-processing` → `Apply Skybox From Image` → `Match Lighting (Dusk)`

## 크레딧

- 캐릭터 / 애니메이션: [Mixamo](https://www.mixamo.com) (Adobe)
- 스카이박스 HDRI: [Poly Haven](https://polyhaven.com) (CC0)
- 스카이박스 컨셉 실험: [Blockade Labs Skybox AI](https://skybox.blockadelabs.com)

> 학습 프로젝트입니다. 스크립트 작성·디버깅에 AI 페어프로그래밍을 활용했고, 코드 내용은 직접 이해·검토했습니다.
