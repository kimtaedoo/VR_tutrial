using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class TutorialSceneRestorer
{
    [MenuItem("VR Tutorial/Restore Throwable Cube Only")]
    public static void RestoreThrowableCubeOnly()
    {
        GameObject throwable = GameObject.Find("[BuildingBlock] Cube");
        if (throwable == null)
        {
            Debug.LogError("Could not find [BuildingBlock] Cube in the active scene.");
            return;
        }

        RestoreThrowable(throwable);
        Selection.activeGameObject = throwable;
        EditorGUIUtility.PingObject(throwable);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Restored throwable object to stable Cube visuals.");
    }

    [MenuItem("VR Tutorial/Configure Throwable For Trigger Pinch Grab")]
    public static void ConfigureThrowableForTriggerPinchGrab()
    {
        GameObject throwable = GameObject.Find("[BuildingBlock] Cube");
        if (throwable == null)
        {
            Debug.LogError("Could not find [BuildingBlock] Cube in the active scene.");
            return;
        }

        ConfigurePinchOnlyGrab(throwable);
        Selection.activeGameObject = throwable;
        EditorGUIUtility.PingObject(throwable);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Configured throwable object for trigger-style pinch grab.");
    }

    [MenuItem("VR Tutorial/Restore Dart Practice Scene")]
    public static void RestoreDartPracticeScene()
    {
        GameObject throwable = GameObject.Find("[BuildingBlock] Cube");
        if (throwable == null)
        {
            Debug.LogError("Could not find [BuildingBlock] Cube in the active scene.");
            return;
        }

        RestoreThrowable(throwable);

        ScoreManager scoreManager = RestoreScoreManager();
        CleanMistakenTeleportTarget();
        GameObject target = RestoreTarget(scoreManager);

        Selection.activeGameObject = target;
        EditorGUIUtility.PingObject(target);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Restored dart practice scene objects and references.");
    }

    private static void RestoreThrowable(GameObject throwable)
    {
        EnsureTag("Throwable");
        throwable.tag = "Throwable";
        throwable.transform.SetPositionAndRotation(new Vector3(0f, 1.2f, 0.6f), Quaternion.identity);

        if (!throwable.TryGetComponent(out Rigidbody rigidbody))
        {
            rigidbody = throwable.AddComponent<Rigidbody>();
        }

        rigidbody.mass = 1f;
        rigidbody.linearDamping = 0f;
        rigidbody.angularDamping = 0.05f;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        if (throwable.TryGetComponent(out BoxCollider boxCollider))
        {
            boxCollider.enabled = true;
            boxCollider.center = Vector3.zero;
            boxCollider.size = Vector3.one;
        }

        if (throwable.TryGetComponent(out MeshRenderer meshRenderer))
        {
            meshRenderer.enabled = true;
        }

        RemoveDartVisuals(throwable);
        RemoveIfExists<DartVisualBuilder>(throwable);
        AlignGrabHelpersWithThrowable(throwable);
        ConfigurePinchOnlyGrab(throwable);
        ResettableObject resettableObject = AddIfMissing<ResettableObject>(throwable);
        SerializedObject serializedResettable = new SerializedObject(resettableObject);
        serializedResettable.FindProperty("waitInStartPosition").boolValue = true;
        serializedResettable.FindProperty("resetBelowY").floatValue = -0.5f;
        serializedResettable.ApplyModifiedProperties();
    }

    private static void AlignGrabHelpersWithThrowable(GameObject throwable)
    {
        Transform handGrabRoutine = throwable.transform.Find("[BuildingBlock] HandGrabInstallationRoutine");
        if (handGrabRoutine == null)
        {
            return;
        }

        handGrabRoutine.localPosition = Vector3.zero;
        handGrabRoutine.localRotation = Quaternion.identity;
        handGrabRoutine.localScale = Vector3.one;
    }

    private static void ConfigurePinchOnlyGrab(GameObject throwable)
    {
        foreach (MonoBehaviour component in throwable.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component == null)
            {
                continue;
            }

            SerializedObject serializedComponent = new SerializedObject(component);
            SerializedProperty supportedGrabTypes = serializedComponent.FindProperty("_supportedGrabTypes");
            if (supportedGrabTypes == null)
            {
                continue;
            }

            supportedGrabTypes.intValue = 1;
            serializedComponent.ApplyModifiedProperties();
        }
    }

    private static void RemoveDartVisuals(GameObject throwable)
    {
        for (int i = throwable.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = throwable.transform.GetChild(i);
            if (child.name.StartsWith("Generated Dart Visual"))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static ScoreManager RestoreScoreManager()
    {
        GameObject scoreObject = GameObject.Find("ScoreManager");
        if (scoreObject == null)
        {
            scoreObject = new GameObject("ScoreManager");
        }

        ScoreManager scoreManager = AddIfMissing<ScoreManager>(scoreObject);
        AddIfMissing<ControllerGrabInputOverride>(scoreObject);
        Text scoreText = RestoreScoreCanvas();

        SerializedObject serializedScoreManager = new SerializedObject(scoreManager);
        serializedScoreManager.FindProperty("scoreText").objectReferenceValue = scoreText;
        serializedScoreManager.ApplyModifiedProperties();

        return scoreManager;
    }

    private static Text RestoreScoreCanvas()
    {
        GameObject canvasObject = GameObject.Find("ScoreCanvas");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("ScoreCanvas", typeof(RectTransform));
        }

        Canvas canvas = AddIfMissing<Canvas>(canvasObject);
        canvas.renderMode = RenderMode.WorldSpace;

        AddIfMissing<CanvasScaler>(canvasObject);
        AddIfMissing<GraphicRaycaster>(canvasObject);

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.position = new Vector3(0f, 1.8f, 2f);
        canvasRect.rotation = Quaternion.identity;
        canvasRect.localScale = Vector3.one * 0.01f;
        canvasRect.sizeDelta = new Vector2(500f, 140f);

        Transform textTransform = canvasObject.transform.Find("ScoreText");
        GameObject textObject = textTransform != null
            ? textTransform.gameObject
            : new GameObject("ScoreText", typeof(RectTransform));
        textObject.transform.SetParent(canvasObject.transform, false);

        Text text = AddIfMissing<Text>(textObject);
        text.text = "Score: 0";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        text.fontSize = 48;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return text;
    }

    private static GameObject RestoreTarget(ScoreManager scoreManager)
    {
        GameObject target = GameObject.Find("DartTarget");
        if (target == null)
        {
            target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "DartTarget";
        }

        target.transform.SetParent(null, false);
        target.transform.SetPositionAndRotation(new Vector3(0f, 1.2f, 2.5f), Quaternion.identity);
        target.transform.localScale = new Vector3(1.5f, 1.5f, 0.2f);

        if (target.TryGetComponent(out BoxCollider boxCollider))
        {
            boxCollider.isTrigger = false;
        }

        AudioSource audioSource = AddIfMissing<AudioSource>(target);
        audioSource.playOnAwake = false;

        HitTargetColor hitTargetColor = AddIfMissing<HitTargetColor>(target);
        SerializedObject serializedHitTarget = new SerializedObject(hitTargetColor);
        serializedHitTarget.FindProperty("hitScaleMultiplier").floatValue = 1.08f;
        serializedHitTarget.FindProperty("hitScaleDuration").floatValue = 0.18f;
        serializedHitTarget.FindProperty("scoreManager").objectReferenceValue = scoreManager;
        serializedHitTarget.FindProperty("scoreAmount").intValue = 1;
        serializedHitTarget.ApplyModifiedProperties();

        return target;
    }

    private static void CleanMistakenTeleportTarget()
    {
        HitTargetColor[] hitTargets = Object.FindObjectsByType<HitTargetColor>(
            FindObjectsInactive.Include);

        foreach (HitTargetColor hitTarget in hitTargets)
        {
            if (hitTarget.gameObject.name != "Target" || hitTarget.transform.parent == null)
            {
                continue;
            }

            Object.DestroyImmediate(hitTarget);
        }
    }

    private static T AddIfMissing<T>(GameObject gameObject) where T : Component
    {
        if (!gameObject.TryGetComponent(out T component))
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    private static void RemoveIfExists<T>(GameObject gameObject) where T : Component
    {
        if (gameObject.TryGetComponent(out T component))
        {
            Object.DestroyImmediate(component);
        }
    }

    private static void EnsureTag(string tagName)
    {
        foreach (string existingTag in UnityEditorInternal.InternalEditorUtility.tags)
        {
            if (existingTag == tagName)
            {
                return;
            }
        }

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
    }
}
