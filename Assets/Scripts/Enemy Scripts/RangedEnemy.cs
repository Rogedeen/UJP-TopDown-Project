using UnityEngine;

public class RangedEnemy : EnemyBase
{
    [Header("Movement & Combat")]
    public float speed = 3f;
    public float stoppingDistance = 6f;
    public float attackRange = 10f;
    public float fireRate = 2f;

    [Header("Ranged References")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    private float nextFireTime;

    protected override void Start()
    {
        base.Start(); // base.Start oyuncuyu bulacak
    }

    void Update()
    {
        if (player == null || health <= 0 || !GameManager.isGameActive) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= attackRange && Time.time >= nextFireTime)
        {
            if (!IsObstacleInWay())
            {
                Attack();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    void FixedUpdate()
    {
        if (player == null || isKnockedBack || health <= 0) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        transform.LookAt(new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z));

        if (distance > stoppingDistance)
        {
            Vector3 dir = (player.transform.position - transform.position).normalized;
            dir.y = 0;
            enemyRb.linearVelocity = new Vector3(dir.x * speed, enemyRb.linearVelocity.y, dir.z * speed);
            animator.SetFloat("speed_f", speed);
        }
        else
        {
            enemyRb.linearVelocity = new Vector3(0, enemyRb.linearVelocity.y, 0);
            animator.SetFloat("speed_f", 0);
        }
    }

    void Attack()
    {
        animator.SetTrigger("Attack");

        if (projectilePrefab != null && firePoint != null)
        {
            // 1. Mermiyi her zamanki gibi doğur
            GameObject spell = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

            // 2. Merminin gideceği yönü hesapla (Oyuncu - Mermi)
            Vector3 targetDirection = (player.transform.position - firePoint.position).normalized;

            // 3. Merminin havaya veya yere gitmesini engellemek için Y eksenini sıfırla
            targetDirection.y = 0;

            // 4. Merminin önünü (Z eksenini) zorla bu yöne çevir
            spell.transform.forward = targetDirection;
        }
    }

    bool IsObstacleInWay()
    {
        Vector3 dir = (player.transform.position - firePoint.position).normalized;
        if (Physics.Raycast(firePoint.position, dir, out RaycastHit hit, attackRange))
        {
            if (hit.collider.CompareTag("Barrier")) return true;
        }
        return false;
    }
}