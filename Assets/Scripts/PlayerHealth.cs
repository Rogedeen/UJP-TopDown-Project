using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class PlayerHealth : MonoBehaviour
{
    public int playerHealth;
    public float invincibltyTime = 5;
    public GameManager gameManager;
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
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Player collided with an enemy");
            playerHealth--;
            Debug.Log("Player health is now reduced to: " + playerHealth);
            StartCoroutine(Invincible(invincibltyTime));
        }
    }

    IEnumerator Invincible(float invincibltyTime)
    {
        yield return new WaitForSeconds(invincibltyTime);
    }
}
