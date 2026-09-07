#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools ▸ Setup Character (Model + Animator)
///  1) X Bot / Idle / Walking / Running / Jumping FBX 를 Humanoid 로 설정하고
///     애니 클립은 X Bot 아바타로 리타겟 + 루프 설정
///  2) Assets/Animation/PlayerAnimator.controller 를 생성
///     (Speed 블렌드트리: Idle→Walk→Run, Jump 트리거)
///  3) 씬의 Player 에 모델을 자식으로 붙이고 캡슐 메시는 숨김
/// 여러 번 눌러도 같은 결과가 나오도록 만들었다.
/// </summary>
public static class CharacterSetup
{
    const string IdleName = "Idle";
    const string WalkName = "Walking";
    const string RunName = "Running";
    const string JumpName = "Jumping";

    // 캐릭터로 쓰면 안 되는(= 애니메이션 전용) FBX 이름들
    static readonly string[] AnimFileNames =
        { IdleName, WalkName, RunName, JumpName, "Action Idle To Standing Idle" };

    const string AnimDir = "Assets/Animation";
    const string ControllerPath = AnimDir + "/PlayerAnimator.controller";

    [MenuItem("Tools/Setup Character (Model + Animator)")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Setup Character", "Play 모드를 끄고 다시 실행하세요.", "확인");
            return;
        }

        string charPath = FindCharacterModel();
        string idlePath = FindModel(IdleName);
        string walkPath = FindModel(WalkName);
        string runPath = FindModel(RunName);
        string jumpPath = FindModel(JumpName);

        if (charPath == null)
        {
            Debug.LogError("[CharacterSetup] 캐릭터 FBX를 못 찾음. " +
                           "Assets/Models/ 에 스킨(메시)이 있는 캐릭터 FBX를 넣고, " +
                           "Project 창에서 그 FBX를 선택한 뒤 다시 실행하세요.");
            return;
        }
        if (idlePath == null || walkPath == null || runPath == null || jumpPath == null)
        {
            Debug.LogError($"[CharacterSetup] 애니 FBX를 못 찾음 → idle={idlePath}, " +
                           $"walk={walkPath}, run={runPath}, jump={jumpPath}");
            return;
        }
        Debug.Log($"[CharacterSetup] 캐릭터: {Path.GetFileName(charPath)}");

        // 1) 캐릭터 → Humanoid, 자체 아바타 생성
        var charImp = (ModelImporter)AssetImporter.GetAtPath(charPath);
        charImp.animationType = ModelImporterAnimationType.Human;
        charImp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        charImp.SaveAndReimport();

        var charAvatar = AssetDatabase.LoadAllAssetsAtPath(charPath).OfType<Avatar>().FirstOrDefault();
        if (charAvatar == null)
        {
            Debug.LogError("[CharacterSetup] 캐릭터 아바타 생성 실패. X Bot FBX 가 스킨(메시) 포함인지 확인하세요.");
            return;
        }

        // 2) 애니 클립 → Humanoid + 아바타 복사 + 루프
        SetupAnimationFbx(idlePath, charAvatar, loop: true);
        SetupAnimationFbx(walkPath, charAvatar, loop: true);
        SetupAnimationFbx(runPath, charAvatar, loop: true);
        SetupAnimationFbx(jumpPath, charAvatar, loop: false);

        var idleClip = LoadClip(idlePath);
        var walkClip = LoadClip(walkPath);
        var runClip = LoadClip(runPath);
        var jumpClip = LoadClip(jumpPath);

        if (idleClip == null || walkClip == null || runClip == null || jumpClip == null)
        {
            Debug.LogError($"[CharacterSetup] 클립 로드 실패 → idle={idleClip}, walk={walkClip}, " +
                           $"run={runClip}, jump={jumpClip}");
            return;
        }

        // 3) AnimatorController 재생성
        if (!AssetDatabase.IsValidFolder(AnimDir))
            AssetDatabase.CreateFolder("Assets", "Animation");
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        var ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ac.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        ac.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

        var sm = ac.layers[0].stateMachine;

        var blend = new BlendTree
        {
            name = "Locomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false
        };
        AssetDatabase.AddObjectToAsset(blend, ac);
        blend.AddChild(idleClip, 0f);
        blend.AddChild(walkClip, 2f);
        blend.AddChild(runClip, 5.5f);

        var locoState = sm.AddState("Locomotion");
        locoState.motion = blend;
        sm.defaultState = locoState;

        var jumpState = sm.AddState("Jump");
        jumpState.motion = jumpClip;

