using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
using System.IO;
using UnityEditor.UIElements;

public class CameraPreviewWindow : EditorWindow
{
    private Camera previewCamera;
    private RenderTexture previewRenderTexture;
    private VisualElement previewElement;
    private Button screenshotButton;

    private const string ScreenshotFolder = "Assets/_Project/Data/Pictures/";

#if UNITY_POST_PROCESSING_STACK_V2
    private PostProcessVolume postProcessVolume;
    private PostProcessProfile postProcessProfile;
#endif

    // Settings controls
    private Slider fovSlider;
    private Label fovValueLabel;
    private IntegerField resolutionHeightField;
    private EnumField aspectRatioField;

    // Current settings
    private float cameraFOV = 60f;
    private int pictureHeight = 1080;
    private AspectRatio currentAspectRatio = AspectRatio._16_9;

    private enum AspectRatio
    {
        _16_9,
        _4_3
    }

    // === Color Correction ===
    private Slider exposureSlider;
    private Slider contrastSlider;
    private Slider saturationSlider;
    private ColorField tintColorField;

    private float exposure = 0f;
    private float contrast = 1f;
    private float saturation = 1f;
    private Color tintColor = Color.white;

    private Material colorCorrectionMaterial;

    [MenuItem("Tools/Camera Preview")]
    public static void ShowExample()
    {
        var wnd = GetWindow<CameraPreviewWindow>();
        wnd.titleContent = new GUIContent("Camera Preview");
        wnd.minSize = new Vector2(400, 300);
    }

