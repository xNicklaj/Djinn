using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace Mirage.Impostors
{
    public enum ImpostorTextureType
    {
        Albedo,
        Normal,
        Mask
    }

    public enum ImpostorReductionRate
    {
        FullResolution = 1,
        HalfResolution = 2,
        QuarterResolution = 4
    }

    public class ImpostorFormatConverter
    {

        public GameObject impostor;
        public bool convertTextures = false;
        public TextureFormat targetFormatAlbedo = TextureFormat.DXT5;
        public TextureFormat targetFormatMask = TextureFormat.DXT1;
        public TextureFormat targetFormatNormal = TextureFormat.DXT1;
        public ImpostorReductionRate textureReductionRateAlbedo = ImpostorReductionRate.FullResolution;
        public ImpostorReductionRate textureReductionRateMask = ImpostorReductionRate.FullResolution;
        public ImpostorReductionRate textureReductionRateNormal = ImpostorReductionRate.FullResolution;

        public static bool CheckValidity(GameObject impostorCandidate)
        {
            bool isImpostor = impostorCandidate.GetComponent<MeshRenderer>() != null && impostorCandidate.GetComponent<MeshRenderer>().sharedMaterial.shader.name.Contains("mpostor");
            return isImpostor && impostorCandidate.scene.name == null;
        }

        public static bool Display1ChannelTextureFormat(Enum format)
        {
            switch (format)
            {
                case TextureFormat.R8:
                case TextureFormat.R16:
                case TextureFormat.RFloat:
                case TextureFormat.BC4:
                    return true;
                default:
                    return false;
            }
        }

        public static bool Display3ChannelsTextureFormat(Enum format)
        {
            switch (format)
            {
                case TextureFormat.RGB24:
                case TextureFormat.RGB48:
                case TextureFormat.RGB565:
                case TextureFormat.RGB9e5Float:
                case TextureFormat.DXT1:
                case TextureFormat.PVRTC_RGB2:
                case TextureFormat.PVRTC_RGB4:
                case TextureFormat.ETC_RGB4:
                case TextureFormat.ETC2_RGB:
                    return true;
                default:
                    return false;
            }
        }

        public static bool Display4ChannelsTextureFormat(Enum format)
        {
            switch (format)
            {
                case TextureFormat.ARGB32:
                case TextureFormat.ARGB4444:
                case TextureFormat.BGRA32:
                case TextureFormat.RGBA32:
                case TextureFormat.RGBA4444:
                case TextureFormat.RGBA64:
                case TextureFormat.RGBAFloat:
                case TextureFormat.DXT5:
                case TextureFormat.PVRTC_RGBA2:
                case TextureFormat.PVRTC_RGBA4:
                case TextureFormat.ETC2_RGBA1:
                case TextureFormat.ETC2_RGBA8:
                case TextureFormat.ASTC_4x4:
                case TextureFormat.ASTC_5x5:
                case TextureFormat.ASTC_6x6:
                case TextureFormat.ASTC_8x8:
                case TextureFormat.ASTC_10x10:
                case TextureFormat.ASTC_12x12:
                case TextureFormat.BC7:
                    return true;
                default:
                    return false;
            }
        }

        public static bool DisplayImpostorAlbedoTextureFormat(Enum format)
        {
            return Display4ChannelsTextureFormat(format);
        }

        public static bool DisplayImpostorMaskTextureFormat(Enum format)
        {
            return Display1ChannelTextureFormat(format) || Display3ChannelsTextureFormat(format);
        }

        public static bool DisplayImpostorNormalTextureFormat(Enum format)
        {
            return Display3ChannelsTextureFormat(format);
        }

        public void ReplaceCompressionAndResize()
        {
            string sourcePrefabPath = AssetDatabase.GetAssetPath(impostor);
            UnityEngine.Object[] packedObjects = AssetDatabase.LoadAllAssetsAtPath(sourcePrefabPath);
            Material packedMaterial = AssetDatabase.LoadAssetAtPath<Material>(sourcePrefabPath);

            foreach (UnityEngine.Object o in packedObjects)
            {
                if (o.GetType() == typeof(Texture2D))
                {
                    ImpostorTextureType type = o.name.Contains("Normal") ? ImpostorTextureType.Normal : o.name.Contains("Mask") ? ImpostorTextureType.Mask : ImpostorTextureType.Albedo;
                    Texture2D sourceTexture = o as Texture2D;
                    int sourceSize = sourceTexture.width;
                    Texture2D targetTexture;
                    int targetSize;

                    bool conversionValid;
                    string propertyId = "_MainTex";

                    switch (type)
                    {
                        case ImpostorTextureType.Normal:
                            targetSize = sourceSize / ((int)textureReductionRateNormal);
                            conversionValid = convertTextures && targetFormatNormal != sourceTexture.format;
                            targetTexture = conversionValid ? new Texture2D(targetSize, targetSize) : sourceTexture;
                            propertyId = "_NormalMap";
                            break;
                        case ImpostorTextureType.Mask:
                            targetSize = sourceSize / ((int)textureReductionRateMask);
                            conversionValid = convertTextures && targetFormatMask != sourceTexture.format;
                            targetTexture = conversionValid ? new Texture2D(targetSize, targetSize) : sourceTexture;
                            propertyId = "_MaskMap";
                            break;
                        case ImpostorTextureType.Albedo:
                        default:
                            targetSize = sourceSize / ((int)textureReductionRateAlbedo);
                            conversionValid = convertTextures && targetFormatAlbedo != sourceTexture.format;
                            targetTexture = conversionValid ? new Texture2D(targetSize, targetSize) : sourceTexture;
                            break;
                    }

                    if (targetTexture != null)
                    {
                        if (conversionValid)
                        {
                            targetTexture.name = sourceTexture.name;
                            Texture2D resizedTexture = ResizeTexture(sourceTexture, targetSize, targetSize);
                            targetTexture.SetPixels(resizedTexture.GetPixels());
                            targetTexture.Apply();
                            switch (type)
                            {
                                case ImpostorTextureType.Normal:
                                    EditorUtility.CompressTexture(targetTexture, targetFormatNormal, TextureCompressionQuality.Best);
                                    break;
                                case ImpostorTextureType.Mask:
                                    EditorUtility.CompressTexture(targetTexture, targetFormatMask, TextureCompressionQuality.Best);
                                    break;
                                case ImpostorTextureType.Albedo:
                                default:
                                    EditorUtility.CompressTexture(targetTexture, targetFormatAlbedo, TextureCompressionQuality.Best);
                                    break;
                            }
                            AssetDatabase.RemoveObjectFromAsset(o);
                            AssetDatabase.AddObjectToAsset(targetTexture, impostor);
                            packedMaterial.SetTexture(propertyId, targetTexture);
                            Debug.Log("Converted " + sourcePrefabPath + " " + sourceTexture.name + " (" + sourceSize + ")" + " from " + sourceTexture.format + " to " + targetTexture.format + " (" + targetSize + ")");
                        }
                        else if (targetSize != sourceSize)
                        {
                            targetTexture = ResizeTexture(sourceTexture, targetSize, targetSize);
                            AssetDatabase.RemoveObjectFromAsset(o);
                            AssetDatabase.AddObjectToAsset(targetTexture, impostor);
                            packedMaterial.SetTexture(propertyId, targetTexture);
                            EditorUtility.SetDirty(targetTexture);
                            Debug.Log("Resized " + sourcePrefabPath + " " + sourceTexture.name + " from " + sourceTexture.width + " to " + targetSize);
                        }
                    }
                }
            }
            PrefabUtility.SavePrefabAsset(impostor);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.SetDirty(impostor);
            AssetDatabase.ImportAsset(sourcePrefabPath, ImportAssetOptions.ForceUpdate);
            EditorGUIUtility.PingObject(impostor);
        }

        private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
        {
            if (source == null || (source.width == newWidth && source.height == newHeight))
                return source;
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
            Graphics.Blit(source, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D newTexture = new Texture2D(newWidth, newHeight);
            newTexture.name = source.name;
            newTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            newTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            EditorUtility.CompressTexture(newTexture, source.format, TextureCompressionQuality.Best);

            return newTexture;
        }
    }
    public class FormatConverterWindow : EditorWindow
    {
        private ImpostorFormatConverter converter;
        private List<GameObject> impostorPrefabs;
        private GUIStyle centeredStyle;
        private GUIStyle warningLabelStyle;
        private GUIStyle dropBoxStyle;
        private Vector2 scrollPosition;

        [MenuItem("Window/Mirage/Impostor Optimizer")]
        public static void ShowWindow()
        {
            EditorWindow win = GetWindow(typeof(FormatConverterWindow));
            win.titleContent = new GUIContent("Impostor Optimizer", Resources.Load<Texture>("MirageIcon"));
        }


        private void OnEnable()
        {
            converter = new ImpostorFormatConverter();
            impostorPrefabs = new List<GameObject>();
        }

        private void OnGUI()
        {
            if (centeredStyle == null)
            {
                centeredStyle = new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold

                };
            }
            if (warningLabelStyle == null)
            {
                warningLabelStyle = new GUIStyle(EditorStyles.label);
                warningLabelStyle.normal.textColor = Color.yellow;
                warningLabelStyle.fontSize = 10;
                warningLabelStyle.alignment = TextAnchor.MiddleCenter;
                warningLabelStyle.wordWrap = true;
            }
            if (dropBoxStyle == null)
            {
                dropBoxStyle = new GUIStyle(GUI.skin.box);
                dropBoxStyle.alignment = TextAnchor.MiddleCenter;
                dropBoxStyle.fontSize = 10;
                dropBoxStyle.hover.textColor = Color.green;
            }

            GUILayout.Label(Resources.Load<Texture>("MirageLogo"), centeredStyle, GUILayout.Height(96), GUILayout.ExpandWidth(true));
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            Rect dropBoxRect = GUILayoutUtility.GetRect(0, 16, GUILayout.Height(24), GUILayout.Width(156), GUILayout.ExpandWidth(true));
            GUI.skin.box = dropBoxStyle;
            GUI.SetNextControlName("DragDropBox");
            GUI.Box(dropBoxRect, "Drag and Drop impostor prefabs here", dropBoxStyle);
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button(EditorGUIUtility.IconContent("CrossIcon"), GUILayout.Width(30), GUILayout.Height(30)))
                impostorPrefabs.Clear();
            EditorGUILayout.EndHorizontal();
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
                    for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                    {
                        GameObject candidate = DragAndDrop.objectReferences[i] as GameObject;
                        if (candidate != null && candidate.GetComponent<Renderer>() != null)
                        {
                            bool found = false;
                            for (int j = 0; j < impostorPrefabs.Count; ++j)
                            {
                                if (impostorPrefabs[j] == candidate)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                if (ImpostorFormatConverter.CheckValidity(candidate))
                                    impostorPrefabs.Add(candidate);
                                else
                                    Debug.LogWarning("Object " + DragAndDrop.objectReferences[i]?.name + " is not a valid impostor prefab. Ignoring it.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Object " + DragAndDrop.objectReferences[i]?.name + " is not a valid GameObject. Ignoring it.");
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


            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUIStyle.none, GUI.skin.verticalScrollbar);
            for (int i = 0; i < impostorPrefabs.Count; ++i)
                impostorPrefabs[i] = EditorGUILayout.ObjectField(impostorPrefabs[i], typeof(GameObject), false) as GameObject;
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Separator();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            converter.convertTextures = EditorGUILayout.Toggle(
                new GUIContent(
                    "Convert Formats", 
                    "By default, impostors are stored in DXT-compressed textures. Some platforms like iOS are not compatible with DXT, this toggle will let you choose any other compatible format."
                    ),
                converter.convertTextures
                );


            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Albedo", EditorStyles.boldLabel, GUILayout.Width(80));
            if (converter.convertTextures)
                converter.targetFormatAlbedo = (TextureFormat)EditorGUILayout.EnumPopup(new GUIContent(""), converter.targetFormatAlbedo, ImpostorFormatConverter.DisplayImpostorAlbedoTextureFormat, false, GUILayout.MinWidth(80));
            converter.textureReductionRateAlbedo = (ImpostorReductionRate)EditorGUILayout.EnumPopup(converter.textureReductionRateAlbedo, GUILayout.MinWidth(80));
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Mask", EditorStyles.boldLabel, GUILayout.Width(80));
            if (converter.convertTextures)
                converter.targetFormatMask = (TextureFormat)EditorGUILayout.EnumPopup(new GUIContent(""), converter.targetFormatMask, ImpostorFormatConverter.DisplayImpostorMaskTextureFormat, false, GUILayout.MinWidth(80));
            converter.textureReductionRateMask = (ImpostorReductionRate)EditorGUILayout.EnumPopup(converter.textureReductionRateMask, GUILayout.MinWidth(80));
            EditorGUILayout.EndVertical(); 
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Normal", EditorStyles.boldLabel, GUILayout.Width(80));
            if (converter.convertTextures)
                converter.targetFormatNormal = (TextureFormat)EditorGUILayout.EnumPopup(new GUIContent(""), converter.targetFormatNormal, ImpostorFormatConverter.DisplayImpostorNormalTextureFormat, false, GUILayout.MinWidth(80));
            converter.textureReductionRateNormal = (ImpostorReductionRate)EditorGUILayout.EnumPopup(converter.textureReductionRateNormal, GUILayout.MinWidth(80));
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Separator();

            bool valid = impostorPrefabs.Count > 0 ? true : false;

            for (int i = 0; i < impostorPrefabs.Count; ++i)
            {
                valid &= ImpostorFormatConverter.CheckValidity(impostorPrefabs[i]);
                if (!valid)
                    break;
            }

            if (converter != null && valid)
            {
                if (GUILayout.Button(new GUIContent("Update Textures Settings", Resources.Load<Texture>("MirageIcon")), GUILayout.Height(42)))
                {
                    if (EditorUtility.DisplayDialog(
                        "Warning",
                        "Are you sure you want to update the texture settings? This action cannot be undone.",
                        "OK",
                        "Cancel"))
                    {
                        for (int i = 0; i < impostorPrefabs.Count; ++i)
                        {

                            converter.impostor = impostorPrefabs[i]; 
                            converter.ReplaceCompressionAndResize();
                        }
                        impostorPrefabs.Clear();
                    }
                }
            }
            else
            {
                if (impostorPrefabs.Count == 0)
                    EditorGUILayout.HelpBox(new GUIContent("Please drag impostor prefabs in the drop area above"));
                else
                    EditorGUILayout.HelpBox(new GUIContent("Error: Some prefabs are not impostors."));
            }

        }
    }

}


