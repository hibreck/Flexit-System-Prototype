using UnityEngine;

public class Thirst : MonoBehaviour
{
    [SerializeField] private float maxThirst = 100f;
    [SerializeField] private float thirstDecreasePerSecond = 1.5f;
    [SerializeField] private float healthLossPerSecond = 5f;
    [SerializeField] private float healthLossInterval = 1f; // Interval for health loss in seconds
    private float thirst;
    private Health health;
    private float healthLossTimer = 0f;

    private PlayerMovement playerMovement;
    private float baseMaxThirst;
    private bool thirstReduced = false;
    [SerializeField] private float maxThirstPenalty = 10f; // на скільки зменшувати
    [SerializeField] private float lowStaminaThirstMultiplier = 2f; // множник при низькій стаміні

    [SerializeField] private float lowStaminaThreshold = 50f;

    public float ThirstValue => thirst;
    public float MaxThirst => maxThirst;

    private void Awake()
    {
        thirst = maxThirst;
        baseMaxThirst = maxThirst;
        health = GetComponent<Health>();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        float hungerLoss = thirstDecreasePerSecond;
        if (playerMovement != null && playerMovement.GetStamina() < 50f)
        {
            hungerLoss *= lowStaminaThirstMultiplier;
        }
        thirst -= hungerLoss * Time.deltaTime;
        thirst = Mathf.Clamp(thirst, 0, maxThirst);
        thirst = Mathf.Clamp(thirst, 0, maxThirst);

        // Зменшення maxThirst при низькій стаміні
        if (playerMovement != null)
        {
            if (playerMovement.GetStamina() < lowStaminaThreshold && !thirstReduced)
            {
                maxThirst = Mathf.Max(0, maxThirst - maxThirstPenalty);
                thirst = Mathf.Min(thirst, maxThirst); // не більше нового максимуму
                thirstReduced = true;
            }
            else if (playerMovement.GetStamina() >= lowStaminaThreshold && thirstReduced)
            {
                maxThirst = baseMaxThirst;
                thirstReduced = false;
            }
        }

        // Втрата HP при нульовій спразі
        if (thirst <= 0f && health != null)
        {
            healthLossTimer += Time.deltaTime;
            if (healthLossTimer >= healthLossInterval)
            {
                health.TakeDamage(Mathf.CeilToInt(healthLossPerSecond));
                healthLossTimer = 0f;
            }
        }
        else
        {
            healthLossTimer = 0f; // Скидаємо таймер, якщо thirst > 0
        }
    }

    public void Drink(float amount)
    {
        thirst = Mathf.Clamp(thirst + amount, 0, maxThirst);
    }
}