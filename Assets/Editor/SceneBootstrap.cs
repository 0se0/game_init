// ─────────────────────────────────────────────────────────────────────────────
//  SceneBootstrap.cs  —  에디터 전용 스크립트 (게임 실행에는 포함 안 됨)
//
//  [에디터 스크립트란?]
//  · Assets/Editor/ 폴더 안에 있으면 "에디터에서만" 컴파일/실행된다.
//  · using UnityEditor;  → 에디터 전용 API (메뉴, 씬 저장, 임포트 설정 등)
//  · #if UNITY_EDITOR ~ #endif  → 빌드(실제 게임)에서는 이 코드가 통째로 빠진다.
//  · [MenuItem("Tools/...")]  → 상단 메뉴바에 항목을 추가. 누르면 아래 함수 실행.
//
//  보통 씬은 에디터 GUI로 손으로 배치하지만, 여기서는 "코드로" 만들어
//  누구나 버튼 하나로 똑같은 씬을 재현할 수 있게 했다.
// ─────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools ▸ Build Sample Scene 메뉴로 실행하면 플레이 가능한 3인칭 샘플 씬을
/// 코드로 구성해 Assets/Scenes/Game.unity 에 저장한다.
/// 씬 파일을 손으로 편집하지 않으므로 몇 번을 눌러도 같은 결과가 나온다.
/// </summary>
public static class SceneBootstrap
{
    const string ScenePath = "Assets/Scenes/Game.unity";
    const string MaterialDir = "Assets/Materials";

    [MenuItem("Tools/Build Sample Scene")]
    public static void BuildSampleScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Build Sample Scene",
                "Play 모드를 끄고 다시 실행하세요.", "확인");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // 기본 조명 = 석양 각도/색 (LightingSetup 과 동일한 톤)
        var light = Object.FindAnyObjectByType<Light>();
        if (light != null)
        {
            light.transform.rotation = Quaternion.Euler(10f, 25f, 0f);
            light.color = new Color(1f, 0.72f, 0.45f);
            light.intensity = 0.9f;
        }

        // --- 바닥 ---
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(6f, 1f, 6f); // 60 x 60
        Paint(ground, "GroundMat", new Color(0.22f, 0.26f, 0.24f));

        // --- 장애물 / 지형지물 ---
        Box("Box_Small", new Vector3(4f, 0.5f, 3f), Vector3.one, new Color(0.72f, 0.32f, 0.30f));
        Box("Box_Big", new Vector3(-3f, 1f, 6f), new Vector3(2f, 2f, 2f), new Color(0.30f, 0.42f, 0.70f));
        Box("Platform", new Vector3(0f, 0.5f, 11f), new Vector3(5f, 1f, 5f), new Color(0.62f, 0.60f, 0.32f));
        Box("Wall", new Vector3(-9f, 1.5f, 2f), new Vector3(1f, 3f, 18f), new Color(0.50f, 0.50f, 0.55f));

        // --- 플레이어(캡슐) ---
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.1f, 0f);
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = Vector3.zero;
        Paint(player, "PlayerMat", new Color(0.92f, 0.72f, 0.20f));

        // 정면 표시용 코(작은 큐브)
        var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Nose";
        nose.transform.SetParent(player.transform, false);
        nose.transform.localPosition = new Vector3(0f, 0.35f, 0.45f);
        nose.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        Object.DestroyImmediate(nose.GetComponent<BoxCollider>());
        Paint(nose, "NoseMat", new Color(0.10f, 0.10f, 0.12f));

        var move = player.AddComponent<PlayerMovement>();

        // --- 카메라 ---
        var cam = Object.FindAnyObjectByType<Camera>();
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        cam.gameObject.name = "Main Camera";
        cam.transform.position = new Vector3(0f, 3f, -5f);
        var tpc = cam.gameObject.AddComponent<ThirdPersonCamera>();
        tpc.target = player.transform;
        move.cameraTransform = cam.transform;

        // --- 저장 + 빌드 세팅 등록 ---
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);

        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenes.Exists(s => s.path == ScenePath))
        {
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[SceneBootstrap] '{ScenePath}' 생성 완료. Play 버튼을 누르세요. " +
                  "(WASD 이동 / 마우스 시점 / Space 점프 / Shift 달리기 / Esc 커서잠금 토글)");
    }

    // ── helpers ──────────────────────────────────────────────

    static GameObject Box(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        Paint(go, name + "Mat", color);
        return go;
    }

    static void Paint(GameObject go, string matName, Color color)
    {
        go.GetComponent<Renderer>().sharedMaterial = MakeMaterial(matName, color);
    }

    static Material MakeMaterial(string name, Color color)
    {
        string path = $"{MaterialDir}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = color;
            return existing;
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { name = name };
        mat.color = color;

        Directory.CreateDirectory(MaterialDir);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
#endif
