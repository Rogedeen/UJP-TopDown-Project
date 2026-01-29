using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Net;
using UnityEngine.UI;


public class Enemy : MonoBehaviour
{
    public float speed = 4.0f;
    public int health = 3;
    public bool canTakeDamage = true;
    public float invincibilityDuration = 0.5f;
    public Slider enemyHealthSlider;

    private GameObject player;
    private Rigidbody enemyRb;

    void Start()
    {
        enemyRb = GetComponent<Rigidbody>();
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
        if (player != null)
        {
            Vector3 playerDir = (player.transform.position - transform.position).normalized;
            playerDir.y = 0;
            enemyRb.linearVelocity = new(playerDir.x * speed, enemyRb.linearVelocity.y, playerDir.z * speed);
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Weapon"))
        {
            if (canTakeDamage)
            {
                Weapon weapon = other.GetComponent<Weapon>();
                TakeDamage(weapon.damage);
            }
        }

        else if (other.gameObject.CompareTag("OrbitWeapon"))
        {
            if (canTakeDamage)
            {
                OrbitWeapon orbitWeapon = other.GetComponent<OrbitWeapon>();
                TakeDamage(orbitWeapon.damage);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        UpdateEnemyHealth();
        enemyHealthSlider.gameObject.SetActive(true);
        if (health <= 0)
        {
            Die();
            return;
        }
        canTakeDamage = false;
        StartCoroutine(Invincible(invincibilityDuration));
    }

    void Die()
    {
        //animasyon ses efekti vs ekleneceği zaman buraya ekleyeceğiz.
        WaveManager.activeEnemyCount--;
        Destroy(gameObject);       
    }

    IEnumerator Invincible(float duration)
    {
        yield return new WaitForSeconds(duration);
        canTakeDamage = true;
    }
}
