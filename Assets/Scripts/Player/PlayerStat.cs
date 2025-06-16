using UnityEngine;
using System.Collections;
using System;

public class PlayerStat : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private float baseSpeed = 0f; // Additional speed on top of base
    [SerializeField] private float speedIncreaseRate = 0.1f; // Speed increase per second

    [Header("Power-ups")]
    [SerializeField] private bool isShieldActive = false;
    [SerializeField] private float shieldDuration = 0f;
    [SerializeField] private GameObject shieldEffect; // Visual effect for shield

    // Events
    public event Action<int> OnHealthChanged;
    public event Action OnPlayerDeath;
    public event Action<bool> OnShieldStateChanged;

    private Coroutine shieldCoroutine;
    private float timePlayed = 0f;

    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    private void Update()
    {
        // Gradually increase speed over time
        timePlayed += Time.deltaTime;
        baseSpeed = timePlayed * speedIncreaseRate;
    }

    public void TakeDamage(int damage)
    {
        if (!isShieldActive)
        {
            currentHealth -= damage;
            if (currentHealth < 0) currentHealth = 0;

            OnHealthChanged?.Invoke(currentHealth);

            if (currentHealth <= 0)
            {
                OnPlayerDeath?.Invoke();
                // Handle death - stop game, show game over screen, etc.
            }
        }
        else
        {
            // Shield absorbed the hit - maybe add feedback
            Debug.Log("Shield absorbed damage!");
        }
    }

    public void RestoreHealth(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        OnHealthChanged?.Invoke(currentHealth);
    }

    public void BoostSpeed(float boost, float duration)
    {
        baseSpeed += boost;
        StartCoroutine(RevertSpeed(boost, duration));
    }

    private IEnumerator RevertSpeed(float boost, float duration)
    {
        yield return new WaitForSeconds(duration);
        baseSpeed -= boost;
    }

    public void ActivateShield(float duration)
    {
        if (shieldCoroutine != null)
        {
            StopCoroutine(shieldCoroutine);
        }

        isShieldActive = true;
        shieldDuration = duration;

        if (shieldEffect != null)
            shieldEffect.SetActive(true);

        OnShieldStateChanged?.Invoke(true);

        shieldCoroutine = StartCoroutine(DeactivateShield(duration));
    }

    private IEnumerator DeactivateShield(float duration)
    {
        yield return new WaitForSeconds(duration);
        isShieldActive = false;

        if (shieldEffect != null)
            shieldEffect.SetActive(false);

        OnShieldStateChanged?.Invoke(false);
    }

    // Getters
    public float GetSpeed() => baseSpeed;
    public bool IsShieldActive() => isShieldActive;
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;

    // Reset for new game
    public void ResetStats()
    {
        currentHealth = maxHealth;
        baseSpeed = 0f;
        timePlayed = 0f;
        isShieldActive = false;

        if (shieldCoroutine != null)
        {
            StopCoroutine(shieldCoroutine);
        }

        if (shieldEffect != null)
            shieldEffect.SetActive(false);

        OnHealthChanged?.Invoke(currentHealth);
    }
}