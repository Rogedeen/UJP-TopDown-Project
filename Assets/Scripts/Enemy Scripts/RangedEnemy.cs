using PixPlays.ElementalVFX;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RangedEnemy : EnemyBase
{
    // ─── STATE MACHINE ───
    // Wizard'ın "şu an ne yapıyor?" sorusunun cevabı.
    // Her frame, önce hangi state'te olunması gerektiğine karar verilir,
    // sonra o state'in davranışı çalıştırılır.
    public enum WizardState
    {
        Chasing,        // Oyuncuya yaklaşıyor (çok uzakta veya görüş hattı yok)
        Strafing,       // İdeal mesafede sağa-sola kayıyor (ateş edebilir)
        Fleeing,        // Oyuncu çok yaklaştı, geri kaçıyor
        Repositioning   // Engel var, pozisyon değiştiriyor
    }

    [Header("State Machine")]
    [SerializeField] private WizardState currentState = WizardState.Chasing;

    public enum WizardType { Fire, Ice }

    [Header("Wizard Identity")]
    public WizardType wizardType;
    public GameObject projectilePrefab;
    public GameObject hitEffectPrefab;
    public Transform firePoint;

    [Header("Combat Distances")]
    public float attackRange = 10f;
    public float stopDistance = 6f;
    public float fleeDistance = 3f;

    [Header("VFX Timing")]
    public float projectileArrivalTime = 2f;
    public float projectilePhysicalSpeed = 15f;

    [Header("Movement Settings")]
    public float strafeSpeed = 2f;
    public float fireRate = 2f;

    [Header("Timing")]
    public float spellDelay = 1.5f;

    [Header("Damage Settings")]
    [SerializeField] private float impactHitRadius = 1.5f;

    private float nextFireTime;
    private float strafeDirection = 1f;
    private float strafeChangeTimer;
    private float defaultAgentSpeed;

    private static readonly int SpeedHash = Animator.StringToHash("speed_f");
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
        bool hasLineOfSight = !IsObstacleInWay();

        RotateTowardsPlayer();

        // ─── STATE GEÇİŞLERİ ───
        // Önce wizard'ın hangi durumda olması gerektiğine karar ver
        currentState = DetermineState(distance, hasLineOfSight);

        // ─── STATE DAVRANIŞLARI ───
        // Belirlenen duruma göre davran
        switch (currentState)
        {
            case WizardState.Chasing:
                ExecuteChasing();
                break;
            case WizardState.Strafing:
                ExecuteStrafing();
                break;
            case WizardState.Fleeing:
                ExecuteFleeing();
                break;
            case WizardState.Repositioning:
                ExecuteRepositioning();
                break;
        }

        // Saldırı kontrolü (herhangi bir state'te olabilir)
        if (distance <= attackRange && Time.time >= nextFireTime && hasLineOfSight)
        {
            Attack();
        }
    }

    /// <summary>
    /// Mesafe ve görüş hattına göre wizard'ın hangi state'te olması gerektiğini belirler.
    /// Bu, state machine'in "geçiş kuralları" bölümüdür.
    /// </summary>
    WizardState DetermineState(float distance, bool hasLineOfSight)
    {
        // Engel var ve aralıkta ise → pozisyon değiştir
        if (!hasLineOfSight && distance <= attackRange)
            return WizardState.Repositioning;

        // Çok uzakta → yaklaş
        if (distance > stopDistance + 1f)
            return WizardState.Chasing;

        // Çok yakında → kaç
        if (distance < fleeDistance)
            return WizardState.Fleeing;

        // İdeal mesafede → strafe yap
        return WizardState.Strafing;
    }

    // ─── STATE DAVRANIŞLARI ───
    // Her state'in "ne yapacağını" tanımlayan ayrı metodlar

    void ExecuteChasing()
    {
        agent.updateRotation = true;
        agent.isStopped = false;
        agent.speed = defaultAgentSpeed;
        agent.SetDestination(player.transform.position);
        animator.SetFloat(SpeedHash, agent.velocity.magnitude);
        animator.SetBool(IsStrafingHash, false);
    }

    void ExecuteStrafing()
    {
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

    void ExecuteFleeing()
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

    void ExecuteRepositioning()
    {
        agent.isStopped = false;
        agent.speed = defaultAgentSpeed;
        agent.updateRotation = true;
        agent.SetDestination(player.transform.position);
        animator.SetFloat(SpeedHash, agent.velocity.magnitude);
        animator.SetBool(IsStrafingHash, false);
    }

    // ─── SALDIRI & BÜYÜ SİSTEMİ ───

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

        nextFireTime = Time.time + fireRate;
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

        float distToPlayer = Vector3.Distance(firePoint.position, player.transform.position);
        float travelTimeToPlayer = distToPlayer / projectilePhysicalSpeed;

        Vector3 predictedPoint = CalculatePredictedPosition(travelTimeToPlayer);

        Vector3 fireDirection = (predictedPoint - firePoint.position).normalized;
        float maxDistance = 50f;
        Vector3 farAwayTarget = firePoint.position + fireDirection * maxDistance;
        float totalTravelTime = maxDistance / projectilePhysicalSpeed;

        GameObject spellObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        ProjectileVfx vfx = spellObj.GetComponent<ProjectileVfx>();

        if (vfx != null)
        {
            vfx._FlySpeed = totalTravelTime;
            VfxData data = new VfxData(firePoint, farAwayTarget, totalTravelTime, 0f);
            vfx.Play(data);

            StartCoroutine(DealDamageOnArrival(travelTimeToPlayer, predictedPoint, spellObj));
            Destroy(spellObj, totalTravelTime + 1f);
        }
    }

    IEnumerator DealDamageOnArrival(float delay, Vector3 targetedPosition, GameObject spellObj)
    {
        yield return new WaitForSeconds(delay);

        if (player == null || spellObj == null) yield break;

        float distancePlayerToImpact = Vector3.Distance(player.transform.position, targetedPosition);

        if (distancePlayerToImpact <= impactHitRadius)
        {
            PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
            PlayerController pController = player.GetComponent<PlayerController>();

            if (pHealth != null)
            {
                pHealth.TakeDamage(1);

                if (wizardType == WizardType.Ice && pController != null)
                {
                    pController.StartSlow(0.4f, 2.5f);
                }
            }

            if (hitEffectPrefab != null)
            {
                GameObject hitFx = Instantiate(hitEffectPrefab, player.transform.position, Quaternion.identity);
                Destroy(hitFx, 2f);
            }

            spellObj.SetActive(false);
            Destroy(spellObj, 0.1f);
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