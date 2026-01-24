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

    void Start()
    {
        weapon = GameObject.Find("Weapon").GetComponent<Weapon>();
        orbitWeapon = GameObject.Find("Weapon").GetComponent<OrbitWeapon>();
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player")) 
        {
            playerController = other.gameObject.GetComponent<PlayerController>();
            playerHealth = other.gameObject.GetComponent<PlayerHealth>();
            ApplyPowerUp(type);
            Destroy(gameObject);
        }
    }

    void ApplyPowerUp(PowerUpType type) 
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
                break;

            default:
                break;
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
