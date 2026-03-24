using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;

public class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("Core Stats")]
    public int health = 20;
    public int maxHealth = 20;
    public bool canTakeDamage = true;
    public float invincibilityDuration = 0.5f;

    [Tooltip("Eğer kapatılırsa oyuncu bu düşmana çarptığında fiziksel temas hasarı almaz (Örn: Büyücüler)")]
    public bool dealsContactDamage = true;
    [Tooltip("Oyuncuya temas ettiğinde vereceği hasar miktarı")]
    public int contactDamage = 10;

    [Header("Loot")]
    [Tooltip("Ölünce düşen power-up için ağırlık. Normal=1, Strong=2, Wizard=3")]
    public int lootWeight = 1;

    [Header("Knockback")]
    [SerializeField] protected float knockbackSpeed = 10f;
    [SerializeField] protected float knockbackDuration = 0.25f;

    [Header("UI")]
    public Slider enemyHealthSlider;

    [Header("Hit Flash")]
    public Material hitFlashMaterial;

    protected Rigidbody enemyRb;
    protected Animator animator;
    protected bool isKnockedBack = false;
    protected GameObject player;

    protected Renderer[] renderers;
    protected Material[] originalMaterials;
    protected NavMeshAgent agent;

    // Animator parametrelerini hash olarak sakla (performans için)
    // String karşılaştırması her frame yapılırsa yavaş kalır, hash ile anlık olur
    protected static readonly int TakeDamageHash = Animator.StringToHash("TakeDamage");
    protected static readonly int AttackHash = Animator.StringToHash("Attack");
    protected static readonly int DieHash = Animator.StringToHash("Die");
    protected static readonly int IsFizzyHash = Animator.StringToHash("isFizzy");

    // Statik oyuncu referansı: Tüm düşmanlar aynı referansı paylaşır.
    // Player sahnede aktif olduğunda kendini buraya kaydeder.
    // Bu sayede her düşmanın tek tek FindGameObjectWithTag yapmasına gerek kalmaz.
    private static GameObject _cachedPlayer;
    public static GameObject CachedPlayer
    {
        get
        {
            // Eğer cache boşsa veya obje yok edilmişse yeniden bul
            if (_cachedPlayer == null)
                _cachedPlayer = GameObject.FindGameObjectWithTag("Player");
            return _cachedPlayer;
        }
    }

    protected virtual void OnEnable()
    {
        // Object Pool'dan (havuzdan) çekildiğinde değerleri ilk baştaki taze haline sıfırla
        health = maxHealth;
        canTakeDamage = true;
        isKnockedBack = false;
        
        if (enemyRb != null)
        {
            enemyRb.isKinematic = false;
            enemyRb.linearVelocity = Vector3.zero;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }

        if (enemyHealthSlider != null)
        {
            enemyHealthSlider.value = health;
            enemyHealthSlider.gameObject.SetActive(false);
        }

        // Animasyonları sıfırlamak için Rebind hayat kurtarıcıdır
        if (animator != null)
        {
            SafeSetBool(animator, IsFizzyHash, false);
            animator.Rebind();
            animator.Update(0f);
        }
    }

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        renderers = GetComponentsInChildren<Renderer>();
        List<Material> originals = new();
        foreach (Renderer r in renderers)
            originals.AddRange(r.materials);
        originalMaterials = originals.ToArray();
    }

    protected virtual void Start()
    {
        enemyRb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        player = CachedPlayer;

        // Varsa Gece/Gündüz zorluk çarpanını NavMesh hızına uygula
        if (agent != null && DayNightManager.Instance != null)
        {
            agent.speed *= DayNightManager.Instance.CurrentEnemySpeedMultiplier;
        }

        if (enemyHealthSlider != null)
        {
            enemyHealthSlider.maxValue = maxHealth;
            enemyHealthSlider.value = health;
            enemyHealthSlider.gameObject.SetActive(false);
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!canTakeDamage || health <= 0) return;

        if (other.CompareTag("Weapon") || other.CompareTag("OrbitWeapon"))
        {
            int damageValue = 1;

            if (other.TryGetComponent<Weapon>(out var w)) damageValue = w.damage;
            else if (other.TryGetComponent<OrbitWeapon>(out var ow)) damageValue = ow.damage;

            TakeDamage(damageValue, player.transform.position);
        }
    }

    public virtual void TakeDamage(int damage, Vector3 knockbackSource, float knockbackMultiplier = 1f)
    {
        if (!canTakeDamage) return;
        health -= damage;

        if (enemyHealthSlider != null)
        {
            enemyHealthSlider.value = health;
            enemyHealthSlider.gameObject.SetActive(true);
        }

        SafeResetTrigger(animator, TakeDamageHash);
        SafeSetTrigger(animator, TakeDamageHash);
        StartCoroutine(HitFlashRoutine());
        StartCoroutine(ApplyKnockback(knockbackSource, knockbackMultiplier));

        if (health <= 0)
        {
            StartCoroutine(DieRoutine());
            return;
        }

        canTakeDamage = false;
        StartCoroutine(Invincible(invincibilityDuration));
    }

    protected IEnumerator ApplyKnockback(Vector3 source, float knockbackMultiplier = 1f)
    {
        isKnockedBack = true;

        Vector3 pushDir = (transform.position - source).normalized;
        pushDir.y = 0;

        float elapsed = 0f;

        if (agent != null)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;

            float t = 1f - (elapsed / knockbackDuration);
            Vector3 movement = (knockbackSpeed * knockbackMultiplier) * t * Time.deltaTime * pushDir;

            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.Warp(transform.position + movement);
            }

            yield return null;
        }

        isKnockedBack = false;
    }

    protected IEnumerator HitFlashRoutine()
    {
        foreach (Renderer r in renderers)
        {
            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = hitFlashMaterial;

            r.materials = mats;
        }

        yield return new WaitForSeconds(0.1f);

        int index = 0;
        foreach (Renderer r in renderers)
        {
            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
                mats[i] = originalMaterials[index++];

            r.materials = mats;
        }
    }

    protected virtual IEnumerator DieRoutine()
    {
        canTakeDamage = false;

        // Bazı düşman Animator Controller'larında tüm parametreler olmayabilir
        // Bu yüzden güvenli şekilde set ediyoruz
        SafeResetTrigger(animator, AttackHash);
        SafeResetTrigger(animator, TakeDamageHash);
        SafeSetBool(animator, IsFizzyHash, true);
        SafeSetTrigger(animator, DieHash);

        if (!enemyRb.isKinematic)
            enemyRb.linearVelocity = Vector3.zero;

        GetComponent<Collider>().enabled = false;

        // Loot drop: Ölüm pozisyonunda ağırlığa uygun power-up düşür
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.TryDropLoot(transform.position, lootWeight);
        }

        // Event sistemi ile "bir düşman öldü" sinyali yayınla (XP ile birlikte)
        // WaveManager ve UpgradeManager bu sinyali dinliyor
        GameEvents.EnemyDied(lootWeight);
        enemyRb.isKinematic = true;

        yield return new WaitForSeconds(2f);
        
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Despawn(gameObject);
        }
        else
        {
            Destroy(gameObject); // Fallback fail-safe
        }
    }

    // ─── GÜVENLİ ANİMATOR YARDIMCILARI ───
    // Her Animator Controller'da her parametre olmayabilir
    // Bu metodlar hata fırlatmak yerine sessizce atlar

    protected static void SafeResetTrigger(Animator anim, int hash)
    {
        if (HasParameter(anim, hash)) anim.ResetTrigger(hash);
    }

    protected static void SafeSetTrigger(Animator anim, int hash)
    {
        if (HasParameter(anim, hash)) anim.SetTrigger(hash);
    }

    protected static void SafeSetBool(Animator anim, int hash, bool value)
    {
        if (HasParameter(anim, hash)) anim.SetBool(hash, value);
    }

    private static bool HasParameter(Animator anim, int hash)
    {
        foreach (var param in anim.parameters)
        {
            if (param.nameHash == hash) return true;
        }
        return false;
    }

    public void ForceKill()
    {
        if (health <= 0) return;
        health = 0;
        StartCoroutine(DieRoutine());
    }

    public virtual void Heal(int amount)
    {
        if (health <= 0) return;

        health = Mathf.Min(health + amount, maxHealth);

        if (enemyHealthSlider != null)
            enemyHealthSlider.value = health;
    }

    protected IEnumerator Invincible(float duration)
    {
        yield return new WaitForSeconds(duration);
        canTakeDamage = true;
    }
}

