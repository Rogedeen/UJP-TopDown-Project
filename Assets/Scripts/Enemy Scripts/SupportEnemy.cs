using UnityEngine;
using System.Collections;

public class SupportEnemy : EnemyBase
{
    [Header("Aura Management")]
    public GameObject healingAuraPrefab; // wind aura prefabın
    public float auraDuration = 4f;      // Aura ne kadar süre açık kalacak
    public float auraCooldown = 10f;     // Bir sonraki aura için bekleme süresi
    public float checkAllyRange = 8f;    // Yaralı müttefiki ne mesafeden fark etsin?

    [Header("Behavior")]
    public float fleeDistance = 10f;     // Oyuncu gelirse topuklama mesafesi
    public float moveSpeed = 4f;

    private float nextAuraReadyTime;
    private bool isAuraActive = false;

    private static readonly int SpeedHash = Animator.StringToHash("speed_f");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    protected override void Start()
    {
        base.Start();
        if (agent != null) agent.speed = moveSpeed;
    }

    void Update()
    {
        if (player == null || health <= 0 || isKnockedBack) return;

        HandleSupportBehavior();

        // Aurayı tetikleme kontrolü
        if (Time.time >= nextAuraReadyTime && !isAuraActive && HasInjuredAllyInRange())
        {
            StartCoroutine(AuraCycle());
        }
    }

    void HandleSupportBehavior()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // ÖNCELİK 1: KAÇMAK (Seni hiç iplemiyor, sadece kaçıyor)
        if (distToPlayer < fleeDistance)
        {
            Vector3 fleeDir = (transform.position - player.transform.position).normalized;
            agent.isStopped = false;
            agent.SetDestination(transform.position + fleeDir * 6f);
        }
        // ÖNCELİK 2: YARALIYA GİTMEK
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
                // Kimse yaralı değilse ve sen de yakın değilsen durup takılsın
                agent.isStopped = true;
            }
        }

        animator.SetFloat(SpeedHash, agent.velocity.magnitude);
        RotateTowards(player.transform.position); // Seni takip etmese de nerede olduğunu bilmek için sana baksın
    }

    IEnumerator AuraCycle()
    {
        isAuraActive = true;
        animator.SetTrigger(AttackHash);

        if (healingAuraPrefab != null)
        {
            GameObject aura = Instantiate(healingAuraPrefab, transform.position, Quaternion.identity);
            aura.transform.SetParent(transform); // Büyücüyle beraber gitsin
            aura.transform.localPosition = Vector3.zero;
            Destroy(aura, auraDuration);
        }

        yield return new WaitForSeconds(auraDuration);

        isAuraActive = false;
        nextAuraReadyTime = Time.time + auraCooldown;
    }

    bool HasInjuredAllyInRange()
    {
        EnemyBase[] allies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (var ally in allies)
        {
            if (ally == this || ally.health <= 0) continue;
            if (ally.health < ally.maxHealth && Vector3.Distance(transform.position, ally.transform.position) <= checkAllyRange)
                return true;
        }
        return false;
    }

    EnemyBase FindMostHurtAlly()
    {
        EnemyBase[] allies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        EnemyBase worst = null;
        int minHealth = int.MaxValue;

        foreach (var ally in allies)
        {
            if (ally == this || ally.health <= 0 || ally.health >= ally.maxHealth) continue;
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