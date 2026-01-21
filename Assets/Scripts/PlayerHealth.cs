using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class PlayerHealth : MonoBehaviour
{
    public int playerHealth;
    public static bool gameOver = false;
    public float invincibltyTime = 5;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(playerHealth <= 0)
        {
            Debug.Log("Game Over!");
            gameOver = true;
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
