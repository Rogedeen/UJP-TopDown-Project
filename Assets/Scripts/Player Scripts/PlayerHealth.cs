using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int playerHealth = 5;
    public int maxPlayerHealth = 5;
    public float invincibilityTime = 1f;

    [SerializeField] private GameManager gameManager;
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
            TakeDamage(1);
        }
    }

    IEnumerator InvincibleRoutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityTime);
        isInvincible = false;
    }
}