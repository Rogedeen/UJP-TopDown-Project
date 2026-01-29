using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;

public enum PowerUpType
{
    Health,
    OrbitSpeed,
    MovementSpeed,
    DamageBoost
}
public class PowerUp : MonoBehaviour
{
    public PowerUpType type;

    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private Weapon weapon;
    private OrbitWeapon orbitWeapon;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerController = other.GetComponent<PlayerController>();
            playerHealth = other.GetComponent<PlayerHealth>();

            weapon = other.GetComponentInChildren<Weapon>();
            orbitWeapon = other.GetComponentInChildren<OrbitWeapon>();

            ApplyPowerUp(type);
            Destroy(gameObject);
        }
    }

    public void ApplyPowerUp(PowerUpType type) 
    {
        switch (type)
        {
            case PowerUpType.Health:
                if(playerHealth.playerHealth < 5)
                   playerHealth.playerHealth++;
                break;

            case PowerUpType.OrbitSpeed:
                orbitWeapon.rotationSpeed += 120.0f;
                break;

            case PowerUpType.MovementSpeed:
                playerController.speed += 0.5f;
                break;

            case PowerUpType.DamageBoost:
                weapon.damage += 2;
                orbitWeapon.damage += 2;
                break;

            default:
                break;
        }

    }
}
