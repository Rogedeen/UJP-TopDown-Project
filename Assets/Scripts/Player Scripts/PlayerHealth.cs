using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int playerHealth = 5;
    public int maxPlayerHealth = 5;
    public float invincibilityTime = 1f;

    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerController playerController;

    public Slider playerHealthBar;

    private bool isInvincible = false;
    private bool isDead = false;

    void Start()
    {
        if (playerHealthBar != null)
        {
            playerHealthBar.maxValue = maxPlayerHealth;
            playerHealthBar.value = playerHealth;
            playerHealthBar.gameObject.SetActive(false);
        }

        
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible || isDead) return;

        playerController.TakeDamageEffect();
        playerHealth -= damage;
        if (playerHealthBar != null)
        {
            playerHealthBar.value = playerHealth;
            playerHealthBar.gameObject.SetActive(true);
        }

        if (playerHealth <= 0)
        {
            isDead = true;
            gameManager.GameOver();
            return;
        }

        StartCoroutine(InvincibleRoutine());
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyBase enemy = collision.gameObject.GetComponentInParent<EnemyBase>();
            if (enemy != null && enemy.dealsContactDamage)
            {
                float damageMultiplier = DayNightManager.Instance != null ? DayNightManager.Instance.CurrentEnemyDamageMultiplier : 1f;
                TakeDamage(Mathf.RoundToInt(1 * damageMultiplier));
            }
        }
    }

    IEnumerator InvincibleRoutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityTime);
        isInvincible = false;
    }

    /// <summary>
    /// Dash gibi mekanikler sırasında dışarıdan invincibility açıp kapamak için.
    /// Hasar alındıktan sonraki otomatik invincibility ile karışmaz.
    /// </summary>
    public void SetInvincible(bool value)
    {
        isInvincible = value;
    }

    /// <summary>
    /// StatUpgrade'den çağrılıp maksimum canı (kalp sayısını) kalıcı olarak artırır.
    /// </summary>
    public void IncreaseMaxHealth(int amount)
    {
        maxPlayerHealth += amount;
        playerHealth += amount; // Yeni gelen kalbi dolu olarak veriyoruz
        
        if (playerHealthBar != null)
        {
            playerHealthBar.maxValue = maxPlayerHealth;
            playerHealthBar.value = playerHealth;
        }
    }

    /// <summary>
    /// Mevcut canı iyileştirir (MaxHealth'i geçemez).
    /// </summary>
    public void Heal(int amount)
    {
        playerHealth += amount;
        if (playerHealth > maxPlayerHealth) playerHealth = maxPlayerHealth;

        if (playerHealthBar != null)
        {
            playerHealthBar.value = playerHealth;
        }
    }

    public IEnumerator PostUpgradeInvincibilityRoutine()
    {
        isInvincible = true;
        float blinkDuration = 2.5f;
        float elapsed = 0f;
        bool isVisible = true;
        
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        while (elapsed < blinkDuration)
        {
            elapsed += 0.15f;
            isVisible = !isVisible;
            foreach (var r in renderers) { if(r != null) r.enabled = isVisible; }
            yield return new WaitForSeconds(0.15f);
        }
        
        foreach (var r in renderers) { if(r != null) r.enabled = true; }
        isInvincible = false;
    }
}