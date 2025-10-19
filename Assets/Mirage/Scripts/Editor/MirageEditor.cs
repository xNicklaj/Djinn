/*
 * Copyright (c) Léo CHAUMARTIN 2024
 * All Rights Reserved
 * 
 * File: MirageEditor.cs
 * 
 * The main Mirage editor window
 */

using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.Collections.Generic;

namespace Mirage.Impostors.Elements
{
    using Core;
    using System;

    /// <summary>
    /// Enum holding the sphere type to decide the sequence of camera positions during baking
    /// The smart sphere (or pseudo-fibonacci) is experimental, smoothing along two latitude  
    /// samples won't work yet.
    /// </summary>

    /// <summary>
    ///  The main Mirage editor window. 
    /// </summary>
    public class MirageEditor : EditorWindow
    {
        /// <summary>
        /// Same LOD colors used in the LODGroup inspector
        /// </summary>
        private Color[] lodColors = new Color[9] {
        new Color(0.23f, 0.27f, 0.12f),
        new Color(0.18f, 0.21f, 0.26f),
        new Color(0.16f, 0.25f, 0.29f),
        new Color(0.25f, 0.14f, 0.12f),
        new Color(0.20f, 0.18f, 0.25f),
        new Color(0.32f, 0.22f, 0.11f),
        new Color(0.35f, 0.32f, 0.04f),
        new Color(0.32f, 0.27f, 0.12f),
        new Color(0.32f, 0f, 0f)
    };
        public GameObject commonAncestor;
        public List<MeshFilter> targetMeshFilters;
        private ImpostorPreset settings;
        private ImpostorLODGroupPreset lodGroupSettings;
        private ImpostorGeometrySolver geometrySolver;

        string outputPath = "Mirage/Generated/";
        string[] resOptions = { "128", "256", "512", "1024", "2048", "4096" };
        string[] typeNames = { "UV Sphere", "Pseudo-Fibonacci (Experimental)" };
        string[] lightingOptions = { "Surface Estimation", "Use Sun Source (static)", "New Surface Estimation (beta)" };
        string[] optimizationTabOptions = new string[] { "POVs", "Layout" };
        Vector2 scrollPosition = new Vector2();
        Vector2 mouseDrag = Vector2.zero;

        private PreviewRenderUtility previewRenderUtility;
        private Camera previewCamera;
        private IBakingEngine bakingEngine;


        int optimizationTab = 0;
        public bool generate = false;

        private GUIStyle centeredStyle;
        private GUIStyle centeredHandleStyle;
        private GUIStyle titleStyle;
        private GUIStyle pathCreatedStyle;
        private GUIStyle impostorLODGroupStyle;
        private GUIStyle pathInputStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle dropBoxStyle;
        private GUIStyle smallFont;
        private GUIStyle labelDefault;
        private GUIStyle labelValid;
        private GUIStyle labelWarning;
        private RenderTexture renderTexture;
        private int impostorSize;

        [MenuItem("Window/Mirage/Impostors")]
        public static void ShowWindow()
        {
            EditorWindow win = GetWindow(typeof(MirageEditor));
            win.titleContent = new GUIContent("Mirage", Resources.Load<Texture>("MirageIcon"));
            win.minSize = new Vector2(400, 500);
        }

        /// <summary>
        /// Method to create a new menu item to open the online help
        /// </summary>
        [MenuItem("Window/Mirage/Impostors Documentation")]
        public static void ShowHelp()
        {
            Help.BrowseURL("https://leochaumartin.com/wiki/index.php/Mirage");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += this.OnSceneGUI;
            previewRenderUtility = new PreviewRenderUtility(true);
            //previewRenderUtility.lights[0].intensity = 100f;
            //previewRenderUtility.lights[0].range = 1000f;
            previewCamera = previewRenderUtility.camera;
            previewCamera.orthographic = true;
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 10000f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
        }
        private void OnDisable()
        {
            if (previewRenderUtility != null)
            {
                previewRenderUtility.Cleanup();
            }
            SceneView.duringSceneGui -= this.OnSceneGUI;
        }