    private void OnEnable()
    {
        CreateScreenshotDirectory();
        CreatePreviewCamera();
        RecreateRenderTextureAndResize();

#if !UNITY_POST_PROCESSING_STACK_V2
        // Initialize shader material early
        Shader shader = Shader.Find("Hidden/EditorPreviewColorCorrection");
        if (shader != null)
        {
            colorCorrectionMaterial = new Material(shader);
            Debug.Log("Color correction shader initialized successfully.");
        }
        else
        {
            Debug.LogWarning("Shader 'Hidden/EditorPreviewColorCorrection' not found. Color correction disabled.");
        }
#endif

        var root = rootVisualElement;

        // --- Preview container ---
        var previewContainer = new VisualElement();
        previewContainer.style.flexGrow = 1;
        previewContainer.style.position = Position.Relative;
        root.Add(previewContainer);

        previewElement = new VisualElement();
        previewElement.style.position = Position.Absolute;
        previewElement.style.top = 0;
        previewElement.style.left = 0;
        previewElement.style.right = 0;
        previewElement.style.bottom = 0;
        previewContainer.Add(previewElement);

        // --- Screenshot button ---
        screenshotButton = new Button(() => TakeScreenshot())
        {
            text = "Take Screenshot"
        };
        screenshotButton.style.height = 30;
        screenshotButton.style.marginTop = 4;
        root.Add(screenshotButton);

        // --- Settings Panel ---
        var settingsPanel = new VisualElement();
        settingsPanel.style.position = Position.Absolute;
        settingsPanel.style.top = 5;
        settingsPanel.style.right = 5;
        settingsPanel.style.width = 240;
        settingsPanel.style.paddingLeft = 10;
        settingsPanel.style.paddingRight = 10;
        settingsPanel.style.paddingTop = 5;
        settingsPanel.style.paddingBottom = 5;
        settingsPanel.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        settingsPanel.style.borderTopLeftRadius = 6;
        settingsPanel.style.borderBottomLeftRadius = 6;
        root.Add(settingsPanel);

        // --- FOV ---
        settingsPanel.Add(new Label("Field of View:")
        {
            style =
            {
                color = Color.white,
                unityTextAlign = TextAnchor.MiddleLeft,
                marginBottom = 2
            }
        });

        var fovContainer = new VisualElement();
        fovContainer.style.flexDirection = FlexDirection.Row;
        fovContainer.style.alignItems = Align.Center;
        settingsPanel.Add(fovContainer);

        fovSlider = new Slider(10f, 100f);
        fovSlider.value = cameraFOV;
        fovSlider.style.flexGrow = 1f;
        fovSlider.RegisterValueChangedCallback(evt =>
        {
            cameraFOV = evt.newValue;
            if (previewCamera != null)
                previewCamera.fieldOfView = cameraFOV;
            fovValueLabel.text = cameraFOV.ToString("F1");
            Repaint();
        });
        fovContainer.Add(fovSlider);

        fovValueLabel = new Label(cameraFOV.ToString("F1"));
        fovValueLabel.style.width = 40;
        fovValueLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        fovValueLabel.style.marginLeft = 6;
        fovValueLabel.style.color = Color.white;
        fovContainer.Add(fovValueLabel);

        // --- Aspect Ratio ---
        var aspectLabel = new Label("Aspect Ratio:");
        aspectLabel.style.color = Color.white;
        aspectLabel.style.marginTop = 8;
        settingsPanel.Add(aspectLabel);

        aspectRatioField = new EnumField(currentAspectRatio);
        aspectRatioField.style.width = Length.Percent(100);
        aspectRatioField.RegisterValueChangedCallback(evt =>
        {
            currentAspectRatio = (AspectRatio)evt.newValue;
            UpdateResolutionHeightAndRecreateRT();
            Repaint();
        });
        settingsPanel.Add(aspectRatioField);

        // --- Resolution Height ---
        var heightLabel = new Label("Picture Height (px):");
        heightLabel.style.color = Color.white;
        heightLabel.style.marginTop = 8;
        settingsPanel.Add(heightLabel);

        resolutionHeightField = new IntegerField();
        resolutionHeightField.value = pictureHeight;
        resolutionHeightField.style.width = Length.Percent(100);
        resolutionHeightField.isDelayed = true;
        resolutionHeightField.RegisterValueChangedCallback(evt =>
        {
            int h = Mathf.Clamp(evt.newValue, 16, 8192);
            if (h != pictureHeight)
            {
                pictureHeight = h;
                resolutionHeightField.SetValueWithoutNotify(pictureHeight);
                RecreateRenderTextureAndResize();
                Repaint();
            }
        });
        settingsPanel.Add(resolutionHeightField);

        // === Color Correction ===
        var ccLabel = new Label("Color Correction:");
        ccLabel.style.color = Color.white;
        ccLabel.style.marginTop = 10;
        settingsPanel.Add(ccLabel);

        exposureSlider = new Slider(-2f, 2f);
        exposureSlider.value = exposure;
        exposureSlider.label = "Exposure";
        exposureSlider.RegisterValueChangedCallback(evt =>
        {
            exposure = evt.newValue;
            UpdateColorCorrection();
        });
        settingsPanel.Add(exposureSlider);

        contrastSlider = new Slider(0.5f, 2f);
        contrastSlider.value = contrast;
        contrastSlider.label = "Contrast";
        contrastSlider.RegisterValueChangedCallback(evt =>
        {
            contrast = evt.newValue;
            UpdateColorCorrection();
        });
        settingsPanel.Add(contrastSlider);

        saturationSlider = new Slider(0f, 2f);
        saturationSlider.value = saturation;
        saturationSlider.label = "Saturation";
        saturationSlider.RegisterValueChangedCallback(evt =>
        {
            saturation = evt.newValue;
            UpdateColorCorrection();
        });
        settingsPanel.Add(saturationSlider);

        tintColorField = new ColorField("Tint");
        tintColorField.value = tintColor;
        tintColorField.RegisterValueChangedCallback(evt =>
        {
            tintColor = evt.newValue;
            UpdateColorCorrection();
        });
        settingsPanel.Add(tintColorField);

        UpdateColorCorrection();
        EditorApplication.update += UpdatePreview;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdatePreview;

#if UNITY_POST_PROCESSING_STACK_V2
        if (postProcessProfile != null)
        {
            DestroyImmediate(postProcessProfile);
            postProcessProfile = null;
        }
        if (postProcessVolume != null)
        {
            DestroyImmediate(postProcessVolume.gameObject);
            postProcessVolume = null;
        }
#endif

        DestroyPreviewCamera();
        DestroyRenderTexture();
    }

