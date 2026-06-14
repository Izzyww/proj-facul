#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class FoxyAnimationBuilder
{
    private const string FoxyPath = "Assets/Sunny Land/Characters/Foxy";
    private const string AnimFolder = "Assets/Sunny Land/Characters/Foxy/Animations";
    private const string PrefabPath = "Assets/Sunny Land/Characters/Foxy/player-idle-1_0.prefab";

    [MenuItem("Tools/Foxy/Build Animator")]
    public static void BuildAnimator()
    {
        EnsureFolder(FoxyPath, "Animations");

        Sprite[] idleSprites = LoadSprites($"{FoxyPath}/Sprites/idle");
        Sprite[] runSprites = LoadSprites($"{FoxyPath}/Sprites/run");
        Sprite[] jumpSprites = LoadSprites($"{FoxyPath}/Sprites/jump");

        AnimationClip idleClip = CreateSpriteClip("Foxy_Idle", idleSprites, 8f, true);
        AnimationClip runClip = CreateSpriteClip("Foxy_Run", runSprites, 12f, true);
        AnimationClip jumpClip = CreateSpriteClip("Foxy_Jump", jumpSprites, 10f, false);

        SaveClip(idleClip, $"{AnimFolder}/Foxy_Idle.anim");
        SaveClip(runClip, $"{AnimFolder}/Foxy_Run.anim");
        SaveClip(jumpClip, $"{AnimFolder}/Foxy_Jump.anim");

        AnimatorController controller = BuildController(idleClip, runClip, jumpClip);
        AssignToPlayerPrefab(controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Foxy Animator created in Assets/Sunny Land/Characters/Foxy/Animations");
    }

    private static void EnsureFolder(string parentPath, string folderName)
    {
        string fullPath = $"{parentPath}/{folderName}";
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parentPath, folderName);
        }
    }

    private static Sprite[] LoadSprites(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        List<Sprite> sprites = new List<Sprite>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                sprites.Add(sprite);
            }
        }

        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites.ToArray();
    }

    private static AnimationClip CreateSpriteClip(string clipName, Sprite[] sprites, float frameRate, bool loop)
    {
        AnimationClip clip = new AnimationClip { frameRate = frameRate, name = clipName };

        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[i]
            };
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }

    private static void SaveClip(AnimationClip clip, string assetPath)
    {
        AnimationClip existingClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existingClip == null)
        {
            AssetDatabase.CreateAsset(clip, assetPath);
            return;
        }

        existingClip.ClearCurves();
        EditorUtility.CopySerialized(clip, existingClip);
        EditorUtility.SetDirty(existingClip);
    }

    private static AnimatorController BuildController(AnimationClip idleClip, AnimationClip runClip, AnimationClip jumpClip)
    {
        string controllerPath = $"{AnimFolder}/Foxy.controller";
        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        AnimatorState idleState = stateMachine.AddState("Idle", new Vector3(300f, 0f, 0f));
        idleState.motion = idleClip;
        stateMachine.defaultState = idleState;

        AnimatorState runState = stateMachine.AddState("Run", new Vector3(300f, 100f, 0f));
        runState.motion = runClip;

        AnimatorState jumpState = stateMachine.AddState("Jump", new Vector3(300f, 200f, 0f));
        jumpState.motion = jumpClip;

        AnimatorStateTransition idleToRun = idleState.AddTransition(runState);
        idleToRun.hasExitTime = false;
        idleToRun.duration = 0f;
        idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");

        AnimatorStateTransition runToIdle = runState.AddTransition(idleState);
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0f;
        runToIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");

        AnimatorStateTransition idleToJump = idleState.AddTransition(jumpState);
        idleToJump.hasExitTime = false;
        idleToJump.duration = 0f;
        idleToJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");

        AnimatorStateTransition runToJump = runState.AddTransition(jumpState);
        runToJump.hasExitTime = false;
        runToJump.duration = 0f;
        runToJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");

        AnimatorStateTransition jumpToIdle = jumpState.AddTransition(idleState);
        jumpToIdle.hasExitTime = false;
        jumpToIdle.duration = 0f;
        jumpToIdle.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
        jumpToIdle.AddCondition(AnimatorConditionMode.Less, 0.01f, "Speed");

        AnimatorStateTransition jumpToRun = jumpState.AddTransition(runState);
        jumpToRun.hasExitTime = false;
        jumpToRun.duration = 0f;
        jumpToRun.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
        jumpToRun.AddCondition(AnimatorConditionMode.Greater, 0.01f, "Speed");

        return controller;
    }

    private static void AssignToPlayerPrefab(AnimatorController controller)
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);

        Animator animator = prefabRoot.GetComponent<Animator>();
        if (animator == null)
        {
            animator = prefabRoot.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;

        MonoBehaviour[] behaviours = prefabRoot.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour != null && behaviour.GetType().Name == "PlayerAnimation")
            {
                Object.DestroyImmediate(behaviour);
            }
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }
}
#endif
