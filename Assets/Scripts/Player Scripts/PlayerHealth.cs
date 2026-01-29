using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class PlayerHealth : MonoBehaviour
{
    public int playerHealth;
    public float invincibltyTime = 1;
    public GameManager gameManager;

    private bool isInvincible = false;
    void Start()
    {
        gameManager = GameObject.Find("Game Manager").GetComponent<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if(playerHealth <= 0)
        {
            Debug.Log("Game Over!");
            gameManager.GameOver();
        }

    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") && !isInvincible)
        {
            Debug.Log("Player collided with an enemy");
            playerHealth--;
            Debug.Log("Player health is now reduced to: " + playerHealth);
            StartCoroutine(Invincible());
        }
    }

    IEnumerator Invincible()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibltyTime);
        isInvincible = false;
    }
}
