using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class FlexitAutoUV : MonoBehaviour
{
    [SerializeField] private bool xAxis = true;
    [SerializeField] private bool yAxis = true;
    [SerializeField] private bool zAxis = true;

    private Vector2[] baseUVs;     // UV з оригінального sharedMesh (незмінні)
    private Mesh mesh;             // Інстансований mesh

    private Vector3 lastScale;

    // Початкові параметри — підтягуються при Awake
    private Vector3 initialScale;
    private float initialPixelCount = 13f; // початковий pixel count для розміру initialScale (приклад)

    private const float minPixelCount = 3f;   // мінімальні пікселі (при дуже малому масштабі)
    // Верхній ліміт видалено, щоб текстура повторювалась безкінечно

    private void Awake()
    {
        if (!Application.isPlaying) return;

        MeshFilter mf = GetComponent<MeshFilter>();

        // Беремо UV без змін із sharedMesh і зберігаємо у baseUVs
        if (mf != null && mf.sharedMesh != null)
        {
            baseUVs = (Vector2[])mf.sharedMesh.uv.Clone();
        }

        EnsureUniqueMesh();

        mesh = mf.mesh;

        // Запам'ятовуємо початковий масштаб як базу для розрахунків
        initialScale = transform.localScale;

        lastScale = initialScale;
        AutoUV();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        if (mesh == null || baseUVs == null) return;

        if (transform.localScale != lastScale)
        {
            lastScale = transform.localScale;
            AutoUV();
        }
    }

    private void EnsureUniqueMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null && !mf.sharedMesh.name.Contains("(Instance)"))
        {
            mf.mesh = Instantiate(mf.sharedMesh);
            mf.mesh.name += " (Instance)";
        }
    }

    private float CalcUVScale(float currentSize, float axisInitialSize)
    {
        float ratio = currentSize / axisInitialSize;
        float pixels = Mathf.Max(initialPixelCount * ratio, minPixelCount);
        return pixels / initialPixelCount; // коефіцієнт масштабування UV (може бути >1 без ліміту)
    }

    private void AutoUV()
    {
        if (mesh == null || baseUVs == null) return;

        Vector3 scale = transform.localScale;

        float uvScaleX = xAxis ? CalcUVScale(scale.x, initialScale.x) : 1f;
        float uvScaleY = yAxis ? CalcUVScale(scale.y, initialScale.y) : 1f;
        float uvScaleZ = zAxis ? CalcUVScale(scale.z, initialScale.z) : 1f;

        Vector2[] uvMap = new Vector2[baseUVs.Length];

        for (int i = 0; i < uvMap.Length; i++)
        {
            Vector2 uv = baseUVs[i];

            if ((i >= 0 && i <= 3) || (i == 6 || i == 7 || i == 10 || i == 11))
            {
                // Front & Back (X, Y)
                uvMap[i] = new Vector2(uv.x * uvScaleX, uv.y * uvScaleY);
            }
            else if ((i >= 4 && i <= 5) || (i >= 8 && i <= 9) || (i >= 12 && i <= 15))
            {
                // Top & Bottom (X, Z)
                uvMap[i] = new Vector2(uv.x * uvScaleX, uv.y * uvScaleZ);
            }
            else if (i >= 16 && i <= 23)
            {
                // Left & Right (Z, Y)
                uvMap[i] = new Vector2(uv.x * uvScaleZ, uv.y * uvScaleY);
            }
            else
            {
                uvMap[i] = uv;
            }
        }

        mesh.uv = uvMap;
    }
}
