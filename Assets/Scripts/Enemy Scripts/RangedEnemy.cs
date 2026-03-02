using PixPlays.ElementalVFX;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RangedEnemy : EnemyBase
{
    public enum WizardType { Fire, Ice }

    [Header("Wizard Identity")]
    public WizardType wizardType;
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Combat Distances")]
    public float attackRange = 10f;
    public float stopDistance = 6f;
    public float fleeDistance = 3f;

    [Header("VFX Timing")]
    public float projectileArrivalTime = 2f;

    [Header("Movement Settings")]
    public float strafeSpeed = 2f;
    public float fireRate = 2f;

    [Header("Timing")]
    public float spellDelay = 1.5f;

    private float nextFireTime;
    private float strafeDirection = 1f;
    private float strafeChangeTimer;
    private float defaultAgentSpeed;

    private static readonly int SpeedHash = Animator.StringToHash("speed_f");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IsStrafingHash = Animator.StringToHash("isStrafing");

    protected override void Start()
    {
        base.Start();

        if (agent != null)
        {
            defaultAgentSpeed = agent.speed;
            agent.stoppingDistance = stopDistance;
            agent.updateRotation = false;
        }
        strafeDirection = Random.value > 0.5f ? 1f : -1f;
        strafeChangeTimer = Random.Range(1f, 2f);
    }

    void Update()
    {
        if (player == null || health <= 0 || !GameManager.isGameActive || isKnockedBack) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);

        RotateTowardsPlayer();

        bool hasLineOfSight = !IsObstacleInWay();

        if (!hasLineOfSight && distance <= attackRange)
        {
            agent.isStopped = false;
            agent.speed = defaultAgentSpeed;
            agent.updateRotation = true;
            agent.SetDestination(player.transform.position);
            animator.SetFloat(SpeedHash, agent.velocity.magnitude);
            animator.SetBool(IsStrafingHash, false);
        }
        else
        {
            HandleSmartMovement(distance);
        }

        if (distance <= attackRange && Time.time >= nextFireTime && hasLineOfSight)
        {
            Attack();
        }
    }

    void HandleSmartMovement(float distance)
    {
        if (distance > stopDistance + 1f)
        {
            agent.updateRotation = true;
            agent.isStopped = false;
            agent.speed = defaultAgentSpeed;
            agent.SetDestination(player.transform.position);
            animator.SetFloat(SpeedHash, agent.velocity.magnitude);
            animator.SetBool(IsStrafingHash, false);
        }
        else if (distance < fleeDistance)
        {
            Vector3 fleeDir = (transform.position - player.transform.position).normalized;
            Vector3 fleeTarget = transform.position + fleeDir * 5f;

            if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.updateRotation = false;
                agent.speed = defaultAgentSpeed;
                agent.SetDestination(hit.position);
            }

            animator.SetFloat(SpeedHash, agent.velocity.magnitude);
            animator.SetBool(IsStrafingHash, false);
        }
        else
        {
            // İdeal mesafe: strafe
            strafeChangeTimer -= Time.deltaTime;
            if (strafeChangeTimer <= 0)
            {
                strafeDirection *= -1;
                strafeChangeTimer = Random.Range(1.5f, 3.5f);
            }

            Vector3 strafeTarget = transform.position + transform.right * strafeDirection * 3f;

            if (NavMesh.SamplePosition(strafeTarget, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.updateRotation = false;
                agent.speed = strafeSpeed;
                agent.SetDestination(navHit.position);
            }
            else
            {
                agent.isStopped = true;
                strafeDirection *= -1;
            }

            animator.SetFloat(SpeedHash, agent.velocity.magnitude);
            animator.SetBool(IsStrafingHash, true);
        }
    }

    void RotateTowardsPlayer()
    {
        Vector3 lookPos = player.transform.position - transform.position;
        lookPos.y = 0;
        if (lookPos != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookPos);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        }
    }

    void Attack()
    {
        if (isKnockedBack) return;


        float currentFireRate = fireRate;
        nextFireTime = Time.time + currentFireRate;
        animator.SetTrigger(AttackHash);
        StartCoroutine(DelayedLaunch(spellDelay));
    }

    IEnumerator DelayedLaunch(float delay)
    {
        yield return new WaitForSeconds(delay);
        LaunchSpell();
    }

    public void LaunchSpell()
    {
        if (health <= 0 || isKnockedBack) return;   

        GameObject spellObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        ProjectileVfx vfx = spellObj.GetComponent<ProjectileVfx>();

        if (vfx != null)
        {
            // Tahmin bir kez yapılıp hem VFX'e hem hasar sistemine gönderiliyor
            Vector3 predictedTarget = CalculatePredictedPosition(projectileArrivalTime);

            VfxData data = new VfxData(firePoint, predictedTarget, projectileArrivalTime, 0f);
            vfx.Play(data);

            StartCoroutine(DealDamageOnArrival(projectileArrivalTime, predictedTarget));
            Destroy(spellObj, projectileArrivalTime + 2f);
        }
    }

    IEnumerator DealDamageOnArrival(float delay, Vector3 targetedPosition)
    {
        yield return new WaitForSeconds(delay);

        if (player == null) yield break;

        float distancePlayerToImpact = Vector3.Distance(player.transform.position, targetedPosition);
        if (distancePlayerToImpact > 1.5f) yield break;

        PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
        if (pHealth == null) yield break;

        switch (wizardType)
        {
            case WizardType.Fire:
                pHealth.TakeDamage(1);
                break;
            case WizardType.Ice:
                pHealth.TakeDamage(1);
                // pHealth.ApplySlow(0.4f, 2.5f); // ApplySlow hazır olduğunda aç
                break;
        }
    }

    Vector3 CalculatePredictedPosition(float timeToArrive)
    {
        Rigidbody pRb = player.GetComponent<Rigidbody>();
        Vector3 currentPos = player.transform.position;

        if (pRb != null && pRb.linearVelocity.magnitude > 0.1f)
        {
            currentPos += pRb.linearVelocity * timeToArrive;
        }

        return currentPos;
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