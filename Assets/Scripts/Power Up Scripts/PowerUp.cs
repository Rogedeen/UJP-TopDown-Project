using System.Collections;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;

public enum PowerUpType
{
    Health,
    OrbitCoolDownReduce,
    MovementSpeed,
    DamageBoost
}
public class PowerUp : MonoBehaviour
{
    public PowerUpType type;
    public float powerUpDuration;
    public float amount;
    public float destroyAfterThisMuchSeconds;

    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private Weapon weapon;
    private OrbitWeapon orbitWeapon;

    public void Update()
    {
        StartCoroutine(DestroyUncollectedPowerUp());
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerController = other.GetComponent<PlayerController>();
            playerHealth = other.GetComponent<PlayerHealth>();

            weapon = other.GetComponentInChildren<Weapon>();
            orbitWeapon = other.GetComponentInChildren<OrbitWeapon>();

            StartCoroutine(ApplyPowerUp(type));
        }
    }

    IEnumerator ApplyPowerUp(PowerUpType type) 
    {
        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<Collider>().enabled = false;

        switch (type)
        {
            case PowerUpType.Health:
                if(playerHealth.playerHealth < 5)
                   playerHealth.playerHealth++;
                break;

            case PowerUpType.OrbitCoolDownReduce:
                amount = 2.0f;
                orbitWeapon.cooldown -= amount;
                break;

            case PowerUpType.MovementSpeed:
                amount = 2.0f;
                playerController.speed += amount;
                break;

            case PowerUpType.DamageBoost:
                amount = 2;
                weapon.damage += (int)amount;
                orbitWeapon.damage += (int)amount;
                break;

            default:
                break;
        }

        yield return new WaitForSeconds(powerUpDuration);

        switch (type)
        {
            case PowerUpType.MovementSpeed:

                playerController.speed -= amount;
                break;

            case PowerUpType.DamageBoost:

                weapon.damage -= (int)amount;
                orbitWeapon.damage -= (int)amount;
                break;

            default:
                break;
        }
        Destroy(gameObject);
    }

    IEnumerator DestroyUncollectedPowerUp() 
    {
        yield return new WaitForSeconds(destroyAfterThisMuchSeconds);
        //glare effect koyarız yanıp söner hesabı
        Destroy(gameObject);
    }




}
