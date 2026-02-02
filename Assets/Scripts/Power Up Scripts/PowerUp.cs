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
    public float powerUpDuration = 5f;
    public float amount = 2.0f;
    public float floorLifeTime = 7f; 

    private Coroutine floorCoroutine; 
    private bool isCollected = false;

    void Start()
    {
        floorCoroutine = StartCoroutine(DestroyIfUncollected());
    }

    IEnumerator DestroyIfUncollected()
    {

        yield return new WaitForSeconds(floorLifeTime - 2f);

        // Buraya yanıp sönme animasyonu veya ses gelebilir

        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            isCollected = true;
            if (floorCoroutine != null) StopCoroutine(floorCoroutine);

            // Görseli ve collider'ı kapat ama objeyi henüz yok etme
            GetComponent<MeshRenderer>().enabled = false;
            GetComponent<Collider>().enabled = false;

            StartCoroutine(ApplyPowerUp(other.gameObject));
        }
    }

    IEnumerator ApplyPowerUp(GameObject player)
    {
        var pc = player.GetComponent<PlayerController>();
        var w = player.GetComponentInChildren<Weapon>();
        var ow = player.GetComponentInChildren<OrbitWeapon>();
        var ph = player.GetComponent<PlayerHealth>();

        // ETKİYİ VER (Multiplier = 1)
        ModifyStats(pc, w, ow, ph, 1);

        yield return new WaitForSeconds(powerUpDuration);

        // ETKİYİ GERİ AL (Multiplier = -1)
        ModifyStats(pc, w, ow, ph, -1);

        // Power up'ın işi bitti sahneden sil
        Destroy(gameObject);
    }

    private void ModifyStats(PlayerController pc, Weapon w, OrbitWeapon ow, PlayerHealth ph, float multiplier)
    {
        switch (type)
        {
            case PowerUpType.MovementSpeed:
                pc.speed += amount * multiplier;
                break;
            case PowerUpType.DamageBoost:
                if (w != null) w.damage += (int)(amount * multiplier);
                if (ow != null) ow.damage += (int)(amount * multiplier);
                break;
            case PowerUpType.OrbitCoolDownReduce:
                if (ow != null) ow.cooldown -= amount * multiplier;
                break;
            case PowerUpType.Health:
                if (multiplier > 0 && ph.playerHealth < 5) ph.playerHealth++;
                break;
        }
    }
}