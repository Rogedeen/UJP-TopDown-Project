using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int playerHealth = 100;
    public int maxPlayerHealth = 100;
    public float invincibilityTime = 1f;

    [Header("Visual Effects")]
    [Tooltip("Dokunulmaz olduğunda (hasar yenince veya kart seçilince) açılacak kalkan objesi.")]
    public GameObject invincibilityShield;

    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerController playerController;



    private bool isInvincible = false;
    private bool isDead = false;

    void Start()
    {
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible || isDead) return;

        playerController.TakeDamageEffect();
        playerHealth -= damage;


        if (playerHealth <= 0)
        {
            isDead = true;
            gameManager.GameOver();
            return;
        }

        StartCoroutine(InvincibleRoutine(invincibilityTime, false));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyBase enemy = collision.gameObject.GetComponentInParent<EnemyBase>();
            if (enemy != null && enemy.dealsContactDamage)
            {
                float damageMultiplier = DayNightManager.Instance != null ? DayNightManager.Instance.CurrentEnemyDamageMultiplier : 1f;
                TakeDamage(Mathf.RoundToInt(enemy.contactDamage * damageMultiplier));
            }
        }
    }

    IEnumerator InvincibleRoutine(float duration, bool useBlinking = false)
    {
        isInvincible = true;
        
        // Kalkan özelliği ileride power-up olarak eklenmek üzere hazırda bekliyor
        if (invincibilityShield != null && invincibilityShield.activeSelf == false) 
        {
            // İleride kalkan skill'i eklersek burayı true yapabiliriz
            // invincibilityShield.SetActive(true);
        }

        if (useBlinking)
        {
            float elapsed = 0f;
            bool isVisible = true;
            
            // Tüm rendererları al ama Particle veya Trail olan (VFX) renderları yoksay!
            var renderers = new System.Collections.Generic.List<Renderer>();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                // Görsel efektlerin (VFX) ve ayrıca kılıcın (OrbitWeapon) kendi kendine görünür olmasını engelle
                if (!(r is ParticleSystemRenderer) && !(r is TrailRenderer) && r.GetComponentInParent<OrbitWeapon>() == null)
                    renderers.Add(r);
            }

            while (elapsed < duration)
            {
                elapsed += 0.15f;
                isVisible = !isVisible;
                foreach (var r in renderers) { if (r != null) r.enabled = isVisible; }
                yield return new WaitForSeconds(0.15f);
            }
            
            foreach (var r in renderers) { if (r != null) r.enabled = true; }
        }
        else
        {
            yield return new WaitForSeconds(duration);
        }
        
        if (invincibilityShield != null) invincibilityShield.SetActive(false);
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
        

    }

    /// <summary>
    /// Mevcut canı iyileştirir (MaxHealth'i geçemez).
    /// </summary>
    public void Heal(int amount)
    {
        playerHealth += amount;
        if (playerHealth > maxPlayerHealth) playerHealth = maxPlayerHealth;


    }

    public IEnumerator PostUpgradeInvincibilityRoutine()
    {
        // Eski sisteme dönüldü: 2.5 saniye boyunca yanıp sönüyor (ama artık VFX'ler etkilenmiyor)
        yield return StartCoroutine(InvincibleRoutine(2.5f, true));
    }
}