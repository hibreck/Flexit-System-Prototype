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
    public StatsIconConfig statsIcons; // Признач у інспекторі
    [Header("Weapon Stats Settings")]
    public Image damageImage;
    public Image cooldownImage;   
    public Image knockbackImage;
    public TMPro.TextMeshProUGUI damageText;
    public TMPro.TextMeshProUGUI cooldownText;
    public TMPro.TextMeshProUGUI knockbackText;
    public StatsWeaponIconConfig weaponIcons; // Признач у інспекторі

    private PlayerControls controls;
    private InputAction scrollSlotAction;
    private InputAction useActiveSlotAction;


    private void Awake()
    {
        controls = new PlayerControls();
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

            // Якщо це не зброя — зменшуємо count
            if (!(slot.item is MacheteItem)) // або: if (!(slot.item is WeaponItem))
            {
                slot.count--;
                if (slot.count == 0)
                    slot.item = null;
            }
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

        var slot = slots[activeSlot];

        // Вивід назви предмета
        var currentSlot = slots[activeSlot];

        if (itemNameText != null)
        {
            itemNameText.text = currentSlot.item != null ? currentSlot.item.itemName : " ";
        }

        Dictionary<string, int> stats = currentSlot.item?.GetStats();

        // 🔹 Автоматичне оновлення всіх параметрів
        UpdateStatUI(healImage, healText, "heal", stats);
        UpdateStatUI(foodImage, foodText, "food", stats);
        UpdateStatUI(waterImage, waterText, "water", stats);
        UpdateStatUI(staminaImage, staminaText, "stamina", stats);
        UpdateStatUI(damageImage, damageText, "damage", stats);
        UpdateStatUI(cooldownImage, cooldownText, "cooldown", stats, "s");
        UpdateStatUI(knockbackImage, knockbackText, "knockback", stats);


        // 🔹 Оновлення іконок автоматично
        healImage.sprite = stats != null && stats.ContainsKey("heal") ? statsIcons.healIcon : defaultIcon;
        healImage.gameObject.SetActive(stats != null && stats.ContainsKey("heal"));

        foodImage.sprite = stats != null && stats.ContainsKey("food") ? statsIcons.foodIcon : defaultIcon;
        foodImage.gameObject.SetActive(stats != null && stats.ContainsKey("food"));

        waterImage.sprite = stats != null && stats.ContainsKey("water") ? statsIcons.waterIcon : defaultIcon;
        waterImage.gameObject.SetActive(stats != null && stats.ContainsKey("water"));

        staminaImage.sprite = stats != null && stats.ContainsKey("stamina") ? statsIcons.staminaIcon : defaultIcon;
        staminaImage.gameObject.SetActive(stats != null && stats.ContainsKey("stamina"));

        damageImage.sprite = stats != null && stats.ContainsKey("damage") ? weaponIcons.damageIcon : defaultIcon;
        damageImage.gameObject.SetActive(stats != null && stats.ContainsKey("damage"));

        cooldownImage.sprite = stats != null && stats.ContainsKey("cooldown") ? weaponIcons.cooldownIcon : defaultIcon;
        cooldownImage.gameObject.SetActive(stats != null && stats.ContainsKey("cooldown"));

        knockbackImage.sprite = stats != null && stats.ContainsKey("knockback") ? weaponIcons.knockbackIcon : defaultIcon;
        knockbackImage.gameObject.SetActive(stats != null && stats.ContainsKey("knockback"));
    

        // Переміщення та ресайз рамки активного слота по контейнеру
        if (activeSlotBorder != null && slotRects != null && activeSlot >= 0 && activeSlot < slotRects.Length)
        {
            activeSlotBorder.SetParent(slotRects[activeSlot].parent, false);
            activeSlotBorder.localPosition = slotRects[activeSlot].localPosition;
            activeSlotBorder.sizeDelta = slotRects[activeSlot].sizeDelta;
            Debug.Log($"Move border to slot {activeSlot}: {slotRects[activeSlot].localPosition}");
        }
    }
    void UpdateStatUI(Image icon, TMPro.TextMeshProUGUI text, string statKey, Dictionary<string, int> stats, string suffix = "")
    {
        bool hasStat = stats != null && stats.ContainsKey(statKey);

        icon.sprite = hasStat ? statsIcons.GetIcon(statKey) : defaultIcon;
        icon.gameObject.SetActive(hasStat);

        text.text = hasStat ? $"{stats[statKey]}{suffix}" : "";
        text.gameObject.SetActive(hasStat);
    }

}

