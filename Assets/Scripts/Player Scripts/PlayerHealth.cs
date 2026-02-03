using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int playerHealth = 5;
    public float invincibltyTime = 1f;
    public GameManager gameManager;
    public Slider playerHealthBar;

    private bool isInvincible = false;

    void Start()
    {
        GameObject gmObj = GameObject.Find("Game Manager");
        if (gmObj != null) gameManager = gmObj.GetComponent<GameManager>();

        if (playerHealthBar != null)
        {
            playerHealthBar.maxValue = playerHealth;
            playerHealthBar.value = playerHealth;
            playerHealthBar.gameObject.SetActive(false);
        }

    }

    void Update()
    {
        if (playerHealth <= 0)
        {
            gameManager.GameOver();
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return;

        playerHealth -= damage;
        if (playerHealthBar != null)
        {
            playerHealthBar.value = playerHealth;
            playerHealthBar.gameObject.SetActive(true);
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
        yield return new WaitForSeconds(invincibltyTime);
        isInvincible = false;
    }
}