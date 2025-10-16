using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public class TextureProcessorWindow : EditorWindow
{
 private ObjectField metallicField;
    private ObjectField roughnessField;
    private Toggle overrideMetallicToggle;
    private ColorField metallicColorField;

    [MenuItem("Tools/Texture Processor")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<TextureProcessorWindow>();
        wnd.titleContent = new GUIContent("Texture Processor");
    }

    public void CreateGUI()
    {
        // Root container
        var root = rootVisualElement;
        root.style.paddingLeft = 10;
        root.style.paddingRight = 10;
        root.style.paddingTop = 10;
        root.style.paddingBottom = 10;
        root.style.flexDirection = FlexDirection.Column;

        // --- Metallic Texture Field ---
        metallicField = new ObjectField("Metallic Texture")
        {
            objectType = typeof(Texture2D),
            allowSceneObjects = false
        };
        metallicField.style.width = new Length(100, LengthUnit.Percent);
        root.Add(metallicField);

        // --- Override Metallic Checkbox ---
        overrideMetallicToggle = new Toggle("Override Metallic");
        overrideMetallicToggle.RegisterValueChangedCallback(evt =>
        {
            bool enabled = evt.newValue;

            metallicField.SetEnabled(!enabled);
            metallicColorField.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
        });
        root.Add(overrideMetallicToggle);

        // --- Metallic Color Field (hidden by default) ---
        metallicColorField = new ColorField("Metallic Value")
        {
            value = Color.gray
        };
        metallicColorField.style.display = DisplayStyle.None;
        metallicColorField.RegisterValueChangedCallback(evt =>
        {
            // Force grayscale: lock R=G=B
            float g = evt.newValue.grayscale;
            metallicColorField.value = new Color(g, g, g, 1f);
        });
        root.Add(metallicColorField);

        // --- Roughness Texture Field ---
        roughnessField = new ObjectField("Roughness Texture")
        {
            objectType = typeof(Texture2D),
            allowSceneObjects = false
        };
        roughnessField.style.width = new Length(100, LengthUnit.Percent);
        root.Add(roughnessField);

        // --- Generate Button ---
        var generateButton = new Button(() =>
        {
            var roughnessTex = roughnessField.value as Texture2D;

            if (roughnessTex == null)
            {
                Debug.LogError("Please assign a Roughness texture.");
                return;
            }

            if (overrideMetallicToggle.value)
            {
                float metallicValue = metallicColorField.value.grayscale;
                Process(metallicValue, roughnessTex);
            }
            else
            {
                var metallicTex = metallicField.value as Texture2D;
                if (metallicTex == null)
                {
                    Debug.LogError("Please assign a Metallic texture or enable Override Metallic.");
                    return;
                }

                Process(metallicTex, roughnessTex);
            }
        })
        {
            text = "Generate"
        };

        generateButton.style.marginTop = 10;
        root.Add(generateButton);
    }

    private void Process(Texture2D MetallicTex, Texture2D RoughnessTex)
    {
        
        var MetallicImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(MetallicTex)) as TextureImporter;
        var RoughnessImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(RoughnessTex)) as TextureImporter;

        if (MetallicImporter == null || RoughnessImporter == null)
        {
            return;
        }

        if(!MetallicImporter.isReadable){
            MetallicImporter.isReadable = true;
            MetallicImporter.SaveAndReimport();
        }

        if(!RoughnessImporter.isReadable){
            RoughnessImporter.isReadable = true;
            RoughnessImporter.SaveAndReimport();
        }
        
        var roughnessPixels = RoughnessTex.GetPixels();
        var metallicPixels = MetallicTex.GetPixels();
        
        int total = roughnessPixels.Length;
        var pixels = new Color[total];

        for (int i = 0; i < total; i++)
        {
            var rGray = roughnessPixels[i].r * 0.299f + roughnessPixels[i].g * 0.587f + roughnessPixels[i].b * 0.114f;
            var smoothness = 1f - rGray;
            
            var mGray = metallicPixels[i].r * 0.299f + metallicPixels[i].g * 0.587f + metallicPixels[i].b * 0.114f;
            
            pixels[i] = new Color(mGray, 0f, 0f, smoothness);
        }
        
        var tex = new Texture2D(MetallicTex.width, MetallicTex.height, TextureFormat.ARGB32, false);
        tex.SetPixels(pixels);
        
        AssetDatabase.CreateAsset(tex, $"{AssetDatabase.GetAssetPath(MetallicTex).Split(".")[0]}_converted.asset");
        AssetDatabase.SaveAssets();
    }

    private void Process(float MetallicValue, Texture2D RoughnessTex)
    {
        var RoughnessImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(RoughnessTex)) as TextureImporter;

        if(!RoughnessImporter.isReadable){
            RoughnessImporter.isReadable = true;
            RoughnessImporter.SaveAndReimport();
        }
        
        var roughnessPixels = RoughnessTex.GetPixels();
        
        int total = roughnessPixels.Length;
        var pixels = new Color[total];

        for (int i = 0; i < total; i++)
        {
            var rGray = roughnessPixels[i].r * 0.299f + roughnessPixels[i].g * 0.587f + roughnessPixels[i].b * 0.114f;
            var smoothness = 1f - rGray;
            
            
            pixels[i] = new Color(MetallicValue, 0f, 0f, smoothness);
        }
        
        var tex = new Texture2D(RoughnessTex.width, RoughnessTex.height, TextureFormat.ARGB32, false);
        tex.SetPixels(pixels);
        
        AssetDatabase.CreateAsset(tex, $"{AssetDatabase.GetAssetPath(RoughnessTex).Split(".")[0]}_converted.asset");
        AssetDatabase.SaveAssets();
    }
}