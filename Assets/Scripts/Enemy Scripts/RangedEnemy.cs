using System.Collections;
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
        base.Start();
        if (agent != null)
        {
            agent.speed = speed;
            agent.stoppingDistance = stoppingDistance;
            agent.updateRotation = false;
        }
    }

    void Update()
    {
        if (player == null || health <= 0 || !GameManager.isGameActive || isKnockedBack) return;

        if (agent.isActiveAndEnabled)
        {
            agent.SetDestination(player.transform.position);

            float distance = Vector3.Distance(transform.position, player.transform.position);
            animator.SetFloat("speed_f", agent.velocity.magnitude);

            // Oyuncuya bak
            Vector3 lookPos = player.transform.position - transform.position;
            lookPos.y = 0;
            if (lookPos != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(lookPos);

            // Ates etme mantigi
            if (distance <= attackRange && Time.time >= nextFireTime)
            {
                if (!IsObstacleInWay())
                {
                    StartCoroutine(Attack());
                    nextFireTime = Time.time + fireRate;
                }
            }
        }
    }
    IEnumerator Attack()
    {
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");

        StartCoroutine(SpawnProjectileDelayed(1.0f));
        yield return new WaitForSeconds(0);
    }
    IEnumerator SpawnProjectileDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Düşman bu sürede ölmüş olabilir, kontrol şart
        if (health <= 0 || !gameObject.activeInHierarchy) yield break;

        if (projectilePrefab != null && firePoint != null)
        {
            GameObject spell = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            Vector3 targetDirection = (player.transform.position - firePoint.position).normalized;
            targetDirection.y = 0;
            spell.transform.forward = targetDirection;
        }
    }

    bool IsObstacleInWay()
    {
        Vector3 dir = (player.transform.position - firePoint.position).normalized;
        if (Physics.Raycast(firePoint.position, dir, out RaycastHit hit, attackRange))
        {
            if (hit.collider.CompareTag("Barrier") || hit.collider.CompareTag("Barrel")) return true;
        }
        return false;
    }
}