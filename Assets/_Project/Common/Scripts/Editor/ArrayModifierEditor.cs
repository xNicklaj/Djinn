// ArrayModifierEditor.cs
// Unity Editor Tool — Blender-style Array Modifier with Gizmo snapping & rotation
// Works in Unity 2020.3+
// Put this file in Assets/Editor/

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ArrayModifierEditor : EditorWindow
{
    GameObject sourceObject;
    Transform originTransform;
    Vector3 originPosition;
    Quaternion originRotation = Quaternion.identity;
    Vector3 originScale = Vector3.one;

    int count = 5;
    Vector3 gap = new Vector3(1f, 0f, 0f);

    bool gridMode = false;
    int rows = 1;
    int columns = 5;
    float rowGap = 1f;
    float columnGap = 1f;

    bool useSourceRotation = true;
    bool useSourceScale = true;
    bool placeInWorldSpace = true;

    GameObject previewParent;

    // Gizmo system
    bool useGizmoOrigin = false;
    GameObject gizmoHandle;
    bool isRotating = false;
    float rotationSpeed = 5f;
    bool isDragging = false;
    int controlId = -1;

    [MenuItem("Tools/Array Modifier Window")]
    public static void OpenWindow()
    {
        var w = GetWindow<ArrayModifierEditor>("Array Modifier");
        w.minSize = new Vector2(420, 260);
    }

    void OnEnable()
    {
        Selection.selectionChanged += OnSelectionChanged;
        SceneView.duringSceneGui += OnSceneGUI;
        OnSelectionChanged();
    }

    void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        SceneView.duringSceneGui -= OnSceneGUI;
        ClearPreview();
        if (gizmoHandle != null)
            DestroyImmediate(gizmoHandle);
    }

    void OnSelectionChanged()
    {
        if (Selection.activeGameObject != null && sourceObject == null)
        {
            sourceObject = Selection.activeGameObject;
            originTransform = sourceObject.transform;
            originPosition = originTransform.position;
            originRotation = originTransform.rotation;
            originScale = originTransform.localScale;
            Repaint();
        }
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Array Modifier (Blender-like)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        sourceObject = (GameObject)EditorGUILayout.ObjectField("Prefab Source", sourceObject, typeof(GameObject), false);

        if (sourceObject != null && !PrefabUtility.IsPartOfPrefabAsset(sourceObject))
        {
            EditorGUILayout.HelpBox("Only prefab assets can be used with this tool.", MessageType.Warning);
            GUI.enabled = false;
        }

        useGizmoOrigin = EditorGUILayout.ToggleLeft("Use Gizmo as Origin (Drag / R to rotate)", useGizmoOrigin);
        if (useGizmoOrigin)
        {
            if (gizmoHandle == null)
                CreateGizmoHandle();
            originTransform = gizmoHandle.transform;
        }
        else if (gizmoHandle != null)
        {
            DestroyImmediate(gizmoHandle);
            gizmoHandle = null;
        }

        EditorGUILayout.Space();

        if (!useGizmoOrigin)
        {
            originTransform = (Transform)EditorGUILayout.ObjectField("Origin Transform", originTransform, typeof(Transform), true);
            if (originTransform == null)
            {
                originPosition = EditorGUILayout.Vector3Field("Position", originPosition);
                originRotation = Quaternion.Euler(EditorGUILayout.Vector3Field("Rotation", originRotation.eulerAngles));
                originScale = EditorGUILayout.Vector3Field("Scale", originScale);
            }
        }

        placeInWorldSpace = EditorGUILayout.ToggleLeft("World Space Instances", placeInWorldSpace);
        useSourceRotation = EditorGUILayout.ToggleLeft("Use Source Rotation", useSourceRotation);
        useSourceScale = EditorGUILayout.ToggleLeft("Use Source Scale", useSourceScale);

        gridMode = EditorGUILayout.ToggleLeft("Grid Mode", gridMode);

        if (!gridMode)
        {
            count = EditorGUILayout.IntField("Count", Mathf.Max(1, count));
            gap = EditorGUILayout.Vector3Field("Gap", gap);
        }
        else
        {
            columns = Mathf.Max(1, EditorGUILayout.IntField("Columns", columns));
            rows = Mathf.Max(1, EditorGUILayout.IntField("Rows", rows));
            columnGap = EditorGUILayout.FloatField("Column Gap", columnGap);
            rowGap = EditorGUILayout.FloatField("Row Gap", rowGap);
            EditorGUILayout.HelpBox("Grid origin = top-left. Columns extend +X, rows extend -Z.", MessageType.Info);
        }

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(sourceObject == null || !PrefabUtility.IsPartOfPrefabAsset(sourceObject)))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview")) CreatePreview();
            if (GUILayout.Button("Clear Preview")) ClearPreview();
            if (GUILayout.Button("Apply")) ApplyInstances();
            EditorGUILayout.EndHorizontal();
        }

        GUI.enabled = true;
    }

    void CreateGizmoHandle()
    {
        gizmoHandle = new GameObject("ArrayToolOriginHandle");
        gizmoHandle.hideFlags = HideFlags.DontSave;
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(gizmoHandle.transform);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale = Vector3.one * 0.25f;
        DestroyImmediate(sphere.GetComponent<Collider>());
        var renderer = sphere.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Standard"));
        renderer.sharedMaterial.color = new Color(0, 1, 1, 0.5f);
    }

    // -------------------------------- Scene GUI --------------------------------
    void OnSceneGUI(SceneView sceneView)
    {
        if (!useGizmoOrigin || gizmoHandle == null)
            return;

        Event e = Event.current;
        if (controlId == -1) controlId = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlId);

        // Rotation toggle
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R)
        {
            isRotating = !isRotating;
            e.Use();
            sceneView.Repaint();
        }

        // Draw gizmo visuals
        Handles.color = Color.cyan;
        Handles.SphereHandleCap(0, gizmoHandle.transform.position, gizmoHandle.transform.rotation, 0.25f, EventType.Repaint);
        Handles.ArrowHandleCap(0, gizmoHandle.transform.position, gizmoHandle.transform.rotation, 0.6f, EventType.Repaint);

        if (isRotating)
        {
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(gizmoHandle.transform.position, gizmoHandle.transform.up, 0.7f);
        }

        Vector2 mousePos = e.mousePosition;

        // Start drag when clicking near gizmo
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Vector2 guiPoint = HandleUtility.WorldToGUIPoint(gizmoHandle.transform.position);
            float dist = Vector2.Distance(guiPoint, mousePos);
            if (dist <= 18f)
            {
                isDragging = true;
                GUIUtility.hotControl = controlId;
                e.Use();
            }
        }

        // Drag behavior
        if (e.type == EventType.MouseDrag && e.button == 0 && GUIUtility.hotControl == controlId && isDragging)
        {
            if (!isRotating)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    Undo.RecordObject(gizmoHandle.transform, "Move Array Gizmo");
                    gizmoHandle.transform.position = hit.point;
                    gizmoHandle.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                }
            }
            else
            {
                float delta = e.delta.x;
                Undo.RecordObject(gizmoHandle.transform, "Rotate Array Gizmo");
                gizmoHandle.transform.Rotate(Vector3.up, delta * rotationSpeed, Space.World);
            }

            e.Use();
            sceneView.Repaint();
        }

        // Release control on mouse up
        if (e.type == EventType.MouseUp && e.button == 0 && GUIUtility.hotControl == controlId)
        {
            isDragging = false;
            GUIUtility.hotControl = 0;
            e.Use();
            sceneView.Repaint();
        }
    }

    // -------------------------------- Array logic --------------------------------
    Vector3 GetOriginPosition() => originTransform ? originTransform.position : originPosition;
    Quaternion GetOriginRotation() => originTransform ? originTransform.rotation : originRotation;
    Vector3 GetOriginScale() => originTransform ? originTransform.localScale : originScale;

    void CreatePreview()
    {
        ClearPreview();
        if (sourceObject == null || !PrefabUtility.IsPartOfPrefabAsset(sourceObject)) return;

        previewParent = new GameObject("ArrayPreview_" + sourceObject.name);
        previewParent.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;

        if (!gridMode)
        {
            for (int i = 0; i < count; i++)
                InstantiateInstanceAt(GetOriginPosition(), GetOriginRotation(), GetOriginScale(), gap * i, previewParent.transform, true);
        }
        else
        {
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                    InstantiateInstanceAt(GetOriginPosition(), GetOriginRotation(), GetOriginScale(),
                        new Vector3(c * columnGap, 0, -r * rowGap), previewParent.transform, true);
        }

        SceneView.RepaintAll();
    }

    void ClearPreview()
    {
        if (previewParent != null)
        {
            DestroyImmediate(previewParent);
            previewParent = null;
        }
    }

    void ApplyInstances()
    {
        if (sourceObject == null || !PrefabUtility.IsPartOfPrefabAsset(sourceObject)) return;

        GameObject parent = new GameObject("ArrayResult_" + sourceObject.name);
        Undo.RegisterCreatedObjectUndo(parent, "Create array parent");

        if (!gridMode)
        {
            for (int i = 0; i < count; i++)
                InstantiateInstanceAt(GetOriginPosition(), GetOriginRotation(), GetOriginScale(), gap * i, parent.transform, false);
        }
        else
        {
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                    InstantiateInstanceAt(GetOriginPosition(), GetOriginRotation(), GetOriginScale(),
                        new Vector3(c * columnGap, 0, -r * rowGap), parent.transform, false);
        }

        ClearPreview();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    void InstantiateInstanceAt(Vector3 originPos, Quaternion originRot, Vector3 originScl, Vector3 offset, Transform parent, bool isPreview)
    {
        var prefab = PrefabUtility.GetCorrespondingObjectFromSource(sourceObject);
        if (prefab == null) return;

        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Vector3 pos = originPos + originRot * offset;

        newObj.transform.position = pos;
        newObj.transform.rotation = useSourceRotation ? sourceObject.transform.rotation : originRot;
        newObj.transform.localScale = useSourceScale ? sourceObject.transform.localScale : originScl;
        newObj.transform.SetParent(parent, true);

        if (isPreview)
            newObj.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
        else
        {
            Undo.RegisterCreatedObjectUndo(newObj, "Create array instance");
            newObj.hideFlags = HideFlags.None;
        }
    }
}
