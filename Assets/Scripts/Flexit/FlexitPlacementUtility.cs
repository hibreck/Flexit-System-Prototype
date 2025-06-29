// FlexitPlacementUtility.cs
using UnityEngine;

public static class FlexitPlacementUtility
{
    public static bool ComputePlacement(Camera cam, GameObject prefab, float maxDistance, LayerMask surfaceMask,
    out Vector3 position, out Quaternion rotation, bool ignoreSnap = false)

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

        if (!ignoreSnap && hit.collider.CompareTag("Flexit"))
        {
            BoxCollider targetBoxCol = hit.collider as BoxCollider;
            BoxCollider tempBoxCol = temp.GetComponent<BoxCollider>();

            if (targetBoxCol != null && tempBoxCol != null)
            {
                placePos = SnapToFaceEdgeIfClose(targetBoxCol, placePos, hitNormal, 0.1f);
                Vector3 planeOffset = CalculateEdgePlaneShiftOffset(tempBoxCol, placePos, hitNormal);
                placePos += planeOffset;
            }
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
    private static Vector3 CalculateEdgePlaneShiftOffset(BoxCollider col, Vector3 hitPointWorld, Vector3 hitNormalWorld, float minThreshold = 0f, float maxThreshold = 0.4f)
    {
        Vector3 localHit = col.transform.InverseTransformPoint(hitPointWorld);
        Vector3 localNormal = col.transform.InverseTransformDirection(hitNormalWorld).normalized;

        Vector3 halfSize = col.size * 0.5f;
        Vector3 center = col.center;

        int normalAxis = 0;
        Vector3 absNormal = new Vector3(Mathf.Abs(localNormal.x), Mathf.Abs(localNormal.y), Mathf.Abs(localNormal.z));
        if (absNormal.y > absNormal.x && absNormal.y > absNormal.z) normalAxis = 1;
        else if (absNormal.z > absNormal.x && absNormal.z > absNormal.y) normalAxis = 2;

        int axisA = (normalAxis + 1) % 3;
        int axisB = (normalAxis + 2) % 3;

        float minA = center[axisA] - halfSize[axisA];
        float maxA = center[axisA] + halfSize[axisA];
        float minB = center[axisB] - halfSize[axisB];
        float maxB = center[axisB] + halfSize[axisB];

        float distToMinA = Mathf.Abs(localHit[axisA] - minA);
        float distToMaxA = Mathf.Abs(localHit[axisA] - maxA);
        float distToMinB = Mathf.Abs(localHit[axisB] - minB);
        float distToMaxB = Mathf.Abs(localHit[axisB] - maxB);

        float minDist = Mathf.Min(distToMinA, distToMaxA, distToMinB, distToMaxB);

        // Перевірка на діапазон мінімальної відстані
        if (minDist < minThreshold || minDist > maxThreshold)
        {
            // Зсув не виконуємо, бо поза діапазоном
            return Vector3.zero;
        }

        int shiftAxis = -1;
        float direction = 0;

        if (minDist == distToMinA) { shiftAxis = axisA; direction = 1; }
        else if (minDist == distToMaxA) { shiftAxis = axisA; direction = -1; }
        else if (minDist == distToMinB) { shiftAxis = axisB; direction = 1; }
        else if (minDist == distToMaxB) { shiftAxis = axisB; direction = -1; }

        float shiftAmount = col.size[shiftAxis] * 0.1f;
        Vector3 offsetLocal = Vector3.zero;
        offsetLocal[shiftAxis] = direction * shiftAmount;

        Vector3 offsetWorld = col.transform.TransformDirection(offsetLocal);
        return offsetWorld;
    }







    private static Vector3 SnapToFaceEdgeIfClose(BoxCollider col, Vector3 hitPointWorld, Vector3 hitNormalWorld, float snapThresholdWorld = 0.05f)
    {
        // Конвертуємо світовий поріг у локальний, враховуючи масштаб по всіх осях
        Vector3 scale = col.transform.lossyScale;
        // Щоб вибрати найбільш консервативний поріг по осях площини фейса:
        // Визначимо домінантну вісь нормалі (ось, по якій перпендикулярна площина)
        Vector3 localHitNormal = col.transform.InverseTransformDirection(hitNormalWorld);
        Vector3 absLocalNormal = new Vector3(Mathf.Abs(localHitNormal.x), Mathf.Abs(localHitNormal.y), Mathf.Abs(localHitNormal.z));
        int dominantAxis = 0;
        if (absLocalNormal.y > absLocalNormal.x && absLocalNormal.y > absLocalNormal.z) dominantAxis = 1;
        else if (absLocalNormal.z > absLocalNormal.x && absLocalNormal.z > absLocalNormal.y) dominantAxis = 2;

        int axisA = (dominantAxis + 1) % 3;
        int axisB = (dominantAxis + 2) % 3;

        // Розрахунок локального порогу з урахуванням масштабів по осях площини
        float localSnapThresholdA = snapThresholdWorld / scale[axisA];
        float localSnapThresholdB = snapThresholdWorld / scale[axisB];

        // Тепер продовжуємо з рештою логіки, використовуючи localSnapThresholdA та localSnapThresholdB

        Vector3 localHitPoint = col.transform.InverseTransformPoint(hitPointWorld);

        Vector3 colCenter = col.center;
        Vector3 colSize = col.size;

        Vector3 min = colCenter - colSize * 0.5f;
        Vector3 max = colCenter + colSize * 0.5f;

        float hitA = localHitPoint[axisA];
        float hitB = localHitPoint[axisB];

        float minA = min[axisA];
        float maxA = max[axisA];
        float minB = min[axisB];
        float maxB = max[axisB];

        if (hitA - minA < localSnapThresholdA)
            hitA = minA;
        else if (maxA - hitA < localSnapThresholdA)
            hitA = maxA;

        if (hitB - minB < localSnapThresholdB)
            hitB = minB;
        else if (maxB - hitB < localSnapThresholdB)
            hitB = maxB;

        Vector3 snappedLocalPos = localHitPoint;
        snappedLocalPos[axisA] = hitA;
        snappedLocalPos[axisB] = hitB;

        Vector3 snappedWorldPos = col.transform.TransformPoint(snappedLocalPos);

        return snappedWorldPos;
    }

    private static Vector3 AxisToVector(int axis)
{
    switch (axis)
    {
        case 0: return Vector3.right;   // X
        case 1: return Vector3.up;      // Y
        case 2: return Vector3.forward; // Z
        default: return Vector3.zero;
    }
}

}
