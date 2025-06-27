using System.Collections;                    // Для корутин (IEnumerator)
using UnityEngine;                           // Основний простір імен Unity
using UnityEngine.InputSystem;               // Нова система вводу Unity (Input System)

public class PlayerMovement : MonoBehaviour // Клас для керування рухом гравця, наслідується від MonoBehaviour
{
    [SerializeField] private float walkSpeed = 3f;        // Швидкість ходьби
    [SerializeField] private float runSpeed = 6f;         // Швидкість бігу
    [SerializeField] private float dashForce = 15f;       // Сила ривка (даш)
    [SerializeField] private float dashDuration = 0.2f;   // Тривалість ривка у секундах
    [SerializeField] private float dashCooldown = 1f;     // Кулдаун між дашами

    [Header("Stamina")]                                 // Заголовок у інспекторі для витривалості
    [SerializeField] private float maxStamina = 100f;    // Максимальна витривалість
    private float stamina;                               // Поточний запас витривалості
    [SerializeField] private float staminaDrainRun = 15f;// Витрата витривалості за секунду при бігу
    [SerializeField] private float staminaDrainDash = 30f;// Витрата витривалості при ривку (даш)
    [SerializeField] private float staminaRecoveryRate = 10f; // Відновлення витривалості за секунду

    private InputSystem_Actions playerControls;               // Екземпляр автозгенерованого класу для вводу
    private Vector2 movement;                             // Напрямок руху (вхідні дані)
    private Rigidbody2D rb;                               // Rigidbody2D для фізичного руху гравця
    private InputAction runAction;                        // Дія бігу (Shift)

    private float lastDashTime = -999f;                   // Час останнього дашу для кулдауну
    private bool isDashing = false;                        // Чи зараз триває даш

    private float lastTapTime = 0f;                        // Час останнього тапу (для подвійного тапу)
    private Vector2 lastTapDirection = Vector2.zero;      // Напрямок останнього тапу
    private float doubleTapThreshold = 0.3f;               // Максимальний інтервал між тапами (подвійний тап)

    private bool isRunning = false;                         // Чи гравець зараз біжить

    private void Awake()                                   // Метод викликається при ініціалізації скрипта
    {
        playerControls = new InputSystem_Actions();            // Ініціалізація системи вводу
        rb = GetComponent<Rigidbody2D>();                  // Отримуємо Rigidbody2D з цього GameObject
        stamina = maxStamina;                              // Встановлюємо початкову витривалість на максимум

        runAction = playerControls.Player.Sprint;             // Отримуємо дію бігу з Input System
        runAction.Enable();                                // Активуємо дію бігу
    }

    private void OnEnable()                               // Метод викликається при активації об'єкта
    {
        playerControls.Enable();                           // Активуємо систему вводу
        playerControls.Player.Move.performed += OnMovePerformed; // Підписуємося на подію натискання руху
    }

    private void OnDisable()                              // Метод викликається при деактивації об'єкта
    {
        playerControls.Player.Move.performed -= OnMovePerformed; // Відписуємося від події руху
        playerControls.Disable();                          // Вимикаємо систему вводу
        runAction.Disable();                               // Вимикаємо дію бігу
    }

    private void Update()                                 // Метод викликається щокадру
    {
        HandleRunning();                                  // Обробляємо логіку бігу і витривалості
        RecoverStamina();                                 // Відновлення витривалості, якщо це можливо
    }

    private void FixedUpdate()                            // Викликається на кожен фізичний кадр (фізика)
    {
        if (isKnockedBack)                                // Якщо триває knockback
        {
            if (Time.time >= knockbackEndTime)            // Якщо час knockback минув
                isKnockedBack = false;                     // Скидаємо стан knockback

            return;                                       // Виходимо з FixedUpdate (рух не виконуємо)
        }

        if (!isDashing)                                   // Якщо не дашимо (не ривок)
        {
            Move();                                       // Виконуємо звичайний рух
        }
    }

    private void PlayerInput()                            // Метод оновлення напрямку руху з вводу
    {
        movement = playerControls.Player.Move.ReadValue<Vector2>(); // Читаємо вектор руху з Input System
    }

