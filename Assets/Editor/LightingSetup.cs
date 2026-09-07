// ─────────────────────────────────────────────────────────────────────────────
//  LightingSetup.cs  —  에디터 전용. Tools ▸ Match Lighting (Dusk)
//
//  저녁 스카이박스(golden_bay)에 맞춰 씬의 빛을 통일한다.
//   · Directional Light = "태양". 각도가 곧 해의 위치, 색/세기가 시간대 느낌을 만든다.
//   · Ambient(환경광) = 그림자 속에도 들어오는 하늘 반사광. 여기선 스카이박스에서 계산.
//   · Fog(안개) = 먼 물체를 배경색에 섞어 거리감·분위기.
//  씬을 다시 만들지 않고 "지금 열린 씬"만 조정하므로 스카이박스/포스트FX는 유지된다.
// ─────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class LightingSetup
{
    [MenuItem("Tools/Match Lighting (Dusk)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Match Lighting", "Play 모드를 끄고 다시 실행하세요.", "확인");
            return;
        }

        // ── 1. 태양(Directional Light) ──────────────────────────
        Light sun = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) { sun = l; break; }

        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(10f, 25f, 0f); // 해를 지평선 가까이 = 석양
            sun.color = new Color(1f, 0.72f, 0.45f);                 // 노을 주황색
            sun.intensity = 0.9f;                                    // 대낮보다 약하게
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.7f;                               // 그림자 살짝 연하게
        }

        // ── 2. 환경광 ───────────────────────────────────────────
        RenderSettings.ambientMode = AmbientMode.Skybox; // 하늘색에서 은은한 환경광
        RenderSettings.ambientIntensity = 1.1f;
        DynamicGI.UpdateEnvironment();                   // 스카이박스 바뀐 것 반영해 재계산

        // ── 3. 안개 (저녁 색) ───────────────────────────────────
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.34f, 0.39f, 0.52f);
        RenderSettings.fogStartDistance = 20f;
        RenderSettings.fogEndDistance = 110f;

        // ── 4. 바닥색 (대낮 초록 → 차분하게) ────────────────────
        var groundMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GroundMat.mat");
        if (groundMat != null)
        {
            groundMat.color = new Color(0.22f, 0.26f, 0.24f);
            EditorUtility.SetDirty(groundMat);
        }

        // ── 저장 ───────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[LightingSetup] 저녁 톤으로 조명·환경광·안개·바닥 조정 완료. Game 뷰 확인.");
    }
}
#endif
