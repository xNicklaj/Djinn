using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif
using System.IO;

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

        var root = rootVisualElement;

        // Preview container with flex-grow 1 for main area
        var previewContainer = new VisualElement();
        previewContainer.style.flexGrow = 1;
        previewContainer.style.position = Position.Relative;
        root.Add(previewContainer);

        // The preview element (will show camera feed)
        previewElement = new VisualElement();
        previewElement.style.position = Position.Absolute;
        previewElement.style.top = 0;
        previewElement.style.left = 0;
        previewElement.style.right = 0;
        previewElement.style.bottom = 0;
        previewContainer.Add(previewElement);

        // Screenshot button at bottom
        screenshotButton = new Button(() => TakeScreenshot())
        {
            text = "Take Screenshot"
        };
        screenshotButton.style.height = 30;
        screenshotButton.style.marginTop = 4;
        root.Add(screenshotButton);

        // Floating settings panel top-right
        var settingsPanel = new VisualElement();
        settingsPanel.style.position = Position.Absolute;
        settingsPanel.style.top = 5;
        settingsPanel.style.right = 5;
        settingsPanel.style.width = 220;
        settingsPanel.style.paddingLeft = 10;
        settingsPanel.style.paddingRight = 10;
        settingsPanel.style.paddingTop = 5;
        settingsPanel.style.paddingBottom = 5;
        settingsPanel.style.backgroundColor = new Color(0, 0, 0, 0.5f);
        settingsPanel.style.borderTopLeftRadius = 6;
        settingsPanel.style.borderBottomLeftRadius = 6;

        root.Add(settingsPanel);

        // FOV label and slider container (horizontal)
        var fovContainer = new VisualElement();
        fovContainer.style.flexDirection = FlexDirection.Row;
        fovContainer.style.alignItems = Align.Center;
        settingsPanel.Add(new Label("Field of View:") { style = { color = Color.white, unityTextAlign = TextAnchor.MiddleLeft, marginBottom = 2 } });
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

        // Aspect ratio selection
        var aspectLabel = new Label("Aspect Ratio:");
        aspectLabel.style.color = Color.white;
        aspectLabel.style.marginTop = 8;
        settingsPanel.Add(aspectLabel);

        aspectRatioField = new EnumField(currentAspectRatio);
        aspectRatioField.style.width = Length.Percent(100);
        aspectRatioField.RegisterValueChangedCallback(evt =>
        {
            currentAspectRatio = (AspectRatio)evt.newValue;

            // Update height field and recreate render texture
            UpdateResolutionHeightAndRecreateRT();
            Repaint();
        });
        settingsPanel.Add(aspectRatioField);

        // Height field label
        var heightLabel = new Label("Picture Height (px):");
        heightLabel.style.color = Color.white;
        heightLabel.style.marginTop = 8;
        settingsPanel.Add(heightLabel);

        resolutionHeightField = new IntegerField();
        resolutionHeightField.value = pictureHeight;
        resolutionHeightField.style.width = Length.Percent(100);
        
        // Remove any input filter to allow normal editing
        resolutionHeightField.isDelayed = true; // Use delayed to commit only after editing (avoids partial invalid input)
        
        resolutionHeightField.RegisterValueChangedCallback(evt =>
        {
            int h = Mathf.Clamp(evt.newValue, 16, 8192); // Clamp reasonable min/max height

            if (h != pictureHeight)
            {
                pictureHeight = h;
                resolutionHeightField.SetValueWithoutNotify(pictureHeight);
                RecreateRenderTextureAndResize();
                Repaint();
            }
        });
        
        settingsPanel.Add(resolutionHeightField);

#if UNITY_POST_PROCESSING_STACK_V2
        if (previewCamera != null)
            previewCamera.fieldOfView = cameraFOV;
#endif

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

		    previewCamera.clearFlags = CameraClearFlags.Skybox; // <--- change here!
		    // Optional: remove backgroundColor since not used with skybox clear
		    // previewCamera.backgroundColor = Color.gray;

		    previewCamera.orthographic = false;
		    previewCamera.nearClipPlane = 0.3f;
		    previewCamera.farClipPlane = 100f;
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

        var bloom = postProcessProfile.AddSettings<UnityEngine.Rendering.PostProcessing.Bloom>();
        bloom.enabled.Override(true);

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
            previewRenderTexture = new RenderTexture(width, height, 24);
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

		Texture2D tex = new Texture2D(previewRenderTexture.width, previewRenderTexture.height, TextureFormat.RGBA32, false);

		RenderTexture.active = previewRenderTexture;

#if UNITY_2021_2_OR_NEWER
		tex.ReadPixels(new Rect(0,0,previewRenderTexture.width,previewRenderTexture.height),0,0,false);
#else
		tex.ReadPixels(new Rect(0,0,previewRenderTexture.width,previewRenderTexture.height),0,0);
#endif

		tex.Apply();
		RenderTexture.active = null;

		float windowWidth = position.width;
		float windowHeightAvailable = position.height - 40; 

		float targetAspectRatio;

		switch(currentAspectRatio)
		{
			case AspectRatio._16_9:
				targetAspectRatio = 16f / 9f; break;
			case AspectRatio._4_3:
				targetAspectRatio = 4f / 3f; break;
			default:
				targetAspectRatio = 16f / 9f; break;
		}

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

		var bgImage = new StyleBackground(tex);
		previewElement.style.backgroundImage = bgImage;

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

#if UNITY_POST_PROCESSING_STACK_V2
#endif
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

		RenderTexture.active = previewRenderTexture;

		Texture2D texScreenshot =
			new Texture2D(previewRenderTexture.width, previewRenderTexture.height, TextureFormat.RGB24, false);

#if UNITY_2021_2_OR_NEWER
		 texScreenshot.ReadPixels(new Rect(0,0,previewRenderTexture.width,previewRenderTexture.height),0,0,false);
#else
		 texScreenshot.ReadPixels(new Rect(0,0,previewRenderTexture.width,previewRenderTexture.height),0,0);
#endif

		texScreenshot.Apply();

		RenderTexture.active = null;

		byte[] bytes = texScreenshot.EncodeToPNG();
		Object.DestroyImmediate(texScreenshot);

		string fileName =
		 $"Screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";

		string fullPath =
		 Path.Combine(ScreenshotFolder, fileName);

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
}