    private void Move()                                   // Метод руху гравця
    {
        float currentSpeed = isRunning ? runSpeed : walkSpeed; // Визначаємо швидкість залежно від стану бігу
        rb.linearVelocity = movement * currentSpeed;           // Встановлюємо швидкість Rigidbody2D
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx) // Обробник події натискання клавіш руху
    {
        Vector2 inputDir = ctx.ReadValue<Vector2>();             // Отримуємо напрямок руху

        if (inputDir != Vector2.zero)                             // Якщо напрямок не нульовий
        {
            if (inputDir == lastTapDirection && Time.time - lastTapTime < doubleTapThreshold) // Перевіряємо подвійний тап
            {
                TryDash(inputDir);                                // Спроба дашу в напрямку
                lastTapTime = 0f;                                 // Скидаємо час останнього тапу
                lastTapDirection = Vector2.zero;                  // Скидаємо напрямок останнього тапу
            }
            else
            {
                lastTapDirection = inputDir;                      // Запам'ятовуємо напрямок тапу
                lastTapTime = Time.time;                           // Запам'ятовуємо час тапу
            }
        }
    }

    private void TryDash(Vector2 direction)                   // Метод для початку дашу
    {
        if (Time.time < lastDashTime + dashCooldown) return;  // Якщо кулдаун ще не минув — вихід
        if (stamina < staminaDrainDash) return;               // Якщо витривалості замало — вихід

        StartCoroutine(DashRoutine(direction.normalized));    // Запускаємо корутину дашу
        stamina -= staminaDrainDash;                           // Віднімаємо витривалість за даш
    }

    private IEnumerator DashRoutine(Vector2 direction)       // Корутіна, що реалізує даш
    {
        isDashing = true;                                      // Встановлюємо стан дашу активним
        float dashEndTime = Time.time + dashDuration;          // Обчислюємо час кінця дашу

        while (Time.time < dashEndTime)                         // Поки не закінчився час дашу
        {
            rb.linearVelocity = direction * dashForce;          // Встановлюємо швидкість для дашу

            yield return new WaitForFixedUpdate();              // Чекаємо наступного фізичного кадру
        }

        isDashing = false;                                      // По завершенню дашу скидаємо стан
        lastDashTime = Time.time;                               // Запам'ятовуємо час останнього дашу
    }

    private void HandleRunning()                               // Обробка логіки бігу
    {
        bool shiftPressed = runAction.ReadValue<float>() > 0.5f; // Чи натиснуто клавішу бігу (Shift)

        if (shiftPressed && movement.magnitude > 0 && stamina > 0) // Біг можливий при виконанні умов
        {
            isRunning = true;                                   // Встановлюємо стан бігу
            stamina -= staminaDrainRun * Time.deltaTime;       // Витрачаємо витривалість за час

            if (stamina <= 0f)                                  // Якщо витривалість вичерпалась
            {
                stamina = 0f;                                   // Обнуляємо витривалість
                isRunning = false;                              // Припиняємо біг
            }
        }
        else
        {
            isRunning = false;                                  // Якщо умови не виконані — не біжимо
        }
    }

    public void RecoverStamina()                              // Відновлення витривалості
    {
        if (!isRunning && !isDashing && stamina < maxStamina)  // Відновлюємо, якщо не бігаємо і не дашимо і витривалість не максимальна
        {
            stamina += staminaRecoveryRate * Time.deltaTime;   // Відновлюємо витривалість за секунду
            stamina = Mathf.Min(stamina, maxStamina);           // Обмежуємо максимальною витривалістю
        }
    }
    public void RecoverStamina(float amount)
    {
        stamina = Mathf.Min(stamina + amount, maxStamina);
    }
    private void LateUpdate()                                  // Викликається після Update
    {
        PlayerInput();                                         // Зчитуємо ввід пізніше, щоб врахувати всі зміни
    }

    private bool isKnockedBack = false;                        // Чи активний knockback (відкидання)
    private float knockbackEndTime = 0f;                       // Час завершення knockback

    // Метод для зовнішнього виклику knockback (відкидання):
    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        rb.linearVelocity = Vector2.zero;                      // Скидаємо поточну швидкість
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse); // Додаємо імпульс knockback у напрямку
        isKnockedBack = true;                                   // Встановлюємо стан knockback активним
        knockbackEndTime = Time.time + duration;                // Записуємо час завершення knockback
    }

    // Геттер для UI (поточної витривалості)
    public float GetStamina() => stamina;

    // Геттер для UI (максимальної витривалості)
    public float GetMaxStamina() => maxStamina;
}
