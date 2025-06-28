// FlexitPlacementUtility.cs
using UnityEngine;

public static class FlexitPlacementUtility
{
    public static bool ComputePlacement(Camera cam, GameObject prefab, float maxDistance, LayerMask surfaceMask,
out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (cam == null || prefab == null)
            return false;

        int playerLayer = LayerMask.NameToLayer("Player");
        LayerMask maskWithoutPlayer = surfaceMask & ~(1 << playerLayer);

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, maskWithoutPlayer))
            return false;

        Vector3 hitNormal = hit.normal;
        float verticalDot = Vector3.Dot(hitNormal, Vector3.up);

        if (Mathf.Abs(verticalDot) > 0.9f)
        {
            // ⬇️ Нова логіка для Flexit-блоків
            if (hit.collider.CompareTag("Flexit"))
            {
                // орієнтація буде такою ж, як у блоку під курсором
                rotation = Quaternion.Euler(0f, hit.collider.transform.eulerAngles.y, 0f);
            }
            else
            {
                Vector3 forward = cam.transform.forward;
                forward.y = 0f;
                forward.Normalize();
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;

                rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
        }
        else
        {
            rotation = Quaternion.LookRotation(-hitNormal, Vector3.up);
        }

        GameObject temp = Object.Instantiate(prefab);
        temp.transform.rotation = rotation;
        temp.transform.position = hit.point;

        Vector3 placePos = hit.point;
        Collider col = temp.GetComponent<Collider>();

        if (col != null)
        {
            Vector3 offset = CalculateOffset(col, hitNormal, rotation);
            placePos += offset;
        }

        Object.Destroy(temp);

        position = placePos;
        return true;
    }


    private static Vector3 CalculateOffset(Collider col, Vector3 hitNormal, Quaternion rotation)
    {
        Vector3 localNormal = Quaternion.Inverse(rotation) * hitNormal;
        Vector3 absLocalNormal = new Vector3(Mathf.Abs(localNormal.x), Mathf.Abs(localNormal.y), Mathf.Abs(localNormal.z));
        Vector3 extents = col.bounds.extents;

        Vector3 offsetLocal = Vector3.zero;

        if (absLocalNormal.x > absLocalNormal.y && absLocalNormal.x > absLocalNormal.z)
            offsetLocal.x = Mathf.Sign(localNormal.x) * extents.x;
        else if (absLocalNormal.y > absLocalNormal.x && absLocalNormal.y > absLocalNormal.z)
            offsetLocal.y = Mathf.Sign(localNormal.y) * extents.y;
        else
            offsetLocal.z = Mathf.Sign(localNormal.z) * extents.z;

        return rotation * offsetLocal;
    }

}
