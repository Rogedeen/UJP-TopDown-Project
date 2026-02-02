using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int playerHealth = 5;
    public float invincibltyTime = 1f;
    public GameManager gameManager;

    private bool isInvincible = false;

    void Start()
    {
        GameObject gmObj = GameObject.Find("Game Manager");
        if (gmObj != null) gameManager = gmObj.GetComponent<GameManager>();
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
        Debug.Log("Oyuncu hasar aldı! Kalan Can: " + playerHealth);

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