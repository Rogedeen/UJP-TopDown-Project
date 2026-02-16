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
        if (agent != null) agent.enabled = false;
        enemyRb.isKinematic = false;

        Vector3 pushDir = (transform.position - source).normalized;
        enemyRb.linearVelocity = Vector3.zero;
        enemyRb.AddForce(pushDir * 7f, ForceMode.Impulse);

        yield return new WaitForSeconds(0.2f);

        enemyRb.linearVelocity = Vector3.zero;
        enemyRb.isKinematic = true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }

        if (agent != null) agent.enabled = true;
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
