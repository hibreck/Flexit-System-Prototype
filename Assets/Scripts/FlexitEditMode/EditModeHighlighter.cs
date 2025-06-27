using UnityEngine;

public class EditModeHighlighter : MonoBehaviour
{
    private Material blockMaterial;
    private bool isHighlighting = false;
    private Color baseEmission = Color.black;

    public void Initialize()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            blockMaterial = renderer.material;
            baseEmission = blockMaterial.GetColor("_EmissionColor");
            blockMaterial.EnableKeyword("_EMISSION");
        }
    }

    public void StartHighlight()
    {
        if (!isHighlighting && blockMaterial != null)
            StartCoroutine(EmissionPulse());
    }

    private System.Collections.IEnumerator EmissionPulse()
    {
        isHighlighting = true;
        float time = 0f;

        while (true)
        {
            float intensity = 0.05f + Mathf.Sin(time * 2f) * 0.03f;
            Color pulseColor = Color.white * intensity;
            blockMaterial.SetColor("_EmissionColor", pulseColor);

            time += Time.deltaTime;
            yield return null;
        }
    }

    public void StopHighlight()
    {
        isHighlighting = false;
        StopAllCoroutines();

        if (blockMaterial != null)
        {
            blockMaterial.SetColor("_EmissionColor", baseEmission); // або Color.black
        }
    }
}