    private void CreatePreviewCamera()
    {
        if (previewCamera == null)
        {
            var go = new GameObject("EditorPreviewCamera");
            go.hideFlags = HideFlags.HideAndDontSave;
            previewCamera = go.AddComponent<Camera>();

            previewCamera.clearFlags = CameraClearFlags.Skybox;
            previewCamera.orthographic = false;
            previewCamera.nearClipPlane = 0.3f;
            previewCamera.farClipPlane = 100f;

            // ✅ Important fixes for alpha clipping:
            previewCamera.renderingPath = RenderingPath.Forward;
            previewCamera.forceIntoRenderTexture = true;
            previewCamera.allowHDR = true;
            previewCamera.transparencySortMode = TransparencySortMode.Orthographic;

            previewCamera.enabled = true;

#if UNITY_POST_PROCESSING_STACK_V2
            var ppLayer = go.GetComponent<PostProcessLayer>();
            if (ppLayer == null)
                ppLayer = go.AddComponent<PostProcessLayer>();

            ppLayer.volumeLayer = LayerMask.GetMask("Default");
            ppLayer.Init(null);

            var volumeGO = new GameObject("EditorPreviewPostProcessVolume");
            volumeGO.hideFlags = HideFlags.HideAndDontSave;
            volumeGO.transform.parent = go.transform;

            postProcessProfile = ScriptableObject.CreateInstance<PostProcessProfile>();
            postProcessVolume = volumeGO.AddComponent<PostProcessVolume>();
            postProcessVolume.isGlobal = true;
            postProcessVolume.sharedProfile = postProcessProfile;
#endif

            previewCamera.fieldOfView = cameraFOV;
        }
    }

    private void DestroyPreviewCamera()
    {
#if UNITY_POST_PROCESSING_STACK_V2
        if (postProcessProfile != null)
        {
            DestroyImmediate(postProcessProfile);
            postProcessProfile = null;
        }
        if (postProcessVolume != null)
        {
            DestroyImmediate(postProcessVolume.gameObject);
            postProcessVolume = null;
        }
#endif

        if (previewCamera != null)
        {
            DestroyImmediate(previewCamera.gameObject);
            previewCamera = null;
        }
    }

    private void CreateRenderTexture(int width, int height)
    {
        if (previewRenderTexture != null)
            DestroyRenderTexture();

        if (width > 0 && height > 0)
        {
            // ✅ Use ARGB32 for proper alpha and cutout rendering
            previewRenderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            previewRenderTexture.useMipMap = false;
            previewRenderTexture.autoGenerateMips = false;
            previewRenderTexture.Create();

            if (previewCamera != null)
                previewCamera.targetTexture = previewRenderTexture;
        }
    }

    private void DestroyRenderTexture()
    {
        if (previewRenderTexture != null)
        {
            if (previewCamera != null)
                previewCamera.targetTexture = null;

            previewRenderTexture.Release();
            DestroyImmediate(previewRenderTexture);
            previewRenderTexture = null;
        }
    }

    private void RecreateRenderTextureAndResize()
    {
        int width = CalculateWidthFromHeight(pictureHeight, currentAspectRatio);
        CreateRenderTexture(width, pictureHeight);
    }

    private int CalculateWidthFromHeight(int height, AspectRatio ratio)
    {
        switch (ratio)
        {
            case AspectRatio._16_9:
                return Mathf.RoundToInt(height * 16f / 9f);
            case AspectRatio._4_3:
                return Mathf.RoundToInt(height * 4f / 3f);
            default:
                return Mathf.RoundToInt(height * 16f / 9f);
        }
    }

    private void UpdateResolutionHeightAndRecreateRT()
    {
        resolutionHeightField.SetValueWithoutNotify(pictureHeight);
        RecreateRenderTextureAndResize();
    }

