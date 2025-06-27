using UnityEngine;
using System.Collections.Generic;
//3
public class OutlineManager : MonoBehaviour
{
    private struct OutlineEntry
    {
        public Renderer renderer;
        public Color color;
    }

    private List<OutlineEntry> outlines = new List<OutlineEntry>();

    private Material lineMaterial;

    private void Awake()
    {
        var shader = Shader.Find("Unlit/Color");
        lineMaterial = new Material(shader);
    }

    // Перевірка чи об'єкт або будь-який його батько має ім'я Cube_GizmoHandlesRoot
    private bool IsUnderGizmoRoot(GameObject go)
    {
        Transform current = go.transform;
        while (current != null)
        {
            if (current.name == "43245")
                return true;
            current = current.parent;
        }
        return false;
    }


    public void AddOutline(Renderer renderer, Color color)
    {
        if (renderer == null) return;

        if (IsUnderGizmoRoot(renderer.gameObject))
            return; // Ігноруємо всі рендерери під гізмо

        if (!HasTagInHierarchy(renderer.gameObject, "Flexit"))
            return; // Малюємо лише якщо в ієрархії є тег Fltxit

        if (!outlines.Exists(e => e.renderer == renderer))
        {
            outlines.Add(new OutlineEntry { renderer = renderer, color = color });
        }
    }
    private bool HasTagInHierarchy(GameObject go, string tag)
    {
        Transform current = go.transform;
        while (current != null)
        {
            if (current.gameObject.CompareTag(tag))
                return true;
            current = current.parent;
        }
        return false;
    }

    public void RemoveOutline(Renderer renderer)
    {
        outlines.RemoveAll(e => e.renderer == renderer);
    }

    private void OnRenderObject()
    {
        foreach (var entry in outlines)
        {
            if (entry.renderer == null) continue;

            if (IsUnderGizmoRoot(entry.renderer.gameObject))
                continue; // Ігноруємо при рендері

            DrawOutline(entry.renderer, entry.color);
        }
    }

    private void DrawOutline(Renderer renderer, Color color)
    {
        if (renderer == null) return;

        Vector3[] corners = GetBoundingBoxCorners(renderer);

        GL.PushMatrix();

        lineMaterial.color = color;
        lineMaterial.SetPass(0);

        GL.Begin(GL.LINES);
        GL.Color(color);

        DrawEdge(corners[0], corners[1]);
        DrawEdge(corners[1], corners[2]);
        DrawEdge(corners[2], corners[3]);
        DrawEdge(corners[3], corners[0]);

        DrawEdge(corners[4], corners[5]);
        DrawEdge(corners[5], corners[6]);
        DrawEdge(corners[6], corners[7]);
        DrawEdge(corners[7], corners[4]);

        DrawEdge(corners[0], corners[4]);
        DrawEdge(corners[1], corners[5]);
        DrawEdge(corners[2], corners[6]);
        DrawEdge(corners[3], corners[7]);

        GL.End();
        GL.PopMatrix();
    }

    private void DrawEdge(Vector3 from, Vector3 to)
    {
        GL.Vertex(from);
        GL.Vertex(to);
    }

    private static Vector3[] GetBoundingBoxCorners(Renderer renderer)
    {
        var bounds = renderer.localBounds;
        Vector3 half = bounds.size / 2;

        Vector3[] corners = new Vector3[8];
        Vector3 c = bounds.center;

        corners[0] = new Vector3(c.x - half.x, c.y - half.y, c.z - half.z);
        corners[1] = new Vector3(c.x + half.x, c.y - half.y, c.z - half.z);
        corners[2] = new Vector3(c.x + half.x, c.y - half.y, c.z + half.z);
        corners[3] = new Vector3(c.x - half.x, c.y - half.y, c.z + half.z);

        corners[4] = new Vector3(c.x - half.x, c.y + half.y, c.z - half.z);
        corners[5] = new Vector3(c.x + half.x, c.y + half.y, c.z - half.z);
        corners[6] = new Vector3(c.x + half.x, c.y + half.y, c.z + half.z);
        corners[7] = new Vector3(c.x - half.x, c.y + half.y, c.z + half.z);

        for (int i = 0; i < 8; i++)
        {
            corners[i] = renderer.transform.localToWorldMatrix.MultiplyPoint3x4(corners[i]);
        }

        return corners;
    }
}
