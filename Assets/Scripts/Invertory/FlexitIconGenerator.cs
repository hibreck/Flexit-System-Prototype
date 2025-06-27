#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class FlexitIconGenerator
{
    public static Sprite GenerateIcon(GameObject prefab, RenderTexture renderTex, Camera previewCam)
    {
        // Інстанціюємо тимчасовий об'єкт
        GameObject instance = GameObject.Instantiate(prefab);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;

        // Призначаємо тимчасовий шар
        int previewLayer = LayerMask.NameToLayer("IconPreview");
        if (previewLayer == -1)
        {
            Debug.LogError("Створи шар 'IconPreview' у Tags and Layers!");
            GameObject.DestroyImmediate(instance);
            return null;
        }

        // Створюємо тимчасове світло
        GameObject lightObj = new GameObject("TempLight");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = Color.white;
        light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        lightObj.layer = previewLayer;

        // Зберігаємо початковий шар об'єкта
        int originalLayer = instance.layer;

        // Встановлюємо всім об'єктам шару IconPreview
        instance.layer = previewLayer;
        foreach (Transform child in instance.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = previewLayer;
        }

        // Зберігаємо початкову маску камери
        int originalCullingMask = previewCam.cullingMask;

        // Камера рендерить лише IconPreview
        previewCam.cullingMask = 1 << previewLayer;

        // Розташування камери
        previewCam.transform.position = new Vector3(2, 2, -2);
        previewCam.transform.rotation = Quaternion.Euler(30, 45, 0);
        previewCam.transform.LookAt(instance.transform.position);
        previewCam.fieldOfView = 5.5f;

        // Підготовка до рендеру
        previewCam.targetTexture = renderTex;
        previewCam.enabled = true;
        previewCam.Render();
        previewCam.enabled = false;

        // Копіювання результату
        RenderTexture.active = renderTex;
        Texture2D tex = new Texture2D(renderTex.width, renderTex.height, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        tex.Apply();

        // Очищення
        RenderTexture.active = null;
        previewCam.targetTexture = null;
        previewCam.cullingMask = originalCullingMask;

        GameObject.DestroyImmediate(instance);
        GameObject.DestroyImmediate(lightObj);

        // Створення спрайта
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }
}
#endif