    private void UpdatePreview()
    {
        if (previewCamera == null || previewRenderTexture == null)
            return;

        SyncWithSceneViewCamera();
        previewCamera.Render();

#if !UNITY_POST_PROCESSING_STACK_V2
        if (colorCorrectionMaterial != null && previewRenderTexture != null)
        {
            RenderTexture tempRT = RenderTexture.GetTemporary(
                previewRenderTexture.width,
                previewRenderTexture.height,
                0,
                previewRenderTexture.format
            );

            Graphics.Blit(previewRenderTexture, tempRT, colorCorrectionMaterial);
            Graphics.Blit(tempRT, previewRenderTexture);
            RenderTexture.ReleaseTemporary(tempRT);
        }
#endif

        Texture2D tex = new Texture2D(previewRenderTexture.width, previewRenderTexture.height, TextureFormat.RGBA32, false);
        RenderTexture.active = previewRenderTexture;
        tex.ReadPixels(new Rect(0, 0, previewRenderTexture.width, previewRenderTexture.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        float windowWidth = position.width;
        float windowHeightAvailable = position.height - 40;

        float targetAspectRatio = currentAspectRatio == AspectRatio._4_3 ? 4f / 3f : 16f / 9f;
        float displayWidth, displayHeight;

        if (windowWidth / windowHeightAvailable > targetAspectRatio)
        {
            displayHeight = windowHeightAvailable;
            displayWidth = displayHeight * targetAspectRatio;
        }
        else
        {
            displayWidth = windowWidth;
            displayHeight = displayWidth / targetAspectRatio;
        }

        previewElement.style.width = displayWidth;
        previewElement.style.height = displayHeight;

        float marginLeftRight = (windowWidth - displayWidth) * 0.5f;
        float marginTopBottom = (windowHeightAvailable - displayHeight) * 0.5f;

        previewElement.style.marginLeft = marginLeftRight;
        previewElement.style.marginRight = marginLeftRight;
        previewElement.style.marginTop = marginTopBottom;
        previewElement.style.marginBottom = marginTopBottom;

        previewElement.style.backgroundImage = new StyleBackground(tex);
    }

    private void SyncWithSceneViewCamera()
    {
        var sceneCam = SceneView.lastActiveSceneView?.camera;
        if (sceneCam == null || previewCamera == null)
            return;

        previewCamera.transform.position = sceneCam.transform.position;
        previewCamera.transform.rotation = sceneCam.transform.rotation;

        if (Mathf.Abs(previewCamera.fieldOfView - cameraFOV) > 0.01f)
            previewCamera.fieldOfView = cameraFOV;
    }

    private void TakeScreenshot()
    {
        if (previewRenderTexture == null || previewCamera == null)
        {
            Debug.LogWarning("No camera or render texture available for screenshot.");
            return;
        }

        if (!Directory.Exists(ScreenshotFolder))
            Directory.CreateDirectory(ScreenshotFolder);

        previewCamera.Render();

#if !UNITY_POST_PROCESSING_STACK_V2
        if (colorCorrectionMaterial != null && previewRenderTexture != null)
        {
            RenderTexture tempRT = RenderTexture.GetTemporary(
                previewRenderTexture.width,
                previewRenderTexture.height,
                0,
                previewRenderTexture.format
            );

            Graphics.Blit(previewRenderTexture, tempRT, colorCorrectionMaterial);
            Graphics.Blit(tempRT, previewRenderTexture);
            RenderTexture.ReleaseTemporary(tempRT);
        }
#endif

        RenderTexture.active = previewRenderTexture;
        Texture2D texScreenshot = new Texture2D(previewRenderTexture.width, previewRenderTexture.height, TextureFormat.RGB24, false);
        texScreenshot.ReadPixels(new Rect(0, 0, previewRenderTexture.width, previewRenderTexture.height), 0, 0);
        texScreenshot.Apply();
        RenderTexture.active = null;

        byte[] bytes = texScreenshot.EncodeToPNG();
        DestroyImmediate(texScreenshot);

        string fileName = $"Screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string fullPath = Path.Combine(ScreenshotFolder, fileName);

        File.WriteAllBytes(fullPath, bytes);
        Debug.Log($"Screenshot saved to {fullPath}");
        AssetDatabase.Refresh();
    }

    private void CreateScreenshotDirectory()
    {
        if (!Directory.Exists(ScreenshotFolder))
        {
            Directory.CreateDirectory(ScreenshotFolder);
            AssetDatabase.Refresh();
        }
    }

    private void UpdateColorCorrection()
    {
#if UNITY_POST_PROCESSING_STACK_V2
        if (postProcessProfile != null)
        {
            if (!postProcessProfile.TryGetSettings(out ColorGrading colorGrading))
                colorGrading = postProcessProfile.AddSettings<ColorGrading>();

            colorGrading.enabled.Override(true);
            colorGrading.postExposure.Override(exposure);
            colorGrading.contrast.Override((contrast - 1f) * 100f);
            colorGrading.saturation.Override((saturation - 1f) * 100f);
            colorGrading.colorFilter.Override(tintColor);
        }
#else
        if (colorCorrectionMaterial == null) return;

        colorCorrectionMaterial.SetFloat("_Exposure", exposure);
        colorCorrectionMaterial.SetFloat("_Contrast", contrast);
        colorCorrectionMaterial.SetFloat("_Saturation", saturation);
        colorCorrectionMaterial.SetColor("_Tint", tintColor);
#endif
    }
}
