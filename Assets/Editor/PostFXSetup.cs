// ─────────────────────────────────────────────────────────────────────────────
//  PostFXSetup.cs  —  에디터 전용. Tools ▸ Setup Post-processing
//
//  [Post-processing 이란?]
//  · 카메라가 그린 최종 화면에 "필터"를 씌우는 것 (블룸, 색보정, 비네트 등).
//  · URP에서는 씬에 놓인 Volume + VolumeProfile(효과 묶음 에셋)으로 관리한다.
//  · 카메라의 renderPostProcessing 을 켜야 실제로 적용된다.
//
//  이 스크립트가 하는 일:
//   1) VolumeProfile 에셋 생성/갱신 (Tonemapping / Bloom / ColorAdjustments / Vignette / WhiteBalance)
//   2) 씬에 "Global Volume" 오브젝트 배치하고 프로파일 연결
//   3) Main Camera 에서 포스트프로세싱 + 안티에일리어싱 켜기
//   4) 안개 + 환경광 세팅
//  값은 나중에 Assets/Settings/GameVolumeProfile 또는 씬의 Global Volume 에서 GUI로 조정 가능.
// ─────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class PostFXSetup
{
    const string ProfilePath = "Assets/Settings/GameVolumeProfile.asset";

    [MenuItem("Tools/Setup Post-processing")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Setup Post-processing", "Play 모드를 끄고 다시 실행하세요.", "확인");
            return;
        }

        // ── 1. 효과 묶음(VolumeProfile) 만들기 or 불러오기 ──────────
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            Directory.CreateDirectory("Assets/Settings");
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }

        // Tonemapping : HDR 색을 화면에 맞게 눌러주는 것. Neutral = 자연스러운 기본값.
        Configure<Tonemapping>(profile, t => t.mode.Override(TonemappingMode.Neutral));

        // Bloom : 밝은 부분이 은은하게 번짐. 과하지 않게.
        Configure<Bloom>(profile, b =>
        {
            b.threshold.Override(1.05f);                 // 이 밝기 이상만 번짐
            b.intensity.Override(0.8f);
            b.scatter.Override(0.6f);                    // 번짐 범위
            b.tint.Override(new Color(1f, 0.97f, 0.9f)); // 살짝 따뜻한 색
        });

        // Color Adjustments : 노출/대비/채도 미세 조정으로 "톤" 잡기.
        Configure<ColorAdjustments>(profile, c =>
        {
            c.postExposure.Override(0.15f);
            c.contrast.Override(12f);
            c.saturation.Override(6f);
        });

        // Vignette : 화면 가장자리를 살짝 어둡게 → 시선을 가운데로.
        Configure<Vignette>(profile, v =>
        {
            v.intensity.Override(0.26f);
            v.smoothness.Override(0.4f);
        });

        // White Balance : 전체 색온도. + 값이면 따뜻하게.
        Configure<WhiteBalance>(profile, w => w.temperature.Override(8f));

        EditorUtility.SetDirty(profile);

        // ── 2. 씬에 Global Volume 배치 ─────────────────────────────
        // isGlobal = true 면 카메라가 어디에 있든 항상 이 효과가 적용된다.
        var volGo = GameObject.Find("Global Volume");
        if (volGo == null) volGo = new GameObject("Global Volume");
        var vol = volGo.GetComponent<Volume>() ?? volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 0f;
        vol.sharedProfile = profile;

        // ── 3. 카메라에서 포스트프로세싱 켜기 ──────────────────────
        var cam = UnityEngine.Object.FindAnyObjectByType<Camera>();
        if (cam != null)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;  // ← 이게 꺼져 있으면 효과가 안 보인다
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; // 계단현상 완화
            data.antialiasingQuality = AntialiasingQuality.High;
        }

        // ── 4. 안개 + 환경광 (분위기) ─────────────────────────────
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.62f, 0.68f, 0.75f);
        RenderSettings.fogStartDistance = 25f;
        RenderSettings.fogEndDistance = 130f;
        RenderSettings.ambientMode = AmbientMode.Skybox; // 하늘색에서 은은한 환경광 계산

        // ── 저장 ──────────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[PostFXSetup] Post-processing 적용 완료. Game 뷰 확인. " +
                  "조정: Assets/Settings/GameVolumeProfile 또는 Hierarchy의 Global Volume.");
    }

    // 프로파일에 효과가 이미 있으면 가져오고, 없으면 추가한 뒤 설정 콜백 실행.
    static void Configure<T>(VolumeProfile profile, Action<T> configure) where T : VolumeComponent
    {
        if (!profile.TryGet(out T comp))
            comp = profile.Add<T>(false);
        configure(comp);
        EditorUtility.SetDirty(comp);
    }
}
#endif
