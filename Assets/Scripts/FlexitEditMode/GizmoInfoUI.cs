using UnityEngine;
using TMPro;
using System.Collections;

public class GizmoInfoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI infoText;

    private string positionInfo = "";
    private string positionPlaneInfo = "";
    private string scaleInfo = "";
    private string scalePlaneInfo = "";
    private string rotationInfo = "";
    private string pivotPositionInfo = "";

    private Coroutine fadeCoroutine;
    private float visibleDuration = 1.5f;
    private float fadeDuration = 1f;

    private bool isInfoVisible = false;

    public enum GizmoInfoType
    {
        Move,
        Scale,
        ScalePlane,
        Rotate,
        Pivot,
        PlaneMove
    }

    // ------------------------- PUBLIC SET METHODS -------------------------

    public void SetPositionInfo(Vector3 pos)
    {
        ClearExcept(GizmoInfoType.Move);
        positionInfo = $"Pos <color=#FF7F7F>X: {pos.x:F2}</color>  <color=#7FFF7F>Y: {pos.y:F2}</color>  <color=#7FBFFF>Z: {pos.z:F2}</color>";
        UpdateInfoText();
        ShowTextWithFade();
    }

    public void SetPlanePositionInfo(Vector3 pos)
    {
        ClearExcept(GizmoInfoType.PlaneMove);
        positionPlaneInfo = $"Plane Pos <color=#FF7F7F>X: {pos.x:F2}</color>  <color=#7FFF7F>Y: {pos.y:F2}</color>  <color=#7FBFFF>Z: {pos.z:F2}</color>";
        UpdateInfoText();
        ShowTextWithFade();
    }

    public void SetScaleInfo(Vector3 scale)
    {
        ClearExcept(GizmoInfoType.Scale);
        scaleInfo = $"Scale <color=#FF7F7F>X: {scale.x:F2}</color>  <color=#7FFF7F>Y: {scale.y:F2}</color>  <color=#7FBFFF>Z: {scale.z:F2}</color>";
        UpdateInfoText();

        if (!isInfoVisible)
        {
            ShowTextWithFade();
            isInfoVisible = true;
        }
    }

    public void SetPlaneScaleInfo(Vector3 scale)
    {
        ClearExcept(GizmoInfoType.ScalePlane);
        scalePlaneInfo = $"Scale Plane <color=#FF7F7F>X: {scale.x:F2}</color>  <color=#7FFF7F>Y: {scale.y:F2}</color>  <color=#7FBFFF>Z: {scale.z:F2}</color>";
        UpdateInfoText();
        ShowTextWithFade();
    }

    public void SetRotateInfo(Vector3 rot)
    {
        ClearExcept(GizmoInfoType.Rotate);
        rotationInfo = $"Rot <color=#FF7F7F>X: {rot.x:F0}°</color>  <color=#7FFF7F>Y: {rot.y:F0}°</color>  <color=#7FBFFF>Z: {rot.z:F0}°</color>";
        UpdateInfoText();
        ShowTextWithFade();
    }

    public void SetPivotPositionInfo(Vector3 pos)
    {
        ClearExcept(GizmoInfoType.Pivot);
        pivotPositionInfo = $"<b>Pivot</b> <color=#FF7F7F>X: {pos.x:F2}</color>  <color=#7FFF7F>Y: {pos.y:F2}</color>  <color=#7FBFFF>Z: {pos.z:F2}</color>";
        UpdateInfoText();
        ShowTextWithFade();
    }

    // ------------------------- CLEAR METHODS -------------------------

    public void ClearAllInfo()
    {
        positionInfo = "";
        positionPlaneInfo = "";
        scaleInfo = "";
        scalePlaneInfo = "";
        rotationInfo = "";
        pivotPositionInfo = "";
        UpdateInfoText();
    }

    public void ClearExcept(GizmoInfoType type)
    {
        if (type != GizmoInfoType.Move && type != GizmoInfoType.PlaneMove)
        {
            positionInfo = "";
            positionPlaneInfo = "";
        }

        if (type != GizmoInfoType.Scale && type != GizmoInfoType.ScalePlane)
        {
            scaleInfo = "";
            scalePlaneInfo = "";

        }

        if (type != GizmoInfoType.Rotate)
        {
            rotationInfo = "";
        }

        if (type != GizmoInfoType.Pivot)
        {
            pivotPositionInfo = "";
        }

        UpdateInfoText();
    }

    // ------------------------- INTERNAL LOGIC -------------------------

    private void UpdateInfoText()
    {
        System.Text.StringBuilder sb = new();

        if (!string.IsNullOrEmpty(positionInfo)) sb.AppendLine(positionInfo);
        if (!string.IsNullOrEmpty(positionPlaneInfo)) sb.AppendLine(positionPlaneInfo);
        if (!string.IsNullOrEmpty(scaleInfo)) sb.AppendLine(scaleInfo);
        if (!string.IsNullOrEmpty(scalePlaneInfo)) sb.AppendLine(scalePlaneInfo);
        if (!string.IsNullOrEmpty(rotationInfo)) sb.AppendLine(rotationInfo);
        if (!string.IsNullOrEmpty(pivotPositionInfo)) sb.AppendLine(pivotPositionInfo);

        infoText.text = sb.ToString().TrimEnd();
    }

    private void ShowTextWithFade()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        infoText.alpha = 1f;
        fadeCoroutine = StartCoroutine(FadeOutAfterDelay());
    }

    private IEnumerator FadeOutAfterDelay()
    {
        yield return new WaitForSeconds(visibleDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            infoText.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        infoText.alpha = 0f;
        ClearAllInfo();

        isInfoVisible = false;  // Важливо: скидаємо прапорець
    }
}
