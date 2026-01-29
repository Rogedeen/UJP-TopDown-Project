using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Net;
using UnityEngine.UI;


public class Enemy : MonoBehaviour
{
    [Header("Enemy values")]
    public float speed = 4.0f;
    public int health = 3;
    public bool canTakeDamage = true;
    public float invincibilityDuration = 0.5f;

    [Header("UI")]
    public Slider enemyHealthSlider;

    private GameObject player;
    private Rigidbody enemyRb;
    private Animator animator;
    private bool isKnockedBack = false;


    void Start()
    {
        enemyRb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");

        if (enemyHealthSlider != null)
        {
            enemyHealthSlider.maxValue = health;
            enemyHealthSlider.value = health;
            enemyHealthSlider.gameObject.SetActive(false);
        }
    }

    void UpdateEnemyHealth()
    {
        if (enemyHealthSlider != null)
        {
            enemyHealthSlider.value = health;
        }
    }
    void FixedUpdate()
    {
        // EĞER düşman öldüyse veya geri itiliyorsa hareket kodunu çalıştırma
        if (player != null && !isKnockedBack && health > 0)
        {
            Vector3 playerDir = (player.transform.position - transform.position).normalized;
            playerDir.y = 0;
            enemyRb.linearVelocity = new(playerDir.x * speed, enemyRb.linearVelocity.y, playerDir.z * speed);
            transform.LookAt(player.transform.position);
            animator.SetFloat("speed_f", speed);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Weapon"))
        {
            if (canTakeDamage)
            {
                Weapon weapon = other.GetComponent<Weapon>();
                TakeDamage(weapon.damage, player.transform.position);
            }
        }

        else if (other.gameObject.CompareTag("OrbitWeapon"))
        {
            if (canTakeDamage)
            {
                OrbitWeapon orbitWeapon = other.GetComponent<OrbitWeapon>();
                TakeDamage(orbitWeapon.damage, player.transform.position);
            }
        }
    }

    public void TakeDamage(int damage, Vector3 knockbackSource)
    {
        if (!canTakeDamage) return;

        health -= damage;
        UpdateEnemyHealth();

        animator.SetTrigger("TakeDamage");
        StartCoroutine(HitFlashRoutine());

        enemyHealthSlider.gameObject.SetActive(true);

        StartCoroutine(ApplyKnockback(knockbackSource));

        if (health < 2)
        {
            animator.SetBool("isFizzy", true);
        }

        if (health <= 0)
        {
            StartCoroutine(DieRoutine());
            return;
        }
        canTakeDamage = false;
        StartCoroutine(Invincible(invincibilityDuration));
    }

    IEnumerator ApplyKnockback(Vector3 source)
    {
        isKnockedBack = true;
        Vector3 pushDir = (transform.position - source).normalized;
        enemyRb.linearVelocity = Vector3.zero; // Mevcut hızı sıfırla ki itme net olsun
        enemyRb.AddForce(pushDir * 7f, ForceMode.Impulse);

        yield return new WaitForSeconds(0.2f); // 0.2 saniye boyunca hareketi engelle
        isKnockedBack = false;
    }

    IEnumerator HitFlashRoutine()
    {
        // PBR Polyart canavarları genelde SkinnedMeshRenderer kullanır
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        // Orijinal renkleri aklımızda tutalım
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
            renderers[i].material.color = Color.white;
        }

        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = originalColors[i]; // Eskisine dön
        }
    }

    IEnumerator DieRoutine()
    {
        canTakeDamage = false; // Artık hasar almasın
        animator.SetTrigger("Die");

        // Hareketi tamamen keselim
        speed = 0;
        enemyRb.linearVelocity = Vector3.zero;

        // Collider'ı kapat ki içinden geçebilelim
        GetComponent<Collider>().enabled = false;

        // WaveManager sayacını düşür
        WaveManager.activeEnemyCount--;

        // Objeyi kinematic yapmadan önce hızın sıfırlandığından emin olduk
        enemyRb.isKinematic = true;

        yield return new WaitForSeconds(2.0f); // Ölüm animasyonunun bitişini bekle
        Destroy(gameObject);
    }

    IEnumerator Invincible(float duration)
    {
        yield return new WaitForSeconds(duration);
        canTakeDamage = true;
    }
}
