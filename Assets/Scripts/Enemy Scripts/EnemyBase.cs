using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
public class EnemyBase : MonoBehaviour
{
    [Header("Core Stats")]
    public int health = 3;
    public bool canTakeDamage = true;
    public float invincibilityDuration = 0.5f;

    [Header("UI")]
    public Slider enemyHealthSlider;

    [Header("Hit Flash")]
    public Material hitFlashMaterial;

    protected Rigidbody enemyRb;
    protected Animator animator;
    protected bool isKnockedBack = false;
    protected GameObject player; // Buraya aldık

    protected Renderer[] renderers;
    protected Material[] originalMaterials;
    protected NavMeshAgent agent;

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
        player = GameObject.FindGameObjectWithTag("Player"); // Her düşman doğduğunda oyuncuyu bulur

        if (enemyHealthSlider != null)
        {
            enemyHealthSlider.maxValue = health;
            enemyHealthSlider.value = health;
            enemyHealthSlider.gameObject.SetActive(false);
        }
    }
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!canTakeDamage || health <= 0) return;

        if (other.CompareTag("Weapon") || other.CompareTag("OrbitWeapon"))
        {
            Debug.Log("Düşmana çarpan objenin adı: " + other.gameObject.name + " | Tag: " + other.tag);
            int damageValue = 1;

            // Silahın damage değerini alalım
            if (other.TryGetComponent<Weapon>(out var w)) damageValue = w.damage;
            else if (other.TryGetComponent<OrbitWeapon>(out var ow)) damageValue = ow.damage;

            TakeDamage(damageValue, player.transform.position);
        }
    }
    public virtual void TakeDamage(int damage, Vector3 knockbackSource)
    {
        if (!canTakeDamage) return;
        health -= damage;

        if (enemyHealthSlider != null)
        {
            enemyHealthSlider.value = health;
            enemyHealthSlider.gameObject.SetActive(true);
        }

        animator.SetTrigger("TakeDamage");
        StartCoroutine(HitFlashRoutine());
        StartCoroutine(ApplyKnockback(knockbackSource));

        if (health <= 0)
        {
            StartCoroutine(DieRoutine());
            return;
        }

        canTakeDamage = false;
        StartCoroutine(Invincible(invincibilityDuration));
    }

    protected IEnumerator ApplyKnockback(Vector3 source)
    {
        isKnockedBack = true;

        Vector3 pushDir = (transform.position - source).normalized;
        pushDir.y = 0; // Dikey bileşeni sıfırla, NavMesh düzleminde kalalım

        float knockbackSpeed = 8f;    // Başlangıç hızı
        float knockbackDuration = 0.25f;
        float elapsed = 0f;

        // Agent'ı kapatmak yerine, agent'ın kendi velocity'sini sıfırlıyoruz
        // ve hareketi Warp ile biz yönetiyoruz
        if (agent != null)
        {
            agent.ResetPath();          // Hedefe gitmeyi durdur
            agent.velocity = Vector3.zero; // Agent'ın kendi momentumunu sıfırla
        }

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;

            // Hız zamanla azalıyor (easing) - bu daha doğal hissettirir
            float t = 1f - (elapsed / knockbackDuration);
            Vector3 movement = knockbackSpeed * t * Time.deltaTime * pushDir;

            // NavMesh üzerinde güvenli hareket
            if (agent != null && agent.isActiveAndEnabled)
            {
                // Warp, agent'ı NavMesh'e "yapıştırarak" hareket ettirir
                agent.Warp(transform.position + movement);
            }

            yield return null;
        }

        isKnockedBack = false;
        // Artık agent yeni bir path hesaplayabilir
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
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("TakeDamage");
        animator.SetTrigger("Die");

        enemyRb.linearVelocity = Vector3.zero;
        GetComponent<Collider>().enabled = false;

        WaveManager.activeEnemyCount--;
        enemyRb.isKinematic = true;

        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    protected IEnumerator Invincible(float duration)
    {
        yield return new WaitForSeconds(duration);
        canTakeDamage = true;
    }
}
