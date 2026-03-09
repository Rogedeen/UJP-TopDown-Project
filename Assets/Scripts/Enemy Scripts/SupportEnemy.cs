using UnityEngine;
using System.Collections;

public class SupportEnemy : EnemyBase
{
    [Header("Aura Management")]
    public GameObject healingAuraPrefab;
    public float auraDuration = 4f;
    public float auraCooldown = 10f;
    public float checkAllyRange = 8f;

    [Header("Behavior")]
    public float fleeDistance = 10f;
    public float moveSpeed = 4f;

    private float nextAuraReadyTime;
    private bool isAuraActive = false;

    // Müttefik taramasını optimize et: Her frame yerine belirli aralıklarla
    private EnemyBase[] cachedAllies;
    private float allyCacheTimer;
    private const float ALLY_CACHE_INTERVAL = 0.5f;

    private static readonly int SpeedHash = Animator.StringToHash("speed_f");

    protected override void Start()
    {
        base.Start();
        if (agent != null) agent.speed = moveSpeed;
    }

    void Update()
    {
        if (player == null || health <= 0 || isKnockedBack) return;

        // Müttefik listesini belirli aralıklarla güncelle (performans optimizasyonu)
        allyCacheTimer -= Time.deltaTime;
        if (allyCacheTimer <= 0f)
        {
            cachedAllies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            allyCacheTimer = ALLY_CACHE_INTERVAL;
        }

        HandleSupportBehavior();

        if (Time.time >= nextAuraReadyTime && !isAuraActive && HasInjuredAllyInRange())
        {
            StartCoroutine(AuraCycle());
        }
    }

    void HandleSupportBehavior()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distToPlayer < fleeDistance)
        {
            Vector3 fleeDir = (transform.position - player.transform.position).normalized;
            agent.isStopped = false;
            agent.SetDestination(transform.position + fleeDir * 6f);
        }
        else
        {
            EnemyBase target = FindMostHurtAlly();
            if (target != null)
            {
                agent.isStopped = false;
                agent.SetDestination(target.transform.position);
            }
            else
            {
                agent.isStopped = true;
            }
        }

        animator.SetFloat(SpeedHash, agent.velocity.magnitude);
        RotateTowards(player.transform.position);
    }

    IEnumerator AuraCycle()
    {
        isAuraActive = true;
        animator.SetTrigger(AttackHash);

        if (healingAuraPrefab != null)
        {
            GameObject aura = Instantiate(healingAuraPrefab, transform.position, Quaternion.identity);
            aura.transform.SetParent(transform);
            aura.transform.localPosition = Vector3.zero;
            Destroy(aura, auraDuration);
        }

        yield return new WaitForSeconds(auraDuration);

        isAuraActive = false;
        nextAuraReadyTime = Time.time + auraCooldown;
    }

    bool HasInjuredAllyInRange()
    {
        if (cachedAllies == null) return false;

        foreach (var ally in cachedAllies)
        {
            if (ally == null || ally == this || ally.health <= 0) continue;
            if (ally.health < ally.maxHealth && Vector3.Distance(transform.position, ally.transform.position) <= checkAllyRange)
                return true;
        }
        return false;
    }

    EnemyBase FindMostHurtAlly()
    {
        if (cachedAllies == null) return null;

        EnemyBase worst = null;
        int minHealth = int.MaxValue;

        foreach (var ally in cachedAllies)
        {
            if (ally == null || ally == this || ally.health <= 0 || ally.health >= ally.maxHealth) continue;
            if (ally.health < minHealth)
            {
                minHealth = ally.health;
                worst = ally;
            }
        }
        return worst;
    }

    void RotateTowards(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
    }
}