        var toJump = sm.AddAnyStateTransition(jumpState);
        toJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
        toJump.hasExitTime = false;
        toJump.duration = 0.08f;
        toJump.canTransitionToSelf = false;

        var backToLoco = jumpState.AddTransition(locoState);
        backToLoco.hasExitTime = true;
        backToLoco.exitTime = 0.75f;
        backToLoco.duration = 0.15f;

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();

        // 4) 씬의 Player 에 장착
        AttachToPlayer(charPath, ac);

        Debug.Log("[CharacterSetup] 완료. Play 를 눌러보세요. (모델 위치가 어긋나면 Player > Model 의 Position Y 를 조정)");
    }

    static void SetupAnimationFbx(string path, Avatar avatar, bool loop)
    {
        var imp = (ModelImporter)AssetImporter.GetAtPath(path);
        imp.animationType = ModelImporterAnimationType.Human;
        imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        imp.sourceAvatar = avatar;

        var clips = imp.clipAnimations;
        if (clips == null || clips.Length == 0) clips = imp.defaultClipAnimations;
        for (int i = 0; i < clips.Length; i++)
            clips[i].loopTime = loop;
        imp.clipAnimations = clips;

        imp.SaveAndReimport();
    }

    static AnimationClip LoadClip(string path)
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    static string FindModel(string fileNameNoExt)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Model"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p).Equals(fileNameNoExt, StringComparison.OrdinalIgnoreCase))
                return p;
        }
        return null;
    }

    /// <summary>
    /// 캐릭터로 쓸 FBX 를 고른다.
    ///  1) Project 창에서 스킨 메시가 있는 FBX 를 선택해 뒀으면 그것
    ///  2) 없으면 Assets/Models 바로 아래에서 "애니 전용이 아니고 스킨 메시가 있는" 모델
    ///     (여러 개면 X Bot 이 아닌 것 우선 = 새로 넣은 옷 입은 캐릭터)
    /// </summary>
    static string FindCharacterModel()
    {
        if (Selection.activeObject != null)
        {
            var p = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(p) &&
                p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) && HasSkinnedMesh(p))
                return p;
        }

        var candidates = AssetDatabase.FindAssets("t:Model", new[] { "Assets/Models" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => (Path.GetDirectoryName(p) ?? "").Replace('\\', '/') == "Assets/Models")
            .Where(p => !AnimFileNames.Contains(Path.GetFileNameWithoutExtension(p), StringComparer.OrdinalIgnoreCase))
            .Where(HasSkinnedMesh)
            .ToList();

        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        var nonXBot = candidates.FirstOrDefault(
            p => !Path.GetFileNameWithoutExtension(p).Equals("X Bot", StringComparison.OrdinalIgnoreCase));
        if (nonXBot != null)
            Debug.LogWarning($"[CharacterSetup] 캐릭터 후보 여러 개 → '{Path.GetFileName(nonXBot)}' 사용. " +
                             "다른 걸 쓰려면 Project 창에서 그 FBX 선택 후 다시 실행.");
        return nonXBot ?? candidates[0];
    }

    static bool HasSkinnedMesh(string fbxPath)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        return go != null && go.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;
    }

    static void AttachToPlayer(string charPath, AnimatorController ac)
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[CharacterSetup] 씬에 'Player' 가 없습니다. Game.unity 를 열고 " +
                           "Tools ▸ Build Sample Scene 을 먼저 실행하세요.");
            return;
        }

        // 캡슐 비주얼 숨김(콜라이더/컨트롤러는 유지)
        var mr = player.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        var nose = player.transform.Find("Nose");
        if (nose != null) UnityEngine.Object.DestroyImmediate(nose.gameObject);

        var oldModel = player.transform.Find("Model");
        if (oldModel != null) UnityEngine.Object.DestroyImmediate(oldModel.gameObject);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(charPath);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        model.name = "Model";
        model.transform.SetParent(player.transform, false);
        model.transform.localPosition = new Vector3(0f, -1f, 0f);  // 캡슐 바닥에 발 맞춤
        model.transform.localRotation = Quaternion.identity;

        var animator = model.GetComponent<Animator>() ?? model.AddComponent<Animator>();
        animator.runtimeAnimatorController = ac;
        animator.applyRootMotion = false;

        var pm = player.GetComponent<PlayerMovement>();
        if (pm != null) pm.animator = animator;

        EditorUtility.SetDirty(player);
        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveScene(player.scene);
    }
}
#endif
