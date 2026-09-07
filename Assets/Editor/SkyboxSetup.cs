// ─────────────────────────────────────────────────────────────────────────────
//  SkyboxSetup.cs  —  에디터 전용. Tools ▸ Apply Skybox From Image
//
//  [스카이박스란?]
//  · 씬을 감싸는 아주 큰 "하늘 상자". 카메라를 어디로 돌려도 배경으로 보인다.
//  · 여기서는 360° 파노라마 이미지(정방형 위경도 = equirectangular)를
//    Skybox/Panoramic 셰이더 머티리얼에 넣어 하늘로 쓴다.
//  · RenderSettings.skybox 에 머티리얼을 넣으면 씬 배경 + 환경광 소스가 된다.
//
//  사용법: Assets/Skybox/ 폴더에 이미지 1장을 넣고 이 메뉴 실행.
// ─────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SkyboxSetup
{
    const string Dir = "Assets/Skybox";
    const string MatPath = Dir + "/SkyboxMat.mat";

    [MenuItem("Tools/Apply Skybox From Image")]
    public static void Apply()
    {
        if (!Directory.Exists(Dir))
        {
            EditorUtility.DisplayDialog("Apply Skybox",
                "먼저 Assets/Skybox/ 폴더를 만들고 파노라마 이미지 1장을 넣으세요.", "확인");
            return;
        }

        // (1) Project 창에서 이미지를 선택해 뒀으면 그걸 쓴다.
        string texPath = null;
        if (Selection.activeObject is Texture sel)
        {
            var p = AssetDatabase.GetAssetPath(sel);
            if (p.StartsWith(Dir + "/")) texPath = p;
        }

        // (2) 선택이 없으면 Assets/Skybox 바로 아래(하위폴더 제외)의 첫 이미지.
        texPath ??= AssetDatabase.FindAssets("t:Texture", new[] { Dir })
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => (Path.GetDirectoryName(p) ?? "").Replace('\\', '/') == Dir);

        if (texPath == null)
        {
            EditorUtility.DisplayDialog("Apply Skybox",
                "Assets/Skybox/ 에 파노라마 이미지를 넣고, Project 창에서 그 이미지를 클릭한 뒤 다시 실행하세요.", "확인");
            return;
        }

        // ── 텍스처 임포트 설정: 파노라마에 맞게 ──
        if (AssetImporter.GetAtPath(texPath) is TextureImporter importer)
        {
            importer.wrapModeU = TextureWrapMode.Repeat;   // 좌우로 이어지도록
            importer.wrapModeV = TextureWrapMode.Clamp;    // 위/아래 끝은 늘어나지 않게
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 8192;                // 원본이 크면 살림
            importer.SaveAndReimport();
        }

        var tex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);

        // ── 스카이박스 머티리얼 만들기/갱신 ──
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Skybox/Panoramic"));
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        mat.SetTexture("_MainTex", tex);
        mat.SetFloat("_Mapping", 1);    // 1 = Latitude Longitude Layout (equirectangular)
        mat.SetFloat("_ImageType", 0);  // 0 = 360 Degrees
        mat.SetFloat("_Exposure", 1f);
        mat.SetFloat("_Rotation", 0f);
        EditorUtility.SetDirty(mat);

        // ── 씬 환경에 적용 ──
        RenderSettings.skybox = mat;
        DynamicGI.UpdateEnvironment(); // 새 하늘색 기준으로 환경광 다시 계산

        AssetDatabase.SaveAssets();
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[SkyboxSetup] '{Path.GetFileName(texPath)}' 를 스카이박스로 적용했습니다. " +
                  "밝기/회전은 Assets/Skybox/SkyboxMat 에서 조정.");
    }
}
#endif
