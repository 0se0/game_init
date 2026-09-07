// ─────────────────────────────────────────────────────────────────────────────
//  PlayerMovement.cs  —  3인칭 캐릭터 이동/점프/회전 + 애니메이터 연동
//
//  [핵심 개념]
//  · MonoBehaviour : Unity가 실행해주는 스크립트. GameObject에 "컴포넌트"로 붙는다.
//  · CharacterController : 물리(Rigidbody) 대신 쓰는 이동 전용 콜라이더.
//    - controller.Move(이동량) 을 호출하면 벽/바닥 충돌을 알아서 막아준다.
//    - 대신 중력은 자동이 아니라 우리가 직접 더해줘야 한다.
//  · Update() : 매 프레임 1번 호출. 프레임레이트가 달라도 결과가 같도록
//    모든 이동량에 Time.deltaTime(지난 프레임 경과 시간, 초)을 곱한다.
//  · Input System : 키보드/마우스/게임패드 입력을 읽는 신형 시스템.
//    여기서는 InputAction을 "코드로" 정의해 인스펙터 설정 없이 동작하게 했다.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.InputSystem;

// 이 스크립트를 붙이면 CharacterController도 자동으로 붙는다(없으면 에러 대신 추가).
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // ── 인스펙터에서 조절 가능한 값들 ─────────────────────────────
    // public 필드는 Unity 인스펙터 창에 슬라이더/입력칸으로 노출된다.
    // [Header]는 인스펙터에서 그룹 제목을 붙여줄 뿐 동작에는 영향 없음.

    [Header("Move")]
    public float moveSpeed = 5f;       // 평상시 이동 속도 (m/s)
    public float sprintSpeed = 9f;     // Shift 눌렀을 때 속도 (m/s)
    public float rotationSpeed = 720f; // 캐릭터가 이동 방향으로 도는 속도 (도/초). 720 = 0.5초에 한 바퀴

    [Header("Jump / Gravity")]
    public float jumpHeight = 1.4f;    // 점프 최고 높이 (m). 실제 속도는 아래에서 물리식으로 역산한다.
    public float gravity = -20f;       // 중력 가속도. 현실은 -9.81이지만 게임은 보통 더 세게 줘야 "쫀득"하다.

    [Header("References")]
    [Tooltip("비우면 Camera.main 을 자동으로 사용")]
    public Transform cameraTransform;  // 이동 방향의 기준이 되는 카메라

    [Tooltip("비우면 자식에서 Animator 를 자동으로 찾음")]
    public Animator animator;          // 걷기/뛰기/점프 애니메이션을 재생하는 컴포넌트

    // ── 내부 상태(인스펙터에 안 보임) ────────────────────────────
    CharacterController controller;    // 이동을 실제로 수행하는 컴포넌트 캐시
    Vector3 velocity;                  // 수직 속도만 저장(velocity.y). x,z는 안 씀.

    // 입력 "액션". 각각 특정 키/버튼 묶음을 대표한다.
    InputAction moveAction;            // WASD → Vector2 (x: 좌우, y: 앞뒤)
    InputAction jumpAction;            // Space → 버튼

    // ── Awake() : 오브젝트가 생성될 때 딱 1번. 참조 세팅/초기화에 쓴다. ──
    void Awake()
    {
        // 같은 GameObject에 붙어있는 CharacterController를 찾아 저장.
        // 매 프레임 GetComponent를 부르면 느리므로 한 번만 캐시한다.
        controller = GetComponent<CharacterController>();

        // Move 액션: "2DVector 컴포짓" = 키 4개를 묶어 하나의 Vector2로 만든다.
        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")     // W → y +1
            .With("Down", "<Keyboard>/s")   // S → y -1
            .With("Left", "<Keyboard>/a")   // A → x -1
            .With("Right", "<Keyboard>/d"); // D → x +1
        moveAction.AddBinding("<Gamepad>/leftStick"); // 게임패드 왼쪽 스틱도 같은 액션에 연결

        // Jump 액션: 스페이스바 또는 게임패드 A(남쪽) 버튼
        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");

        // 카메라 참조가 비어 있으면 "MainCamera" 태그가 달린 카메라를 자동으로 사용
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Animator 참조가 비어 있으면 자식 오브젝트(예: "Model")에서 찾아온다
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    // ── OnEnable/OnDisable : 컴포넌트가 켜지고 꺼질 때 ──
    // InputAction은 Enable()해야 입력을 읽기 시작하고, 꺼질 때 Disable()로 정리한다.
    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
    }

    // Shift(좌/우 아무거나) 눌림 여부. Input System의 키보드 장치를 직접 조회한다.
    // static = 인스턴스 상태를 안 쓰는 순수 함수라는 뜻.
    static bool SprintHeld()
    {
        var kb = Keyboard.current;                 // 현재 연결된 키보드(없으면 null)
        return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
    }

    // ── Update() : 매 프레임 실행되는 본체 ─────────────────────────
    void Update()
    {
        // (1) 접지 판정
        // CharacterController.isGrounded 는 "직전 Move() 호출에서 바닥에 닿았나"를 알려준다.
        // 그래서 프레임 맨 앞에서 읽어 "지난 프레임 기준" 접지 상태로 쓴다.
        bool grounded = controller.isGrounded;

        // 바닥에 있고 아래로 떨어지는 중이면, 수직 속도를 작은 음수로 고정.
        // 0으로 두면 경사/계단에서 붕 떠버리므로 살짝 눌러 붙인다.
        if (grounded && velocity.y < 0f)
            velocity.y = -2f;

        // (2) 입력 → 이동 방향 계산
        Vector2 input = moveAction.ReadValue<Vector2>(); // WASD 결과. 예) W = (0, 1)

        Vector3 move;
        if (cameraTransform != null)
        {
            // 카메라가 보는 방향을 기준으로 앞/오른쪽 벡터를 구한다.
            Vector3 fwd = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            fwd.y = 0f; right.y = 0f;   // 수평 성분만 (위/아래로 안 걷도록)
            fwd.Normalize(); right.Normalize(); // 길이를 1로 (방향만 남김)

            // "오른쪽 * 좌우입력 + 앞 * 앞뒤입력" = 카메라 기준 이동 방향
            move = right * input.x + fwd * input.y;
        }
        else
        {
            // 카메라가 없으면 월드 좌표 그대로
            move = new Vector3(input.x, 0f, input.y);
        }

        // 대각선 입력(W+D 등)이면 벡터 길이가 √2 ≈ 1.41 이 되어 더 빨라진다.
        // 길이가 1을 넘으면 1로 잘라 대각선도 같은 속도가 되게 한다.
        if (move.sqrMagnitude > 1f) move.Normalize();

        // (3) 점프
        // WasPressedThisFrame() = "이번 프레임에 막 눌렸다"(누르고 있는 내내가 아님).
        if (grounded && jumpAction.WasPressedThisFrame())
        {
            // v = √(2 * g * h) : 높이 h까지 도달하는 초기 상승 속도(물리 공식).
            // gravity가 음수라 -2f * gravity 로 부호를 뒤집는다.
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (animator != null) animator.SetTrigger("Jump"); // 점프 애니메이션 1회 재생 신호
        }

        // (4) 중력 적용 : 매 프레임 아래로 가속
        velocity.y += gravity * Time.deltaTime;

        // (5) 실제 이동 : 수평 + 수직을 "한 번에" 한 프레임당 1회만 Move.
        //     (두 번 나눠 부르면 isGrounded 판정이 불안정해진다 — 예전 점프 버그의 원인)
        float speed = SprintHeld() ? sprintSpeed : moveSpeed;
        Vector3 displacement = move * speed + Vector3.up * velocity.y;
        controller.Move(displacement * Time.deltaTime); // deltaTime을 곱해 "속도 → 이번 프레임 이동량"

        // (6) 애니메이터에 현재 상태 전달
        if (animator != null)
        {
            // controller.velocity = 방금 Move로 실제 움직인 속도. 수평 속력만 뽑는다.
            Vector3 planar = controller.velocity;
            planar.y = 0f;
            // 세 번째/네 번째 인자는 "댐핑" — 값이 확 튀지 않고 0.12초에 걸쳐 부드럽게 따라가게 함.
            animator.SetFloat("Speed", planar.magnitude, 0.12f, Time.deltaTime);
            animator.SetBool("Grounded", grounded);
        }

        // (7) 캐릭터를 이동 방향으로 회전
        if (move.sqrMagnitude > 0.0001f) // 거의 안 움직이면 회전 안 함(0 벡터 방향은 정의 불가)
        {
            Quaternion target = Quaternion.LookRotation(move); // move 방향을 바라보는 회전값
            // 현재 회전 → 목표 회전으로 "이번 프레임 만큼만" 조금씩 회전(뚝 돌지 않게)
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, rotationSpeed * Time.deltaTime);
        }
    }
}
