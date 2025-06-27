using UnityEngine;

public class Hunger : MonoBehaviour
{
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float hungerDecreasePerSecond = 1f;
    [SerializeField] private float healthLossPerTick = 5f;
    [SerializeField] private float healthLossInterval = 1f; // Затримка між уронами (секунд)
    private float hunger;
    private Health health;
    private float healthLossTimer = 0f;

    private PlayerMovement playerMovement;
    private float baseMaxHunger;
    private bool hungerReduced = false;
    [SerializeField] private float maxHungerPenalty = 10f; // на скільки зменшувати
    [SerializeField] private float lowStaminaHungerMultiplier = 2f; // множник при низькій стаміні
    [SerializeField] private float lowStaminaThreshold = 50f;

    public float HungerValue => hunger;
    public float MaxHunger => maxHunger;

    private void Awake()
    {
        hunger = maxHunger;
        health = GetComponent<Health>();
        baseMaxHunger = maxHunger;
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        float hungerLoss = hungerDecreasePerSecond;
        if (playerMovement != null && playerMovement.GetStamina() < 50f)
        {
            hungerLoss *= lowStaminaHungerMultiplier;
        }
        hunger -= hungerLoss * Time.deltaTime;
        hunger = Mathf.Clamp(hunger, 0, maxHunger);
        hunger = Mathf.Clamp(hunger, 0, maxHunger);

        if (hunger <= 0f && health != null)
        {
            healthLossTimer += Time.deltaTime;
            if (healthLossTimer >= healthLossInterval)
            {
                health.TakeDamage(Mathf.CeilToInt(healthLossPerTick));
                healthLossTimer = 0f;
            }
        }
        if (playerMovement != null)
        {
            if (playerMovement.GetStamina() < lowStaminaThreshold && !hungerReduced)
            {
                maxHunger = Mathf.Max(0, maxHunger - maxHungerPenalty);
                hunger = Mathf.Min(hunger, maxHunger); // не більше нового максимуму
                hungerReduced = true;
            }
            else if (playerMovement.GetStamina() >= lowStaminaThreshold && hungerReduced)
            {
                maxHunger = baseMaxHunger;
                hungerReduced = false;
            }
        }
        else
        {
            healthLossTimer = 0f; // Скидаємо таймер, якщо голод не на нулі
        }
    }

    public void Eat(float amount)
    {
        hunger = Mathf.Clamp(hunger + amount, 0, maxHunger);
    }
}