        /// <summary>
        /// Unity editor's rendering loop
        /// </summary>
        void OnGUI()
        {
            InitializeDataEventually();
            InitializeStylesEventually();

            GUILayout.Label(Resources.Load<Texture>("MirageLogo"), centeredStyle, GUILayout.Height(96f), GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            GameObject newCommonAncestor = null;
            bool modifiedTargetsManually = false;
            if (MirageEditorUtilities.BeginSection("Target", false, () =>
            {
                newCommonAncestor = EditorGUILayout.ObjectField(commonAncestor, typeof(GameObject), true, GUILayout.Height(28)) as GameObject;
            }, true))
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                Rect dropBoxRect = GUILayoutUtility.GetRect(0, 16, GUILayout.Height(38), GUILayout.ExpandWidth(true));
                GUI.skin.box = dropBoxStyle;
                GUI.SetNextControlName("DragDropBox");
                GUI.Box(dropBoxRect, "Either set the common ancestor object above or\ndrag and drop individual mesh filters here", dropBoxStyle);
                if (dropBoxRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.DragUpdated)
                    {
                        GUI.FocusControl("DragDropBox");
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        Event.current.Use();
                    }
                    else if (Event.current.type == EventType.DragPerform)
                    {
                        modifiedTargetsManually = true;
                        for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                        {
                            if ((DragAndDrop.objectReferences[i] as GameObject).GetComponent<MeshFilter>() != null)
                            {
                                bool found = false;
                                for (int j = 0; j < targetMeshFilters.Count; ++j)
                                {
                                    if (targetMeshFilters[j] == null)
                                    {
                                        MeshFilter candidate = (DragAndDrop.objectReferences[i] as GameObject).GetComponent<MeshFilter>();
                                        string status = MirageEditorUtilities.ValidateCandidate(candidate, out bool valid);
                                        if (!valid)
                                        {
                                            Debug.LogWarning("[Mirage] Target " + targetMeshFilters[j].name + " is invalid - " + status);
                                            targetMeshFilters[j] = null;
                                        }
                                        else
                                        {
                                            targetMeshFilters[j] = candidate;
                                            found = true;
                                        }
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    MeshFilter candidate = (DragAndDrop.objectReferences[i] as GameObject).GetComponent<MeshFilter>();
                                    string status = MirageEditorUtilities.ValidateCandidate(candidate, out bool valid);
                                    if (valid)
                                    {
                                        targetMeshFilters.Add(null);
                                        targetMeshFilters[targetMeshFilters.Count - 1] = candidate;
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"[Mirage] Target {candidate?.name} is invalid - {status}");
                                    }
                                }
                            }
                        }
                        Event.current.Use();
                    }
                }
                else
                {
                    if (GUI.GetNameOfFocusedControl() == "DragDropBox")
                    {
                        GUI.FocusControl(null);
                    }
                }
                if (GUILayout.Button(EditorGUIUtility.IconContent("CreateAddNew"), GUILayout.Width(38), GUILayout.Height(38)))
                {
                    targetMeshFilters.Add(null);
                    modifiedTargetsManually = true;
                }
                EditorGUILayout.EndHorizontal();

                GUIContent deleteIcon = EditorGUIUtility.IconContent("d_Toolbar Minus@2x");
                List<int> indexesToRemove = new List<int>();

                for (int i = 0; i < targetMeshFilters.Count; ++i)
                {
                    EditorGUILayout.BeginHorizontal();
                    MeshFilter newTargetMeshFilter = EditorGUILayout.ObjectField(targetMeshFilters[i] == null ? "None" : targetMeshFilters[i].name, targetMeshFilters[i], typeof(MeshFilter), true) as MeshFilter;
                    if (newTargetMeshFilter != targetMeshFilters[i])
                    {
                        string status = MirageEditorUtilities.ValidateCandidate(newTargetMeshFilter, out bool valid);
                        if (valid || newTargetMeshFilter == null)
                        {
                            if (newTargetMeshFilter != null && newTargetMeshFilter.GetComponent<MeshRenderer>() != null && !targetMeshFilters.Contains(newTargetMeshFilter))
                            {
                                targetMeshFilters[i] = newTargetMeshFilter;
                                modifiedTargetsManually = true;
                            }
                            else
                            {
                                if (newTargetMeshFilter != null)
                                    Debug.LogWarning($"[Mirage] Ignoring duplicate or invalid object {newTargetMeshFilter?.name}");
                                targetMeshFilters[i] = null;
                            }
                        }
                        else
                        {
                            if (newTargetMeshFilter != null)
                                Debug.LogWarning($"[Mirage] Target {newTargetMeshFilter.name} is invalid - {status}");
                        }
                    }
                    if (GUILayout.Button(deleteIcon, GUILayout.Width(24), GUILayout.Height(18)))
                    {
                        indexesToRemove.Add(i);
                        modifiedTargetsManually = true;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                indexesToRemove.Reverse();
                foreach (int index in indexesToRemove)
                {
                    targetMeshFilters.RemoveAt(index);
                }
                if (modifiedTargetsManually)
                {

                    commonAncestor = MirageEditorUtilities.GetCommonAncestor(targetMeshFilters);

                    Selection.SetActiveObjectWithContext(commonAncestor, null);
                }

                if (commonAncestor == null && !MirageEditorUtilities.IsEmpty(targetMeshFilters))
                {
                    EditorGUILayout.HelpBox("The given mesh filters do not have any common ancestor", MessageType.Warning, true);
                }
            }

            MirageEditorUtilities.EndSection();
            if (!modifiedTargetsManually && newCommonAncestor != commonAncestor)
            {
                commonAncestor = newCommonAncestor;

                Selection.SetActiveObjectWithContext(commonAncestor, null);
                if (commonAncestor != null)
                    targetMeshFilters = MirageEditorUtilities.GetAllDescendantMeshFilters(commonAncestor);
                else
                    targetMeshFilters.Clear();
                if (newCommonAncestor != null && MirageEditorUtilities.IsEmpty(targetMeshFilters))
                {
                    commonAncestor = null;
                    Debug.LogWarning("[Mirage] Discarding " + newCommonAncestor.name + ": No mesh filters found in its children");
                }
            }

            int atlasTextureSize = int.Parse(resOptions[settings.resIndex]);
            float memory; // Anticipating DXT compression
            if (settings.lightingMethod == LightingMethod.SurfaceEstimation)
                memory = (int)(0.0002543f * Mathf.Pow(atlasTextureSize, 2)) / 100f;
            else
                memory = (int)(0.000127f * Mathf.Pow(atlasTextureSize, 2)) / 100f;
            if (MirageEditorUtilities.BeginSection("Quality Settings", true, () => {
                EditorGUILayout.LabelField(atlasTextureSize.ToString() + "px - " + typeNames[(int)settings.type],
                    EditorStyles.centeredGreyMiniLabel, GUILayout.Height(28));
            }))
            {
                settings.resIndex = EditorGUILayout.Popup("Texture Size", settings.resIndex, resOptions);
                EditorGUILayout.LabelField("Estimated total size = " + memory + " MB", smallFont);
                settings.type = (SphereType)EditorGUILayout.Popup("Sphere Type", (int)settings.type, typeNames);
                switch (settings.type)
                {
                    case SphereType.UV:
                        settings.longitudeSamples = EditorGUILayout.IntSlider("Longitude Samples", settings.longitudeSamples, 1, 64);
                        settings.longitudeOffset = EditorGUILayout.Slider("Longitude Offset", settings.longitudeOffset, 0, 360f);
                        settings.longitudeAngularStep = EditorGUILayout.Slider("Longitude Angular Step", settings.longitudeAngularStep, 0.01f, 360f / (settings.longitudeSamples));
                        settings.latitudeSamples = EditorGUILayout.IntSlider("Latitude Samples", settings.latitudeSamples, 0, 24);
                        settings.latitudeOffset = EditorGUILayout.IntSlider("Latitude Offset", settings.latitudeOffset, -settings.latitudeSamples, settings.latitudeSamples);
                        settings.latitudeAngularStep = EditorGUILayout.Slider("Latitude Angle Step", settings.latitudeAngularStep, 0, 90f / (1f + settings.latitudeSamples + Mathf.Abs(settings.latitudeOffset)));
                        break;
                    case SphereType.PseudoFibonacci:
                        settings.longitudeSamples = EditorGUILayout.IntSlider("Density", settings.longitudeSamples, 1, 64);
                        settings.latitudeSamples = settings.longitudeSamples / 4;
                        settings.latitudeAngularStep = 90f / (1f + settings.latitudeSamples);
                        settings.latitudeOffset = 0;
                        break;
                }

                settings.lightingMethod = (LightingMethod)EditorGUILayout.Popup(new GUIContent("Lighting Method", "Scene Lighting usually gives better results but only works with a single static directional lighting."), (int)settings.lightingMethod, lightingOptions);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Load Preset"))
                {
                    GUI.FocusControl(null);
                    string absPath = EditorUtility.OpenFilePanel("Load Preset", "Assets/Mirage/Editor/Presets", "asset");

                    if (absPath.Contains(Application.dataPath))
                    {
                        string path = absPath.Substring(Application.dataPath.Length);
                        if (path.StartsWith("/"))
                            path = path.Substring(1);
                        path = "Assets/" + path;

                        ImpostorPreset preset = AssetDatabase.LoadAssetAtPath(path, typeof(ImpostorPreset)) as ImpostorPreset;
                        if (preset != null)
                        {
                            settings.resIndex = preset.resIndex;
                            settings.latitudeSamples = preset.latitudeSamples;
                            settings.latitudeOffset = preset.latitudeOffset;
                            settings.latitudeAngularStep = preset.latitudeAngularStep;
                            settings.longitudeSamples = preset.longitudeSamples;
                            settings.type = preset.type;
                            settings.lightingMethod = preset.lightingMethod;
                        }
                        else
                        {
                            Debug.LogWarning("Invalid Preset: " + absPath + ". This asset is not an impostor preset.");
                        }
                    }
                    else
                    {
                        if (absPath != "")
                            Debug.LogWarning("Invalid path: " + absPath + ". Please save the preset under the Assets/ folder");
                    }
                }

                if (GUILayout.Button("Save Preset"))
                {
                    GUI.FocusControl(null);
                    string absPath = EditorUtility.SaveFilePanel("Save Path", "Assets/Mirage/Editor/Presets", "Preset", "asset");

                    if (absPath.Contains(Application.dataPath))
                    {
                        string path = absPath.Substring(Application.dataPath.Length);
                        if (path.StartsWith("/"))
                            path = path.Substring(1);
                        path = "Assets/" + path;

                        ImpostorPreset preset = CreateInstance<ImpostorPreset>();

                        preset.resIndex = settings.resIndex;
                        preset.latitudeSamples = settings.latitudeSamples;
                        preset.latitudeOffset = settings.latitudeOffset;
                        preset.latitudeAngularStep = settings.latitudeAngularStep;
                        preset.longitudeSamples = settings.longitudeSamples;
                        preset.longitudeOffset = settings.longitudeOffset;
                        preset.type = settings.type;
                        preset.lightingMethod = settings.lightingMethod;

                        AssetDatabase.CreateAsset(preset, path);
                        EditorUtility.SetDirty(preset);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                    }
                    else
                    {
                        if (absPath != "")
                            Debug.LogWarning("Invalid path: " + absPath + ". Please save the preset under the Assets/ folder");
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
            MirageEditorUtilities.EndSection();

            if (impostorSize != atlasTextureSize / geometrySolver.Subdivisions)
            {
                impostorSize = atlasTextureSize / geometrySolver.Subdivisions;
                UpdatePreviewRenderTexture(impostorSize);
            }
            float atlasCoverage = Mathf.Round(1000 * (1f - (1f - (float)geometrySolver.POVNumber / (geometrySolver.Subdivisions * geometrySolver.Subdivisions)))) / 10f;
            if (MirageEditorUtilities.BeginSection("Information", true, () =>
            {
                EditorGUILayout.LabelField(impostorSize.ToString() + "px/imp - " + atlasCoverage.ToString() + "% atlas coverage",
                    EditorStyles.centeredGreyMiniLabel, GUILayout.Height(28));
            }))
            {
                GUILayoutOption widthLimiter = GUILayout.Width(140);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(geometrySolver.POVNumber + " POVs", widthLimiter);

                EditorGUILayout.LabelField("Atlas size = " + geometrySolver.Subdivisions + "x" + geometrySolver.Subdivisions, widthLimiter);
                EditorGUILayout.LabelField(impostorSize + "x" + impostorSize + " px / Impostor",
                    impostorSize > 20 ? labelValid : labelWarning, widthLimiter);

                EditorGUILayout.LabelField(atlasCoverage + "% atlas coverage", atlasCoverage >= 90 ? labelValid : labelWarning, widthLimiter);
                EditorGUILayout.LabelField(((int)((1f - atlasCoverage / 100f) * memory * 1000) / 1000f) + " MB unused", atlasCoverage >= 90 ? labelValid : labelWarning, widthLimiter);
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.MaxWidth(Screen.width));
                optimizationTab = GUILayout.Toolbar(optimizationTab, optimizationTabOptions);

                Rect preview = EditorGUILayout.GetControlRect(GUILayout.MaxHeight(128), GUILayout.MinWidth(200));
                EditorGUILayout.EndVertical();

                List<Vector3> vertices = geometrySolver.GetNormalizedPOVs();

                Quaternion previewRotation =
                    Quaternion.Euler(-mouseDrag.y, 0f, 0f) *
                    Quaternion.Euler(0f, -mouseDrag.x, 0f);
                if (optimizationTab == 1)
                {
                    Rect atlasLayoutRect = preview;
                    float atlasLayoutSide = Mathf.Min(preview.width, preview.height);
                    atlasLayoutRect.width = atlasLayoutRect.height = atlasLayoutSide;
                    atlasLayoutRect.x += (preview.width - atlasLayoutRect.width) / 2f;
                    EditorGUI.DrawRect(atlasLayoutRect, new Color(0.1f, 0.1f, 0.1f, 1));
                    float individualSize = atlasLayoutSide / (float)geometrySolver.Subdivisions;
                    for (int i = 0; i < geometrySolver.POVNumber; ++i)
                    {
                        Vector3 center = (Vector3)atlasLayoutRect.position + new Vector3((i % geometrySolver.Subdivisions + 0.5f) * individualSize, atlasLayoutSide - ((int)(i / geometrySolver.Subdivisions) + 0.5f) * individualSize, 0f);
                        Rect tmp = new Rect();
                        tmp.size = new Vector2(atlasLayoutSide / (float)geometrySolver.Subdivisions - 1.1f, atlasLayoutSide / (float)geometrySolver.Subdivisions - 1.1f);
                        tmp.center = center;
                        EditorGUI.DrawRect(tmp, Color.green);
                    }
                    for (int i = geometrySolver.POVNumber; i < geometrySolver.Subdivisions * geometrySolver.Subdivisions; ++i)
                    {
                        Vector3 center = (Vector3)atlasLayoutRect.position + new Vector3((i % geometrySolver.Subdivisions + 0.5f) * individualSize, atlasLayoutSide - ((int)(i / geometrySolver.Subdivisions) + 0.5f) * individualSize, 0f);
                        Rect tmp = new Rect();
                        tmp.size = new Vector2(atlasLayoutSide / (float)geometrySolver.Subdivisions - 1.1f, atlasLayoutSide / (float)geometrySolver.Subdivisions - 1.1f);
                        tmp.center = center;
                        EditorGUI.DrawRect(tmp, Color.red);
                    }
                }
                else
                {
                    EditorGUIUtility.AddCursorRect(preview, MouseCursor.Orbit);
                    if (preview.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDrag)
                    {
                        GUI.FocusControl(null);
                        mouseDrag += Event.current.delta;
                        mouseDrag.y = Mathf.Clamp(mouseDrag.y, -90f, 90f);
                        Repaint();
                    }
                    Bounds commonBounds = geometrySolver.GetBounds(targetMeshFilters);
                    Vector3 worldSpaceCenter = commonBounds.center;
                    commonBounds.center = Vector3.zero;
                    Color originalAmbientColor = RenderSettings.ambientLight;
                    RenderSettings.ambientLight = Color.white;
                    for (int i = 0; i < targetMeshFilters.Count; ++i)
                    {
                        if (targetMeshFilters[i] != null)
                        {
                            Mesh targetMesh = targetMeshFilters[i].sharedMesh;
                            targetMesh.RecalculateBounds();
                            if (targetMesh != null)
                            {
                                Material[] mats = targetMeshFilters[i].GetComponent<MeshRenderer>().sharedMaterials;
                                for (int m = 0; m < mats.Length; ++m)
                                {
                                    Material previewMat = new Material(mats[m]);
                                    Matrix4x4 translation = Matrix4x4.Translate(-worldSpaceCenter);

                                    previewRenderUtility.DrawMesh(
                                        targetMesh,
                                        Matrix4x4.Rotate(previewRotation) * translation * targetMeshFilters[i].GetComponent<MeshRenderer>().localToWorldMatrix,
                                        previewMat,
                                        m < targetMesh.subMeshCount ? m : targetMesh.subMeshCount,
                                        null,
                                        null,
                                        false);
                                }
                            }
                        }
                    }
                    preview.x += (preview.width - Mathf.Min(preview.width, preview.height)) / 2f;
                    preview.width = preview.height = Mathf.Min(preview.width, preview.height);
                    previewCamera.transform.position = new Vector3(0f, 0f, -commonBounds.size.magnitude);
                    previewCamera.transform.LookAt(Vector3.zero);
                    previewCamera.orthographicSize = geometrySolver.GetOrthgraphicCameraSize(targetMeshFilters);
                    if (targetMeshFilters.Count > 0 && renderTexture != null)
                    {
                        previewRenderUtility.BeginStaticPreview(preview);
                        previewRenderUtility.Render(true);

                        Texture2D cameraRender = previewRenderUtility.EndStaticPreview();
                        RenderTexture.active = renderTexture;
                        Graphics.Blit(cameraRender, renderTexture);
                        RenderTexture.active = null;


                        EditorGUI.DrawPreviewTexture(preview, renderTexture);
                    }
                    RenderSettings.ambientLight = originalAmbientColor;
                    Handles.color = new Color(0.3f, 1f, 1f);
                    foreach (Vector3 v in vertices)
                    {
                        Vector3 rotatedV = previewRotation * v;
                        rotatedV.y = -rotatedV.y;

                        if (Vector3.Dot(rotatedV, Vector3.back) > 0)
                            Handles.DrawSolidDisc(new Vector3(preview.x + preview.width / 2, preview.y + preview.height / 2, 0f) + rotatedV * preview.height / 2f, rotatedV.normalized, 1.5f);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            MirageEditorUtilities.EndSection();

            LODGroup lodGroup = MirageEditorUtilities.GetSingleLODGroup(targetMeshFilters, out bool lodGroupValid);
            if (MirageEditorUtilities.BeginSection("LODGroup Settings", false, () =>
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Automatic setup", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(112), GUILayout.Height(28));
                lodGroupSettings.setupLOD = EditorGUILayout.Toggle(lodGroupSettings.setupLOD, GUILayout.Width(28), GUILayout.Height(28));
                GUILayout.FlexibleSpace();
            }, true))
            {
                if (commonAncestor == null)
                {
                    EditorGUILayout.LabelField("Common ancestor cannot be null", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {

                    if (!lodGroupValid)
                    {
                        if (lodGroup != null)
                            EditorGUILayout.HelpBox("There is a conflict with the existing LODGroup(s). Make sure the source mesh filters are all either in no LOD or part of the same LOD level of the same LODGroup.", MessageType.Error);
                    }
                    else
                    {
                        bool hasLODGroup = lodGroup != null;

                        ImpostorReference reference = lodGroup != null ? lodGroup.GetComponent<ImpostorReference>() : null;

                        if (!hasLODGroup)
                        {
                            //EditorGUILayout.Separator();
                            EditorGUILayout.LabelField("LODGroup will be created automatically", pathCreatedStyle);
                            lodGroupSettings.lodPerformance = EditorGUILayout.Slider("Seamless / Performant", lodGroupSettings.lodPerformance, 0.025f, 0.5f);
                            lodGroupSettings.lodSizeCulling = EditorGUILayout.Slider("Relative Height Culling", lodGroupSettings.lodSizeCulling, 0.002f, 0.1f);

                            Rect lodPreview = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
                            EditorGUI.DrawRect(lodPreview, lodColors[0]);
                            EditorGUI.LabelField(lodPreview, "Original" + "\n100%", EditorStyles.miniBoldLabel);
                            float fullWidth = lodPreview.width;
                            float ratio = Mathf.Sqrt(lodGroupSettings.lodPerformance);
                            float xInit = lodPreview.x;
                            lodPreview.width *= ratio;
                            lodPreview.x = xInit + (1f - ratio) * fullWidth;
                            Color lodColor = lodColors[1];
                            EditorGUI.DrawRect(lodPreview, lodColor);
                            EditorGUI.LabelField(lodPreview, "Impostor" + "\n" + Mathf.Round(100f * lodGroupSettings.lodPerformance) + "%", impostorLODGroupStyle);
                            lodPreview.x += (1f - ratio) * lodPreview.width;
                            lodPreview.width *= ratio;
                            lodPreview.width = fullWidth * (Mathf.Sqrt(lodGroupSettings.lodSizeCulling));
                            lodPreview.x = xInit + fullWidth - lodPreview.width;
                            EditorGUI.DrawRect(lodPreview, lodColors[8]);
                            EditorGUI.LabelField(lodPreview, "Culled" + "\n" + Mathf.Round(100f * lodGroupSettings.lodSizeCulling) + "%", EditorStyles.miniBoldLabel);
                        }
                        else //There's a lodGroup but there's no impostors in it
                        {
                            if (reference == null || reference.impostorObject == null)
                            {
                                EditorGUILayout.LabelField("LODGroup detected - Impostor will be added in last position", pathCreatedStyle);
                                if (lodGroup.lodCount >= 2)
                                    lodGroupSettings.lodPerformance = EditorGUILayout.Slider("Seamless / Performant", lodGroupSettings.lodPerformance, 0.025f, lodGroup.GetLODs()[lodGroup.lodCount - 2].screenRelativeTransitionHeight - 0.01f);
                                else
                                    lodGroupSettings.lodPerformance = EditorGUILayout.Slider("Seamless / Performant", lodGroupSettings.lodPerformance, 0.025f, 1f);
                            }
                            else
                                EditorGUILayout.LabelField("LODGroup detected - Impostor will be replaced", pathCreatedStyle);
                            lodGroupSettings.lodSizeCulling = EditorGUILayout.Slider("Relative Height Culling", lodGroupSettings.lodSizeCulling, 0.002f, 0.1f);

                            Rect lodPreview = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
                            LOD[] existingLods = lodGroup.GetLODs();
                            float fullWidth = lodPreview.width;
                            float xInit = lodPreview.x;
                            lodPreview.x = xInit;

                            if (reference == null || reference.impostorObject == null)
                            {
                                for (int lvl = 0; lvl < existingLods.Length; ++lvl)
                                {
                                    lodPreview.width = fullWidth * (1f - Mathf.Sqrt(existingLods[lvl].screenRelativeTransitionHeight));
                                    Color lodColor = lodColors[lvl];
                                    EditorGUI.DrawRect(lodPreview, lodColor);
                                    EditorGUI.LabelField(lodPreview, "LOD " + lvl + "\n" + Mathf.Round(100f * (lvl == 0 ? 1f : existingLods[lvl - 1].screenRelativeTransitionHeight)) + "%", EditorStyles.miniBoldLabel);
                                    lodPreview.x += lodPreview.width - (lvl == 0 ? 0 : fullWidth * (1f - Mathf.Sqrt(existingLods[lvl - 1].screenRelativeTransitionHeight)));
                                }
                                lodPreview.width = fullWidth * (Mathf.Sqrt(lodGroupSettings.lodSizeCulling));
                                lodPreview.x = xInit + fullWidth - lodPreview.width;
                                EditorGUI.DrawRect(lodPreview, lodColors[8]);
                                EditorGUI.LabelField(lodPreview, "Culled" + "\n" + Mathf.Round(100f * lodGroupSettings.lodSizeCulling) + "%", EditorStyles.miniBoldLabel);

                                lodPreview.width = fullWidth * (Mathf.Sqrt(lodGroupSettings.lodPerformance)) - lodPreview.width;
                                lodPreview.x -= lodPreview.width;
                                if (lodPreview.width > 0)
                                {
                                    EditorGUI.DrawRect(lodPreview, lodColors[existingLods.Length]);
                                    EditorGUI.LabelField(lodPreview, "Impostor" + "\n" + Mathf.Round(100f * lodGroupSettings.lodPerformance) + "%", impostorLODGroupStyle);
                                }
                            }
                            else
                            {
                                for (int lvl = 0; lvl < existingLods.Length - 1; ++lvl)
                                {
                                    lodPreview.width = fullWidth * (1f - Mathf.Sqrt(existingLods[lvl].screenRelativeTransitionHeight));
                                    Color lodColor = lodColors[lvl];
                                    EditorGUI.DrawRect(lodPreview, lodColor);
                                    EditorGUI.LabelField(lodPreview, "LOD " + lvl + "\n" + Mathf.Round(100f * (lvl == 0 ? 1f : existingLods[lvl - 1].screenRelativeTransitionHeight)) + "%", EditorStyles.miniBoldLabel);
                                    lodPreview.x += lodPreview.width - (lvl == 0 ? 0 : fullWidth * (1f - Mathf.Sqrt(existingLods[lvl - 1].screenRelativeTransitionHeight)));
                                }
                                lodPreview.width = fullWidth * (Mathf.Sqrt(1f - lodGroupSettings.lodSizeCulling)) - lodPreview.x;
                                EditorGUI.DrawRect(lodPreview, lodColors[existingLods.Length]);
                                EditorGUI.LabelField(lodPreview, "Impostor" + "\n" + Mathf.Round(100f * existingLods[existingLods.Length - 2].screenRelativeTransitionHeight) + "%", impostorLODGroupStyle);

                                lodPreview.width = fullWidth * (Mathf.Sqrt(lodGroupSettings.lodSizeCulling));
                                lodPreview.x = xInit + fullWidth - lodPreview.width;
                                EditorGUI.DrawRect(lodPreview, lodColors[8]);
                                EditorGUI.LabelField(lodPreview, "Culled" + "\n" + Mathf.Round(100f * lodGroupSettings.lodSizeCulling) + "%", EditorStyles.miniBoldLabel);
                            }
                        }
                    }
                }
            }
            MirageEditorUtilities.EndSection();

            bool validPath = Directory.Exists(Application.dataPath + "/" + outputPath) || (outputPath.Contains("/") && Directory.Exists(Application.dataPath + "/" + outputPath.Substring(0, outputPath.LastIndexOf("/"))));

            pathInputStyle.normal.textColor = !validPath ? Color.yellow : Color.green;
            pathInputStyle.hover.textColor = pathInputStyle.normal.textColor;
            pathInputStyle.focused.textColor = pathInputStyle.normal.textColor;

            string processedPath = MirageEditorUtilities.ProcessPath(outputPath, commonAncestor);

            if (MirageEditorUtilities.BeginSection("Save Path", false, () => {
                EditorGUILayout.LabelField(processedPath, EditorStyles.centeredGreyMiniLabel, GUILayout.Height(28));
            }))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Assets/", GUILayout.Width(44));
                outputPath = EditorGUILayout.TextField(outputPath, pathInputStyle);

                if (GUILayout.Button(EditorGUIUtility.IconContent("Folder Icon"), GUILayout.Width(24), GUILayout.Height(18)))
                {
                    GUI.FocusControl(null);
                    string absPath = EditorUtility.SaveFolderPanel("Save Path", "Assets/" + outputPath, "Assets/" + outputPath);

                    if (absPath.Contains(Application.dataPath))
                    {
                        outputPath = absPath.Substring(Application.dataPath.Length);
                        if (outputPath.StartsWith("/"))
                            outputPath = outputPath.Substring(1);
                    }
                    else
                    {
                        if (absPath != "")
                            Debug.LogWarning("Invalid path: " + absPath + ". Please save the file under the Assets/ folder");
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField(processedPath, EditorStyles.whiteMiniLabel);
                if (outputPath.Contains("/") && !Directory.Exists(Application.dataPath + "/" + outputPath.Substring(0, outputPath.LastIndexOf("/"))))
                {
                    EditorGUILayout.LabelField("The directory will be created.", pathCreatedStyle);
                }
            }
            MirageEditorUtilities.EndSection();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Separator();
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(commonAncestor == null || !lodGroupValid);
            if (GUILayout.Button(new GUIContent("Generate Impostor", Resources.Load<Texture>("MirageIcon")), GUILayout.Height(42), GUILayout.Width(225)))
            {
                EditorUtility.DisplayProgressBar("Mirage Baking Process", "Initialization...", 0f);
                
                    float orthographicSize = geometrySolver.GetOrthgraphicCameraSize(targetMeshFilters);
                    Bounds commonBounds = geometrySolver.GetBounds(targetMeshFilters);
                    Vector3 boundsCenter = commonBounds.center;
                    List<Vector3> povs = geometrySolver.GetPOVs(orthographicSize + 0.01f);
                    List<Mesh> meshes = new();
                    List<Material[]> materials = new();
                    List<Matrix4x4> transforms = new();

                    for (int i = 0; i < targetMeshFilters.Count; ++i)
                    {
                        if (targetMeshFilters[i] == null)
                            continue;
                        meshes.Add(targetMeshFilters[i].sharedMesh);
                        materials.Add(targetMeshFilters[i].GetComponent<MeshRenderer>().sharedMaterials);
                        Matrix4x4 translation = Matrix4x4.Translate(-boundsCenter);
                        transforms.Add(translation * targetMeshFilters[i].GetComponent<MeshRenderer>().localToWorldMatrix);
                    }
                    if (GraphicsSettings.currentRenderPipeline != null && GraphicsSettings.currentRenderPipeline.name.Contains("HD"))
                        bakingEngine = new HDRPEditorBakingEngine(povs,
                        meshes,
                        materials,
                        transforms,
                        atlasTextureSize,
                        orthographicSize,
                        settings.lightingMethod
                        );
                    else
                        bakingEngine = new EditorBakingEngine(povs,
                        meshes,
                        materials,
                        transforms,
                        atlasTextureSize,
                        orthographicSize,
                        settings.lightingMethod
                        );

                    EditorUtility.DisplayProgressBar("Mirage Baking Process", "Baking ColorMaps...", 0.25f);
                    Texture2D colorMap = bakingEngine.ComputeColorMaps();
                    bakingEngine.ApplyPostProcessing(ref colorMap, colorMap);

                    Texture2D normalMap = null;
                    Texture2D maskMap = null;
                    if (settings.lightingMethod != LightingMethod.UseSunSource)
                    {
                        EditorUtility.DisplayProgressBar("Mirage Baking Process", "Baking NormalMaps...", 0.4f);
                        normalMap = bakingEngine.ComputeNormalMaps();
                        bakingEngine.ApplyPostProcessing(ref normalMap, colorMap);
                        EditorUtility.DisplayProgressBar("Mirage Baking Process", "Baking NormalMaps...", 0.55f);
                        maskMap = bakingEngine.ComputeMaskMaps();
                        //bakingEngine.ApplyPostProcessing(ref maskMap, colorMap);
                    }
                    bakingEngine.Cleanup();

                    EditorUtility.DisplayProgressBar("Mirage Baking Process", "Packing the impostor prefab...", 0.75f);
                    ImpostorPacker packer = new ImpostorPacker();
                    packer.PackImpostor(
                        commonAncestor,
                        targetMeshFilters,
                        colorMap,
                        normalMap,
                        maskMap,
                        orthographicSize,
                        geometrySolver.Subdivisions,
                        boundsCenter,
                        settings,
                        lodGroupSettings,
                        processedPath
                        );
                    EditorUtility.DisplayProgressBar("Mirage Baking Process", "Baking successful", 1f);
                    EditorUtility.ClearProgressBar();
                
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            EditorGUILayout.Separator();
            GUILayout.BeginHorizontal();


            if (GUILayout.Button(EditorGUIUtility.IconContent("_Help"), GUILayout.Height(20)))
            {
                Help.BrowseURL("https://leochaumartin.com/wiki/index.php/Mirage");
            }
            if (GUILayout.Button(Resources.Load<Texture>("discord"), GUILayout.Height(20), GUILayout.Width(24)))
            {
                Help.BrowseURL("https://discord.gg/kYwzdvAt8q");
            }
            if (GUILayout.Button(Resources.Load<Texture>("youtube"), GUILayout.Height(20), GUILayout.Width(24)))
            {
                Help.BrowseURL("https://www.youtube.com/channel/UCTGysKJUd9Njaxqju4-c6_w");
            }
            if (GUILayout.Button(Resources.Load<Texture>("twitter"), GUILayout.Height(20), GUILayout.Width(24)))
            {
                Help.BrowseURL("https://twitter.com/LeoChaumartin");
            }
            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("mail"), "support@leochaumartin.com"), GUILayout.Height(20), GUILayout.Width(24)))
            {
                Help.BrowseURL("mailto:chaumartinleo@gmail.com");
            }
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();
            GUILayout.Label("v2.2.0 ", smallFont, GUILayout.Height(8), GUILayout.Width(132));
            GUILayout.Label("Mirage © 2024-2025 ", smallFont, GUILayout.Height(8));
            GUILayout.Label("Léo Chaumartin ", smallFont, GUILayout.Height(10));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        void UpdatePreviewRenderTexture(int size)
        {
            renderTexture = new RenderTexture(size, size, 32);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }

        private void InitializeStylesEventually()
        {
            wantsMouseMove = true;
            if (centeredStyle == null)
            {
                centeredStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold

                };
            }
            if (true || centeredHandleStyle == null)
            {
                centeredHandleStyle = new GUIStyle
                {
                    contentOffset = new Vector2(-50, -25),
                    fixedHeight = 50
                };
            }
            if (titleStyle == null)
            {
                titleStyle = EditorStyles.boldLabel;
                titleStyle.alignment = TextAnchor.MiddleLeft;
            }
            if (pathCreatedStyle == null)
            {
                pathCreatedStyle = new GUIStyle(EditorStyles.label);
                pathCreatedStyle.normal.textColor = Color.green;
                pathCreatedStyle.fontSize = 10;
                pathCreatedStyle.alignment = TextAnchor.MiddleCenter;
            }
            if (impostorLODGroupStyle == null)
            {
                impostorLODGroupStyle = new GUIStyle(EditorStyles.miniBoldLabel);
                impostorLODGroupStyle.normal.textColor = new Color(0.15f, 1f, 0.5f);
            }
            if (pathInputStyle == null)
            {
                pathInputStyle = new GUIStyle(EditorStyles.textField);
                pathInputStyle.focused.textColor = pathInputStyle.normal.textColor;
                pathInputStyle.hover.textColor = pathInputStyle.normal.textColor;
            }
            if (subHeaderStyle == null)
            {
                subHeaderStyle = EditorStyles.foldoutHeader;
                subHeaderStyle.fontStyle = FontStyle.Normal;
                subHeaderStyle.stretchWidth = true;
            }
            if (dropBoxStyle == null)
            {
                dropBoxStyle = new GUIStyle(GUI.skin.button);
                dropBoxStyle.alignment = TextAnchor.MiddleCenter;
                dropBoxStyle.fontSize = 10;
                dropBoxStyle.hover.textColor = Color.green;
                dropBoxStyle.wordWrap = true;
            }
            if (smallFont == null)
            {
                smallFont = new GUIStyle
                {
                    fontSize = 8,
                    alignment = TextAnchor.UpperRight
                };
                smallFont.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            }
            if (labelDefault == null)
            {
                labelDefault = new GUIStyle
                {
                    fontSize = 12,

                };
                labelDefault.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            }
            if (labelValid == null)
            {
                labelValid = new GUIStyle
                {
                    fontSize = 12,

                };
                labelValid.normal.textColor = new Color(0.1f, 0.8f, 0.2f);
            }
            if (labelWarning == null)
            {
                labelWarning = new GUIStyle
                {
                    fontSize = 12,

                };
                labelWarning.normal.textColor = new Color(0.8f, 0.8f, 0.2f);
            }
        }

        private void InitializeDataEventually()
        {
            if (settings == null)
                settings = CreateInstance<ImpostorPreset>();
            if (lodGroupSettings == null)
                lodGroupSettings = CreateInstance<ImpostorLODGroupPreset>();
            if (geometrySolver == null)
                geometrySolver = new ImpostorGeometrySolver(ref settings);
            if (targetMeshFilters == null)
            {
                targetMeshFilters = new List<MeshFilter>();
                commonAncestor = null;
            }
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (settings != null && geometrySolver != null && commonAncestor != null && Selection.activeGameObject == commonAncestor)
            {
                Bounds bds = geometrySolver.GetBounds(targetMeshFilters);
                Handles.color = new Color(0.3f, 1f, 1f);
                float radius = bds.extents.magnitude * 1.1f;
                float thickness = 3f;
                Handles.zTest = CompareFunction.Less;
                GUIContent logo = new GUIContent();
                logo.image = Resources.Load<Texture>("MirageLogo");
                Handles.Label(bds.center + 1.25f * bds.extents.magnitude * Vector3.up, logo, centeredHandleStyle);
                if (settings.type == SphereType.UV)
                {
                    float yawAngle = settings.longitudeSamples * settings.longitudeAngularStep;
                    float elevationAngle = (settings.latitudeSamples * 2) * settings.latitudeAngularStep;
                    Quaternion yawOffset = Quaternion.Euler(0f, -settings.longitudeOffset, 0f);
                    Vector3 yawFrom = yawOffset * Vector3.right;
                    Quaternion yawSamples = Quaternion.Euler(0f, -yawAngle, 0f);
                    Quaternion halfYawSamples = Quaternion.Euler(0f, -yawAngle / 2f, 0f);
                    Vector3 yawTo = yawSamples * yawFrom;
                    Vector3 bottomLeft = Vector3.RotateTowards(yawFrom, Vector3.down, (settings.latitudeSamples - settings.latitudeOffset) * settings.latitudeAngularStep * Mathf.Deg2Rad, 0f);
                    Vector3 topRight = Vector3.RotateTowards(yawTo, Vector3.up, (settings.latitudeSamples + settings.latitudeOffset) * settings.latitudeAngularStep * Mathf.Deg2Rad, 0f);
                    Vector3 centerLeft = Vector3.RotateTowards(yawFrom, Vector3.up, (settings.latitudeOffset) * settings.latitudeAngularStep * Mathf.Deg2Rad, 0f);
                    Vector3 bottomCenter = halfYawSamples * bottomLeft;
                    Handles.DrawWireArc(bds.center, Vector3.up, bottomLeft, -yawAngle, radius, thickness);
                    Handles.DrawWireArc(bds.center, Vector3.up, topRight, yawAngle, radius, thickness);
                    Handles.DrawWireArc(bds.center, yawOffset * Vector3.forward, bottomLeft, elevationAngle, radius, thickness);
                    Handles.DrawWireArc(bds.center, yawSamples * yawOffset * Vector3.forward, topRight, -elevationAngle, radius, thickness);
                    Handles.color = Color.white;
                    Handles.DrawWireArc(bds.center, Vector3.up, centerLeft, 360f, radius);
                    Handles.DrawWireArc(bds.center, halfYawSamples * yawOffset * Vector3.forward, bottomCenter, 360, radius);
                }
                else
                {
                    Handles.DrawWireDisc(bds.center, Vector3.up, radius, thickness);
                    Handles.DrawWireDisc(bds.center, Vector3.right, radius, thickness);
                    Handles.DrawWireDisc(bds.center, Vector3.forward, radius, thickness);
                }
            }
        }
    }
}
