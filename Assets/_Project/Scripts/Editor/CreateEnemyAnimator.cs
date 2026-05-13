using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class CreateEnemyAnimator : Editor
{
    [MenuItem("Tools/ITCLASH/Create Monster10 Animator (Enemy v2)")]
    public static void CreateAnimator()
    {
        string savePath = "Assets/_Project/Art/Monster10_EnemyV2.controller";
        
        // 1. Create the Animator Controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(savePath);
        
        // 2. Add Parameters
        controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        // 3. Load the Animation Clips
        // Using the InPlace animations to avoid Root Motion issues
        string animDir = "Assets/Imports/Stylized3DMonster/Monster10/Anim/InPlace_Anim";
        AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Monster10_Idle_InPlace.anim");
        AnimationClip walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Monster10_Walk_InPlace.anim");
        AnimationClip attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Monster10_Attack01_InPlace.anim");
        AnimationClip hitClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Monster10_GetHit_InPlace.anim");
        AnimationClip dieClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Monster10_Die_InPlace.anim");

        // If InPlace is missing, fallback to normal
        if (idleClip == null)
        {
            animDir = "Assets/Imports/Stylized3DMonster/Monster10/Anim";
            idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Monster10_Idle.anim");
            walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Monster10_Walk.anim");
            attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Monster10_Attack01.anim");
            hitClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Monster10_GetHit.anim");
            dieClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{animDir}/Monster10_Die.anim");
        }

        if (idleClip == null)
        {
            Debug.LogError("Could not find Monster10 animations!");
            return;
        }

        var rootStateMachine = controller.layers[0].stateMachine;

        // 4. Create States
        var idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleClip;

        var walkState = rootStateMachine.AddState("Walk");
        walkState.motion = walkClip;

        var attackState = rootStateMachine.AddState("Attack");
        attackState.motion = attackClip;

        var hitState = rootStateMachine.AddState("Hit");
        hitState.motion = hitClip;

        var dieState = rootStateMachine.AddState("Die");
        dieState.motion = dieClip;

        // 5. Setup Transitions
        // Idle <-> Walk
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsWalking");
        idleToWalk.hasExitTime = false;

        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWalking");
        walkToIdle.hasExitTime = false;

        // Any -> Attack -> Exit
        var anyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
        anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        anyToAttack.hasExitTime = false;
        anyToAttack.canTransitionToSelf = false;

        var attackToExit = attackState.AddExitTransition();
        attackToExit.hasExitTime = true;
        attackToExit.exitTime = 1.0f; // Wait until animation finishes

        // Any -> Hit -> Exit
        var anyToHit = rootStateMachine.AddAnyStateTransition(hitState);
        anyToHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        anyToHit.hasExitTime = false;
        anyToHit.canTransitionToSelf = false;

        var hitToExit = hitState.AddExitTransition();
        hitToExit.hasExitTime = true;
        hitToExit.exitTime = 1.0f;

        // Any -> Die (No exit)
        var anyToDie = rootStateMachine.AddAnyStateTransition(dieState);
        anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
        anyToDie.hasExitTime = false;
        anyToDie.canTransitionToSelf = false;

        AssetDatabase.SaveAssets();
        Debug.Log($"Successfully created Animator Controller at {savePath}");
        
        // Highlight in project window
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = controller;
    }
}
