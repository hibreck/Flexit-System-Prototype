using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    public InventorySlot[] slots = new InventorySlot[3];
    public Image[] slotImages; // Призначте UI Image для кожного слота в інспекторі
    public TMPro.TextMeshProUGUI[] slotCounts;
    public RectTransform[] slotRects; // Призначте RectTransform контейнерів слотів
    public RectTransform activeSlotBorder; // Призначте RectTransform рамки активного слота
    public int activeSlot = 0;
    public float pickupDistance = 2f;
    public LayerMask lootLayer;
    public Health playerHealth;

    [Header("Item Text")]
    public TMPro.TextMeshProUGUI itemNameText;
    [SerializeField] private float visibleDuration = 1.5f;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Item Stats Settings")]
    public Image healImage;
    public Image foodImage;
    public Image waterImage;
    public Image staminaImage;
    public Sprite defaultIcon;
    public TMPro.TextMeshProUGUI healText;
    public TMPro.TextMeshProUGUI foodText;
    public TMPro.TextMeshProUGUI waterText;
    public TMPro.TextMeshProUGUI staminaText;

    [Header("Weapon Stats Settings")]
    public Image damageImage;
    public Image cooldownImage;
    public Image knockbackImage;
    public TMPro.TextMeshProUGUI damageText;
    public TMPro.TextMeshProUGUI cooldownText;
    public TMPro.TextMeshProUGUI knockbackText;

    private InputSystem_Actions controls;
    private InputAction scrollSlotAction;
    private InputAction useActiveSlotAction;

    private Coroutine nameDisplayCoroutine;

    private void Awake()
    {
        controls = new InputSystem_Actions();
        controls.Enable();
        controls.Player.InvertoryUse.performed += ctx => UseActiveSlot();
        controls.Player.SwitchSlot.performed += ctx => SetActiveSlot((int)ctx.ReadValue<float>());
        scrollSlotAction = controls.Player.ScrollSlot;
        useActiveSlotAction = controls.Player.UseActiveSlot;

        scrollSlotAction.performed += OnScrollSlot;
        useActiveSlotAction.performed += ctx => UseActiveSlot();
    }

    private void OnDestroy()
    {
        scrollSlotAction.performed -= OnScrollSlot;
        useActiveSlotAction.performed -= ctx => UseActiveSlot();
        controls.Dispose();
    }

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<Health>();
        UpdateUI();
    }

    private void Update()
    {
        // Порожньо, можна додати логіку при потребі
    }

    private IEnumerator FadeItemNameText(string name)
    {
        itemNameText.text = name;
        itemNameText.alpha = 1f;
        itemNameText.gameObject.SetActive(true);

        yield return new WaitForSeconds(visibleDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            itemNameText.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        itemNameText.alpha = 0f;
        itemNameText.text = "";
        itemNameText.gameObject.SetActive(false);
    }

    public void OnScrollSlot(InputAction.CallbackContext ctx)
    {
        float scroll = ctx.ReadValue<float>();
        if (scroll > 0f)
            SetActiveSlot((activeSlot - 1 + slots.Length) % slots.Length);
        else if (scroll < 0f)
            SetActiveSlot((activeSlot + 1) % slots.Length);
    }

    public void AddItem(Item item)
    {
        // Додаємо у перший вільний слот або збільшуємо кількість
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item == null || slots[i].item == item)
            {
                if (slots[i].item == null)
                {
                    slots[i].item = item;
                    slots[i].count = 1;
                }
                else
                {
                    slots[i].count++;
                }
                UpdateUI();
                return;
            }
        }
        // Якщо всі слоти зайняті, можна додати логіку для переповнення
    }

    void SetActiveSlot(int index)
    {
        activeSlot = index;
        UpdateUI();
    }

    void UseActiveSlot()
    {
        Debug.Log("UseActiveSlot called");
        var slot = slots[activeSlot];
        if (slot.item != null && slot.count > 0)
        {
            Debug.Log($"Using item: {slot.item.name} ({slot.item.GetType().Name})");
            slot.item.Use(gameObject);
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            if (slots[i].item != null)
            {
                slotImages[i].sprite = slots[i].item.icon;
                slotImages[i].color = Color.white;
            }
            else
            {
                slotImages[i].sprite = null;
                slotImages[i].color = new Color(1, 1, 1, 0.2f);
            }

            if (slotCounts != null && slotCounts.Length > i)
            {
                slotCounts[i].text = (slots[i].item != null && slots[i].count > 0) ? slots[i].count.ToString() : "";
            }
        }

        var currentSlot = slots[activeSlot];

        if (itemNameText != null)
        {
            string nameToShow = currentSlot.item != null ? currentSlot.item.itemName : "";

            if (nameDisplayCoroutine != null)
                StopCoroutine(nameDisplayCoroutine);

            nameDisplayCoroutine = StartCoroutine(FadeItemNameText(nameToShow));
        }

        // Тут можна додати оновлення інших UI елементів (статів і т.д.)

        if (activeSlotBorder != null && slotRects != null && activeSlot >= 0 && activeSlot < slotRects.Length)
        {
            activeSlotBorder.SetParent(slotRects[activeSlot].parent, false);
            activeSlotBorder.localPosition = slotRects[activeSlot].localPosition;
            activeSlotBorder.sizeDelta = slotRects[activeSlot].sizeDelta;
            Debug.Log($"Move border to slot {activeSlot}: {slotRects[activeSlot].localPosition}");
        }
    }
}
