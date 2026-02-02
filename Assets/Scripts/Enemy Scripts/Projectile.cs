using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 1;
    public float lifeTime = 4f;

    [Header("Effects")]
    public GameObject explosionPrefab;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * speed;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (explosionPrefab != null)
        {
            GameObject expo = Instantiate(explosionPrefab, transform.position, transform.rotation);
            Destroy(expo, 2f);
        }

        if (other.CompareTag("Player"))
        {
            PlayerHealth pHealth = other.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                pHealth.TakeDamage(damage); 
            }
            Destroy(gameObject);
        }
        else if (other.CompareTag("Barrier"))
        {
            Destroy(gameObject);
        }
    }
}