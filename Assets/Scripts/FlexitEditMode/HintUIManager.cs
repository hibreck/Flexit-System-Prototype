using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class HintUIManager : MonoBehaviour
{
    public static HintUIManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI hintText;

    private Dictionary<string, string> activeHints = new Dictionary<string, string>();
    private Coroutine modeFadeCoroutine;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Duplicate HintUIManager destroyed on {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // опційно, якщо хочеш щоб жив між сценами
    }
    public static class Tips
    {
        public const string EditMode =
            "<b>ESC</b>: Exit edit mode\n" +
            "<b>ALT + Scroll</b>: Change gizmo mode\n" +
            "- <b>LMB</b>: Use Gizmo\n" +
            "- <b>Middle Click</b>: Teleport block to looked position";

        public const string Move =
            "<b>Tips:</b>\n" +
            "- <b>LMB / Scroll</b>: Move on one axis\n" +
            "- <b>Shift + LMB / Scroll</b>: Fine movement";

        public const string MovePivot =
            "<b>Tips:</b>\n" +
            "- <b>LMB</b>: Move on one axis\n" +
            "- <b>Shift + LMB</b>: Fine movement\n" +
            "- <b>Ctrl</b>: Clamp movement inside bounds\n" +
            "- <b>Ctrl + Middle Click</b>: Place pivot at surface\n" +
            "- <b>T</b>: Reset pivot position";


        public const string PlaneMove =
            "<b>Tips:</b>\n" +
            "- <b>LMB</b>: Move on plane\n" +
            "- <b>Shift + LMB</b>: Fine movement\n" +
            "- <b>R</b>: Snap to grid";

        public const string PlaneScale =
            "<b>Tips:</b>\n" +
            "- <b>LMB</b>: Scale on two axes\n" +
            "- <b>Ctrl + LMB</b>: Scale evenly on both axes\n" +
            "- <b>Shift + LMB</b>: Smooth scaling\n" +
            "- <b>R</b>: Snap to grid";

        public const string Rotate =
            "<b>Tips:</b>\n" +
            "- <b>LMB / Scroll</b>: Rotate snap\n" +
            "- <b>Shift + LMB / Scroll</b>: Smooth rotate\n" +
            "- <b>R</b>: Reset rotate";

        public const string Scale =
            "<b>Tips:</b>\n" +
            "- <b>LMB / Scroll</b>: Scale on one axis\n" +
            "- <b>Ctrl + LMB / Scroll</b>: Scale uniformly\n" +
            "- <b>Shift + LMB / Scroll</b>: Smooth scaling\n" +
            "- <b>R</b>: Round to pixel step";
    }

    

    public void ShowHint(string source, string text)
    {
        
        activeHints[source] = text;
        UpdateHintText();

        // Якщо це режим "Mode", запускаємо корутину фейду
        if (source == "Mode")
        {
            if (modeFadeCoroutine != null)
                StopCoroutine(modeFadeCoroutine);

            modeFadeCoroutine = StartCoroutine(FadeOutModeHint(3f, 1f)); // 3 сек показати, 1 сек плавний фейд
        }
    }

    public void ClearHint(string source)
    {
        if (activeHints.ContainsKey(source))
        {
            activeHints.Remove(source);
            UpdateHintText();
        }
    }

    public bool IsCurrentHint(string source)
    {
        return activeHints.ContainsKey(source);
    }

    private void UpdateHintText()
    {
        hintText.text = string.Join("\n\n", activeHints.Values);
        hintText.alpha = 1f; // Відновити непрозорість при оновленні тексту
    }
    private IEnumerator FadeOutModeHint(float delaySeconds, float fadeDuration)
    {
        yield return new WaitForSeconds(delaySeconds);

        float elapsed = 0f;
        float startAlpha = hintText.alpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            hintText.alpha = alpha;
            yield return null;
        }

        hintText.alpha = 0f;

        ClearHint("Mode");

        modeFadeCoroutine = null;
    }
}
