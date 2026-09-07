// ─────────────────────────────────────────────────────────────────────────────
//  ThirdPersonCamera.cs  —  타겟(플레이어) 뒤를 도는 3인칭 오빗 카메라
//
//  [핵심 개념]
//  · 이 스크립트는 "Main Camera" GameObject에 붙는다. transform = 카메라 자신.
//  · yaw(좌우 회전) / pitch(상하 회전) 두 각도만 저장하고,
//    매 프레임 "타겟 주변 구(sphere) 위의 한 점"으로 카메라를 옮긴다.
//  · LateUpdate() 를 쓰는 이유:
//    - Update()에서 플레이어가 먼저 움직이고, 그 "최종 위치"를 보고 카메라를 배치해야
//      화면이 덜 떨린다. LateUpdate는 모든 Update가 끝난 뒤 호출된다.
//  · 회전 표현:
//    - 오일러 각(pitch, yaw, roll) : 사람이 이해하기 쉬움. 여기서 입력받는 값.
//    - 쿼터니언(Quaternion) : Unity 내부가 실제로 쓰는 회전 표현. Euler로 변환해 사용.
// ─────────────────────────────────────────────────────────────────────────────

using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                               // 따라다닐 대상(플레이어)
    public Vector3 focusOffset = new Vector3(0f, 1.5f, 0f); // 발밑이 아니라 가슴/머리 높이를 보도록 올려줌

    [Header("Orbit")]
    public float distance = 5f;        // 타겟에서 카메라까지 거리 (m)
    public float sensitivity = 0.12f;  // 마우스 감도. 마우스 delta(픽셀) * 이 값 = 회전 각도
    public float minPitch = -20f;      // 아래로 볼 수 있는 한계 각도
    public float maxPitch = 70f;       // 위로 볼 수 있는 한계 각도

    [Header("Cursor")]
    public bool lockCursor = true;     // true면 마우스 커서를 화면 중앙에 숨기고 고정(FPS/3인칭 표준)

    // ── 내부 상태 ──
    float yaw;          // 좌우 각도(누적)
    float pitch = 15f;  // 상하 각도(시작값: 약간 위에서 내려다봄)
    InputAction lookAction; // 마우스 이동량(delta)을 읽는 액션

    void Awake()
    {
        // 마우스의 "이번 프레임 이동량"을 Vector2로 읽는 액션
        lookAction = new InputAction("Look", InputActionType.Value, expectedControlType: "Vector2");
        lookAction.AddBinding("<Mouse>/delta");

        // 시작 시 카메라 좌우 각도를 타겟이 보는 방향으로 맞춰둔다(안 하면 처음에 홱 돌아감)
        if (target != null) yaw = target.eulerAngles.y;
    }

    void OnEnable()
    {
        lookAction.Enable();
        ApplyCursor(lockCursor);
    }

    void OnDisable() => lookAction.Disable(); // 화살표 함수: { lookAction.Disable(); } 와 동일

    void Update()
    {
        // 에디터에서 테스트할 때 Esc로 커서 잠금을 켰다 껐다(그래야 다른 창 클릭 가능)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            lockCursor = !lockCursor;   // 토글
            ApplyCursor(lockCursor);
        }
    }

    // 실제 카메라 배치. 모든 Update()가 끝난 뒤 호출됨.
    void LateUpdate()
    {
        if (target == null) return; // 타겟이 없으면 아무것도 안 함(에러 방지)

        // (1) 마우스 입력으로 각도 갱신
        //     커서가 잠겨있을 때만 회전(잠금 해제 상태에선 UI 조작 중이라고 보고 무시)
        Vector2 look = lockCursor ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        yaw += look.x * sensitivity;    // 마우스 좌우 → yaw
        pitch -= look.y * sensitivity;  // 마우스 상하 → pitch (화면 좌표는 위가 +y라 부호 반대)
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch); // 위/아래로 뒤집히지 않게 제한

        // (2) 각도 → 회전값
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

        // (3) 카메라 위치 계산
        Vector3 focus = target.position + focusOffset;      // 실제로 바라볼 지점
        // "focus에서 rot 방향의 앞쪽으로 distance만큼 뒤로" = 타겟 뒤 카메라 자리
        Vector3 desired = focus - rot * Vector3.forward * distance;

        // (4) 벽 뚫림 방지: focus와 카메라 사이에 뭔가 있으면 그 앞으로 카메라를 당긴다
        if (Physics.Linecast(focus, desired, out RaycastHit hit))
            desired = hit.point + hit.normal * 0.2f; // 벽에서 0.2m 띄움

        // (5) 위치와 회전을 한 번에 적용(카메라가 focus를 바라보도록)
        transform.SetPositionAndRotation(desired, rot);
    }

    // 커서 잠금/표시 상태를 실제로 적용
    static void ApplyCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
