using UnityEngine;                      // Підключаємо UnityEngine для роботи з Unity API
using System;                          // Підключаємо System для використання Action
using System.Collections;             // Підключаємо для використання корутин

public class Health : MonoBehaviour    // Оголошення класу Health, який наслідує MonoBehaviour
{
    public int maxHealth = 100;         // Максимальне здоров'я персонажа
    private int currentHealth;          // Поточне значення здоров'я

    public event Action<float> OnHealthChanged;  // Подія, яка повідомляє про зміну здоров'я (від 0 до 1)

    public int CurrentHealth => currentHealth;   // Геттер, щоб отримати поточне здоров'я (тільки для читання)

    [SerializeField] private GameObject playerDeathUI; // Посилання на UI екран смерті гравця, задається в редакторі

    void Awake()                      // Метод викликається при створенні об'єкта
    {
        currentHealth = maxHealth;    // Встановлюємо початкове здоров'я рівним максимальному
        OnHealthChanged?.Invoke((float)currentHealth / maxHealth); // Викликаємо подію про зміну здоров'я (100%)

        if (gameObject.CompareTag("Player") && playerDeathUI != null) // Якщо це гравець і UI заданий
        {
            playerDeathUI.SetActive(false); // Вимикаємо UI смерті на початку гри
        }
    }

    public void TakeDamage(int amount)  // Метод для нанесення шкоди
    {
        currentHealth -= amount;         // Зменшуємо поточне здоров'я на задану величину
        if (currentHealth < 0)           // Якщо здоров'я стало менше нуля
            currentHealth = 0;           // Встановлюємо здоров'я в 0, щоб не було від’ємних значень

        OnHealthChanged?.Invoke((float)currentHealth / maxHealth); // Оповіщаємо про зміну здоров'я

        if (currentHealth <= 0)          // Якщо здоров'я досягло нуля або менше
            Die();                      // Викликаємо метод смерті
    }

    public void Heal(int amount)        // Метод для лікування персонажа
    {
        currentHealth += amount;         // Збільшуємо поточне здоров'я на задану величину
        if (currentHealth > maxHealth)  // Якщо здоров'я перевищує максимум
            currentHealth = maxHealth;  // Встановлюємо поточне здоров'я рівним максимальному

        OnHealthChanged?.Invoke((float)currentHealth / maxHealth); // Оповіщаємо про зміну здоров'я
    }

    void Die()                        // Метод, що викликається при смерті персонажа
    {
        

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(); // Отримуємо всі Collider2D на об'єкті і дітей
        foreach (Collider2D col in colliders) col.enabled = false;      // Вимикаємо кожен Collider2D

        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();      // Отримуємо всі MonoBehaviour-скрипти на об'єкті
        foreach (MonoBehaviour script in scripts)                       // Перебираємо всі скрипти
        {
            if (script != this)                                         // Якщо це не скрипт Health
                script.enabled = false;                                 // Вимикаємо його
        }

        if (gameObject.CompareTag("Player"))                           // Якщо це гравець
        {
            Debug.Log("Player has died! Displaying 'YOU DIED' screen."); // Виводимо повідомлення про смерть гравця
            if (playerDeathUI != null)                                  // Якщо UI смерті заданий
                playerDeathUI.SetActive(true);                          // Активуємо UI смерті

            gameObject.SetActive(false);                                // Приховуємо гравця (не знищуємо)
            // Якщо потрібно знищити об'єкт — можна використати DestroyAfterDelay()
        }
        else                                                           // Якщо це ворог чи інший персонаж
        {
            Debug.Log("Enemy has died. Deactivating object.");         // Виводимо повідомлення про смерть ворога
            StartCoroutine(DestroyAfterDelay(0.1f));                   // Запускаємо корутину знищення з затримкою
        }
    }

    private IEnumerator DestroyAfterDelay(float delay)  // Корутина для знищення об'єкта через затримку
    {
        yield return new WaitForSeconds(delay);           // Чекаємо вказану кількість секунд
        
        

        Destroy(gameObject);                              // Знищуємо ігровий об'єкт
    }
}
