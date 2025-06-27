using UnityEngine;                      // ϳ�������� UnityEngine ��� ������ � Unity API
using System;                          // ϳ�������� System ��� ������������ Action
using System.Collections;             // ϳ�������� ��� ������������ �������

public class Health : MonoBehaviour    // ���������� ����� Health, ���� ������ MonoBehaviour
{
    public int maxHealth = 100;         // ����������� ������'� ���������
    private int currentHealth;          // ������� �������� ������'�

    public event Action<float> OnHealthChanged;  // ����, ��� ��������� ��� ���� ������'� (�� 0 �� 1)

    public int CurrentHealth => currentHealth;   // ������, ��� �������� ������� ������'� (����� ��� �������)

    [SerializeField] private GameObject playerDeathUI; // ��������� �� UI ����� ����� ������, �������� � ��������

    void Awake()                      // ����� ����������� ��� �������� ��'����
    {
        currentHealth = maxHealth;    // ������������ ��������� ������'� ����� �������������
        OnHealthChanged?.Invoke((float)currentHealth / maxHealth); // ��������� ���� ��� ���� ������'� (100%)

        if (gameObject.CompareTag("Player") && playerDeathUI != null) // ���� �� ������� � UI �������
        {
            playerDeathUI.SetActive(false); // �������� UI ����� �� ������� ���
        }
    }

    public void TakeDamage(int amount)  // ����� ��� ��������� �����
    {
        currentHealth -= amount;         // �������� ������� ������'� �� ������ ��������
        if (currentHealth < 0)           // ���� ������'� ����� ����� ����
            currentHealth = 0;           // ������������ ������'� � 0, ��� �� ���� �䒺���� �������

        OnHealthChanged?.Invoke((float)currentHealth / maxHealth); // �������� ��� ���� ������'�

        if (currentHealth <= 0)          // ���� ������'� ������� ���� ��� �����
            Die();                      // ��������� ����� �����
    }

    public void Heal(int amount)        // ����� ��� �������� ���������
    {
        currentHealth += amount;         // �������� ������� ������'� �� ������ ��������
        if (currentHealth > maxHealth)  // ���� ������'� �������� ��������
            currentHealth = maxHealth;  // ������������ ������� ������'� ����� �������������

        OnHealthChanged?.Invoke((float)currentHealth / maxHealth); // �������� ��� ���� ������'�
    }

    void Die()                        // �����, �� ����������� ��� ����� ���������
    {
        

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(); // �������� �� Collider2D �� ��'��� � ����
        foreach (Collider2D col in colliders) col.enabled = false;      // �������� ����� Collider2D

        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();      // �������� �� MonoBehaviour-������� �� ��'���
        foreach (MonoBehaviour script in scripts)                       // ���������� �� �������
        {
            if (script != this)                                         // ���� �� �� ������ Health
                script.enabled = false;                                 // �������� ����
        }

        if (gameObject.CompareTag("Player"))                           // ���� �� �������
        {
            Debug.Log("Player has died! Displaying 'YOU DIED' screen."); // �������� ����������� ��� ������ ������
            if (playerDeathUI != null)                                  // ���� UI ����� �������
                playerDeathUI.SetActive(true);                          // �������� UI �����

            gameObject.SetActive(false);                                // ��������� ������ (�� �������)
            // ���� ������� ������� ��'��� � ����� ����������� DestroyAfterDelay()
        }
        else                                                           // ���� �� ����� �� ����� ��������
        {
            Debug.Log("Enemy has died. Deactivating object.");         // �������� ����������� ��� ������ ������
            StartCoroutine(DestroyAfterDelay(0.1f));                   // ��������� �������� �������� � ���������
        }
    }

    private IEnumerator DestroyAfterDelay(float delay)  // �������� ��� �������� ��'���� ����� ��������
    {
        yield return new WaitForSeconds(delay);           // ������ ������� ������� ������
        
        

        Destroy(gameObject);                              // ������� ������� ��'���
    }
}
