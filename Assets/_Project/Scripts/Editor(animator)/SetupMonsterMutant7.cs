using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using ITCLASH.Enemies;

public class SetupMonsterMutant7 : EditorWindow
{
    [MenuItem("Tools/Setup MonsterMutant7 Strong Melee")]
    public static void Setup()
    {
        string fbxPath = "Assets/Imports/MonsterMutant 7/Base mesh/Base mesh MonsterMutant7.fbx";
        string baseControllerPath = "Assets/_Project/Art/animator/Monster10_EnemyV2.controller";
        string overridePath = "Assets/_Project/Art/animator/MonsterMutant7_StrongMelee.overrideController";
        string prefabPath = "Assets/Imports/MonsterMutant 7/Prefab/Base mesh MonsterMutant7 skin3.prefab";
        string statsPath = "Assets/_Project/Scripts/Enemy/v2/States/StrongMeleeEnemy.asset";

        // 1. Get Base Controller
        RuntimeAnimatorController baseController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(baseControllerPath);
        if (baseController == null)
        {
            Debug.LogError("Could not find base controller at " + baseControllerPath);
            return;
        }

        // 2. Create Override Controller
        AnimatorOverrideController overrideController = new AnimatorOverrideController(baseController);
        
        // 3. Extract FBX clips
        AnimationClip idle = GetClip("Assets/Imports/MonsterMutant 7/Animations/MutantMonster2@idle1.fbx");
        AnimationClip walk = GetClip("Assets/Imports/MonsterMutant 7/Animations/MutantMonster2@walk2.fbx");
        AnimationClip attack = GetClip("Assets/Imports/MonsterMutant 7/Animations/MutantMonster2@attack1.fbx");
        AnimationClip hit = GetClip("Assets/Imports/MonsterMutant 7/Animations/MutantMonster2@gethit1.fbx");
        AnimationClip die = GetClip("Assets/Imports/MonsterMutant 7/Animations/MutantMonster2@death1.fbx");

        if (idle == null || walk == null || attack == null || hit == null || die == null)
        {
            Debug.LogError($"Could not find all required animation clips. Idle:{idle!=null} Walk:{walk!=null} Attack:{attack!=null} Hit:{hit!=null} Die:{die!=null}");
            return;
        }

        // 4. Override clips
        var overrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
        overrideController.GetOverrides(overrides);
        
        for (int i = 0; i < overrides.Count; i++)
        {
            string origName = overrides[i].Key.name;
            if (origName.Contains("Idle")) overrides[i] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, idle);
            else if (origName.Contains("Walk")) overrides[i] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, walk);
            else if (origName.Contains("Attack")) overrides[i] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, attack);
            else if (origName.Contains("Hit")) overrides[i] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, hit);
            else if (origName.Contains("Die") || origName.Contains("Death")) overrides[i] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, die);
        }
        overrideController.ApplyOverrides(overrides);

        // 5. Save Override Controller
        AssetDatabase.CreateAsset(overrideController, overridePath);
        AssetDatabase.SaveAssets();

        // 6. Setup Prefab
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("Could not find prefab at " + prefabPath);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        
        // Add components
        Animator anim = instance.GetComponent<Animator>();
        if (anim == null) anim = instance.AddComponent<Animator>();
        anim.runtimeAnimatorController = overrideController;

        NavMeshAgent agent = instance.GetComponent<NavMeshAgent>();
        if (agent == null) agent = instance.AddComponent<NavMeshAgent>();

        StrongMeleeEnemy enemy = instance.GetComponent<StrongMeleeEnemy>();
        if (enemy == null) enemy = instance.AddComponent<StrongMeleeEnemy>();
        
        // Set stats
        EnemyStatsSO stats = AssetDatabase.LoadAssetAtPath<EnemyStatsSO>(statsPath);
        if (stats != null)
        {
            SerializedObject so = new SerializedObject(enemy);
            so.FindProperty("stats").objectReferenceValue = stats;
            so.ApplyModifiedProperties();
        }

        EnemyAnimationEventRelay relay = instance.GetComponent<EnemyAnimationEventRelay>();
        if (relay == null) relay = instance.AddComponent<EnemyAnimationEventRelay>();

        // Tag and Layer
        instance.tag = "Enemy";

        // Save Prefab
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        DestroyImmediate(instance);

        Debug.Log("Successfully setup MonsterMutant7 skin3 as StrongMeleeEnemy!");
    }

    private static AnimationClip GetClip(string path)
    {
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var obj in allAssets)
        {
            if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }
        return null;
    }
}
