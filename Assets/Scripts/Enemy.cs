using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Net;


public class Enemy : MonoBehaviour
{
    public float speed = 4.0f;
    public int health = 3;
    public bool canTakeDamage = true;
    public float invincibilityDuration = 1.0f;

    private GameObject player;
    private Rigidbody enemyRb;

    void Start()
    {
        enemyRb = GetComponent<Rigidbody>();
        player = GameObject.FindGameObjectWithTag("Player");
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
    }

    void TakeDamage(int damage)
    {
        health -= damage;
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
