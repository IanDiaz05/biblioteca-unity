using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class SetupPlayerAnimator
{
    private const string ControllerPath = "Assets/Characters/Animations/PlayerAnimatorController.controller";
    private const string IdleFbx = "Assets/Characters/Animations/idle_animation.fbx";
    private const string WalkFbx = "Assets/Characters/Animations/standard_walk_animation.fbx";
    private const string ModelFbx = "Assets/Characters/CarlJohnson-GTASA.FBX";

    [MenuItem("Tools/Setup Player Animator")]
    public static void Setup()
    {
        var idleClip = LoadFirstClip(IdleFbx);
        var walkClip = LoadFirstClip(WalkFbx);
        if (idleClip == null) { Debug.LogError("[SetupPlayerAnimator] No se encontro clip en " + IdleFbx); return; }
        if (walkClip == null) { Debug.LogError("[SetupPlayerAnimator] No se encontro clip en " + WalkFbx); return; }

        var avatar = LoadFirstAvatar(ModelFbx);
        if (avatar == null) Debug.LogWarning("[SetupPlayerAnimator] No se encontro Avatar en " + ModelFbx);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        EnsureBoolParam(controller, "isWalking");

        var rootSM = controller.layers[0].stateMachine;
        var idleState = FindOrCreateState(rootSM, "Idle");
        var walkState = FindOrCreateState(rootSM, "Walk");
        idleState.motion = idleClip;
        walkState.motion = walkClip;
        rootSM.defaultState = idleState;

        EnsureTransition(idleState, walkState, "isWalking", true, 0.15f);
        EnsureTransition(walkState, idleState, "isWalking", false, 0.15f);

        AssetDatabase.SaveAssets();

        var player = GameObject.Find("Player_CJ");
        if (player == null) { Debug.LogError("[SetupPlayerAnimator] No se encontro GameObject 'Player_CJ' en la escena activa."); return; }
        var animator = player.GetComponent<Animator>();
        if (animator == null) animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        if (avatar != null) animator.avatar = avatar;
        EditorUtility.SetDirty(player);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log("[SetupPlayerAnimator] OK: controller asignado a Player_CJ. Idle=" + idleClip.name + " Walk=" + walkClip.name);
    }

    private static AnimationClip LoadFirstClip(string fbxPath)
    {
        var objs = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (var o in objs)
        {
            if (o is AnimationClip clip && !clip.name.StartsWith("__"))
                return clip;
        }
        foreach (var o in objs)
        {
            if (o is AnimationClip clip)
                return clip;
        }
        return null;
    }

    private static Avatar LoadFirstAvatar(string fbxPath)
    {
        var objs = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (var o in objs)
        {
            if (o is Avatar av)
                return av;
        }
        return null;
    }

    private static void EnsureBoolParam(AnimatorController controller, string paramName)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == paramName && p.type == AnimatorControllerParameterType.Bool)
                return;
        }
        controller.AddParameter(paramName, AnimatorControllerParameterType.Bool);
    }

    private static AnimatorState FindOrCreateState(AnimatorStateMachine sm, string name)
    {
        foreach (var s in sm.states)
        {
            if (s.state != null && s.state.name == name)
                return s.state;
        }
        return sm.AddState(name);
    }

    private static void EnsureTransition(AnimatorState from, AnimatorState to, string param, bool whenTrue, float duration)
    {
        foreach (var t in from.transitions)
        {
            if (t.destinationState == to)
            {
                ConfigureTransition(t, param, whenTrue, duration);
                return;
            }
        }
        var trans = from.AddTransition(to);
        ConfigureTransition(trans, param, whenTrue, duration);
    }

    private static void ConfigureTransition(AnimatorStateTransition t, string param, bool whenTrue, float duration)
    {
        t.hasExitTime = false;
        t.hasFixedDuration = true;
        t.duration = duration;
        t.exitTime = 0f;
        t.conditions = new AnimatorCondition[0];
        t.AddCondition(whenTrue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, param);
    }
}
