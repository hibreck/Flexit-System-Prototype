#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class IconGeneratorWindow : EditorWindow
{
    GameObject prefab;
    Camera previewCamera;
    RenderTexture renderTexture;

    [MenuItem("Tools/Flexit/Generate Icon")]
    static void Init()
    {
        GetWindow<IconGeneratorWindow>("Flexit Icon Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("⚙️ Flexit Icon Generator", EditorStyles.boldLabel);

        prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);

        if (previewCamera == null)
            previewCamera = FindPreviewCamera();

        if (renderTexture == null)
            renderTexture = FindRenderTexture();

        EditorGUILayout.ObjectField("Preview Camera (auto)", previewCamera, typeof(Camera), true);
        EditorGUILayout.ObjectField("Render Texture (auto)", renderTexture, typeof(RenderTexture), false);

        if (GUILayout.Button("Generate Icon"))
        {
            if (prefab && previewCamera && renderTexture)
            {
                Sprite icon = FlexitIconGenerator.GenerateIcon(prefab, renderTexture, previewCamera);
                SaveSpriteAsPNG(icon.texture, prefab.name);
            }
            else
            {
                Debug.LogError("❌ Не всі поля заповнені або не знайдено камеру/рендертекстуру.");
            }
        }
    }

    Camera FindPreviewCamera()
    {
        // Спроба знайти по імені
        GameObject camObj = GameObject.Find("FlexitPreviewCamera");
        if (camObj != null && camObj.TryGetComponent(out Camera foundCam))
            return foundCam;

        // Новий метод для пошуку всіх камер
        Camera[] allCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (var cam in allCams)
        {
            if (cam.CompareTag("EditorOnly"))
                return cam;
        }

        return null;
    }


    RenderTexture FindRenderTexture()
    {
        return AssetDatabase.LoadAssetAtPath<RenderTexture>("Assets/Resources/Textures/FlexitIcons/DefaultPreviewRT.renderTexture");
    }

    void SaveSpriteAsPNG(Texture texture, string name)
    {
        Texture2D tex2D = (Texture2D)texture;
        byte[] bytes = tex2D.EncodeToPNG();
        string path = $"Assets/Resources/Textures/FlexitIcons/{name}_icon.png";
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();
        Debug.Log($"✅ Збережено: {path}");
    }
}
#endif
