using PixPlays.ElementalVFX;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RangedEnemy : EnemyBase
{
    public enum WizardType { Fire, Ice, Support }

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
    public float projectileSpeed = 18f;
    public float fireRate = 2f;

    // Support Wizard ayarları
    [Header("Support Settings")]
    public int healAmount = 2;              // İyileştireceği HP miktarı
    public float healRange = 10f;             // Müttefik arama yarıçapı
    public float supportFireRate = 3f;        // Support mermisi atma süresi

    private float nextFireTime;
    private float strafeDirection = 1f;
    private float strafeChangeTimer;

    // Animator parameter hash'leri — string yerine hash kullanmak performans açısından çok daha iyi.
    // Her frame'de "speed_f" string'ini aramak yerine önceden hesaplanmış int kullanıyoruz.
    private static readonly int SpeedHash = Animator.StringToHash("speed_f");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int IsStrafingHash = Animator.StringToHash("isStrafing");

    private float defaultAgentSpeed;

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

        // Görüş hattı var mı kontrol et
        bool hasLineOfSight = !IsObstacleInWay();

        if (!hasLineOfSight && distance <= attackRange)
        {
            // Görüş hattı yok ama menzil içindeyiz: engeli aşmaya çalış
            // Agent'ı serbest bırak ve oyuncuya doğru git
            agent.isStopped = false;
            agent.speed = defaultAgentSpeed;
            agent.SetDestination(player.transform.position);
            animator.SetFloat(SpeedHash, agent.velocity.magnitude);
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
        if (wizardType == WizardType.Support)
        {
            HandleSupportMovement(); // Ayrı bir metot
            return;
        }

        if (distance > stopDistance + 1f) // stopDistance'a yaklaşana kadar koş
        {
            agent.updateRotation = true;
            agent.isStopped = false;
            agent.speed = defaultAgentSpeed;
            agent.SetDestination(player.transform.position);
            animator.SetFloat(SpeedHash, agent.velocity.magnitude);
        }
        else if (distance < fleeDistance)
        {
            // Çok yakın: Geri kaç
            // Burada önemli bir şey: geri kaçarken de NavMesh kullanıyoruz.
            // Eski kod transform.position + fleeDir*2f hesaplıyordu ama bu kısa mesafe
            // engel kontrolü yapmıyordu. NavMesh.SamplePosition ile güvenli nokta buluyoruz.
            Vector3 fleeDir = (transform.position - player.transform.position).normalized;
            Vector3 fleeTarget = transform.position + fleeDir * 5f;

            // NavMesh üzerinde geçerli bir nokta ara (2f yarıçap içinde)
            if (NavMesh.SamplePosition(fleeTarget, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.updateRotation = false;
                agent.SetDestination(hit.position);
            }

            animator.SetFloat(SpeedHash, agent.velocity.magnitude);
            animator.SetBool(IsStrafingHash, false);
        }
        else
        {
            // İdeal mesafe: NavMesh uyumlu strafe
            // Eski kod transform.Translate kullanıyordu — bu NavMesh'i devre dışı bırakır
            // ve düşman duvarlardan geçebilir. Doğru yaklaşım: agent.SetDestination ile
            // strafe hedefini hesaplayıp NavMesh üzerinde yürütmek.

            strafeChangeTimer -= Time.deltaTime;
            if (strafeChangeTimer <= 0)
            {
                strafeDirection *= -1;
                strafeChangeTimer = Random.Range(1.5f, 3.5f);
            }

            // Düşmanın sağ vektörünü kullanarak yana hedef belirle
            Vector3 strafeTarget = transform.position + transform.right * strafeDirection * 3f;

            if (NavMesh.SamplePosition(strafeTarget, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.updateRotation = false;
                agent.SetDestination(navHit.position);
                // Hızı strafe hızıyla sınırla, yoksa tüm hızında koşar
                agent.speed = strafeSpeed;
            }
            else
            {
                // Geçerli strafe noktası yoksa dur ve yön değiştir
                agent.isStopped = true;
                strafeDirection *= -1;
            }

            animator.SetFloat(SpeedHash, agent.velocity.magnitude);
            animator.SetBool(IsStrafingHash, true); // Animator bunu bilmeli!
        }
    }

    void HandleSupportMovement()
    {
        // En yakın müttefiki bul, onun arkasına konumlan
        EnemyBase[] allies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        EnemyBase closestAlly = null;
        float closestDist = float.MaxValue;

        foreach (EnemyBase ally in allies)
        {
            if (ally == this || ally.health <= 0) continue;
            float d = Vector3.Distance(transform.position, ally.transform.position);
            if (d < closestDist) { closestDist = d; closestAlly = ally; }
        }

        if (closestAlly != null)
        {
            // Müttefikin oyuncudan uzak tarafına konumlan
            Vector3 behindAlly = closestAlly.transform.position +
                (closestAlly.transform.position - player.transform.position).normalized * 2f;

            if (NavMesh.SamplePosition(behindAlly, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }
        }
        else
        {
            // Müttefik yoksa normal flee mantığı: kaç
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < fleeDistance * 2f) // Support daha erken kaçsın
            {
                Vector3 fleeDir = (transform.position - player.transform.position).normalized;
                if (NavMesh.SamplePosition(transform.position + fleeDir * 5f,
                    out NavMeshHit fleeHit, 3f, NavMesh.AllAreas))
                    agent.SetDestination(fleeHit.position);
            }
        }

        animator.SetFloat(SpeedHash, agent.velocity.magnitude);
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

        float currentFireRate = wizardType == WizardType.Support ? supportFireRate : fireRate;
        nextFireTime = Time.time + currentFireRate;
        animator.SetTrigger(AttackHash);

        // Animator Event yerine coroutine ile zamanlama yapıyoruz.
        // spellDelay değeri animasyonun fırlatma karesine denk gelmeli.
        // Bunu bulmak için: animasyonun toplam süresi * fırlatma karesinin yüzdesi.
        // Örneğin animasyon 1 saniyeyse ve fırlatma ortada ise 0.5f.
        // Bunu deneme yanılmayla ayarlaman gerekiyor.
        StartCoroutine(DelayedLaunch(spellDelay));
    }

    [Header("Timing")]
    public float spellDelay = 1f; // Inspector'dan ayarlayabilirsin!

    IEnumerator DelayedLaunch(float delay)
    {
        LaunchSpell();
        yield return new WaitForSeconds(delay);
    }

    // Bu metot Unity Animator Event olarak çağrılır (animasyon ortasında).
    // Yani bu metodu direkt çağırma — Unity animasyon sistemi çağırır.
    public void LaunchSpell()
    {
        if (health <= 0 || isKnockedBack) return;
        if (wizardType == WizardType.Support) { LaunchSupportSpell(); return; }

        GameObject spellObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        ProjectileVfx vfx = spellObj.GetComponent<ProjectileVfx>();

        if (vfx != null)
        {
            // Üçüncü constructor: (Transform source, Transform target, float duration, float radius)
            // duration = projectileArrivalTime: efekt bu kadar sürecek
            // radius = 0f: projectile için alan efekti yok
            VfxData data = new VfxData(firePoint, player.transform, projectileArrivalTime, 0f);

            vfx.Play(data);

            // Mermi hedefe projectileArrivalTime saniyede ulaşıyor
            // O an gelince hasar veriyoruz
            StartCoroutine(DealDamageOnArrival(projectileArrivalTime));

            // Efektin kendisini biraz sonra temizle
            // +2f: hit efektinin oynaması için ekstra süre
            Destroy(spellObj, projectileArrivalTime + 2f);
        }
    }

    IEnumerator DealDamageOnArrival(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (player == null) yield return null;

        // Oyuncu hâlâ menzil içinde mi? Çok uzağa kaçtıysa mermi ıskaladı say
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer > attackRange * 1.5f) yield return null;

        PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
        if (pHealth == null) yield return null;

        // Wizard tipine göre efekt uygula
        switch (wizardType)
        {
            case WizardType.Fire:
                pHealth.TakeDamage(1);
                break;
            case WizardType.Ice:
                pHealth.TakeDamage(1);
                // pHealth.ApplySlow(0.4f, 2.5f); // Hazır olduğunda aç
                break;
        }
    }

    void LaunchSupportSpell()
    {
        // En yaralı müttefiki bul ve ona iyileştirme mermisi fırlat
        EnemyBase[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        EnemyBase mostHurt = null;
        int lowestHealth = int.MaxValue;

        foreach (EnemyBase enemy in allEnemies)
        {
            if (enemy == this) continue; // Kendini iyileştirmez
            if (enemy.health <= 0) continue; // Zaten ölüyor
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist > healRange) continue;

            if (enemy.health < lowestHealth)
            {
                lowestHealth = enemy.health;
                mostHurt = enemy;
            }
        }

        if (mostHurt == null) return; // Müttefik yoksa veya hepsi sağlıklıysa yapma

        GameObject spellObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile projectile = spellObj.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.type = Projectile.ProjectileType.Heal;
            projectile.value = healAmount;

            // Müttefikin pozisyonuna doğru fırlat
            Vector3 dir = (mostHurt.transform.position - firePoint.position).normalized;
            dir.y = 0;
            spellObj.transform.forward = dir;

            // Tag'i değiştir ki oyuncuya çarparsa hasar vermesin
            spellObj.tag = "EnemyProjectile"; // Bu tag'i Unity'de oluşturmalısın
        }
    }

    void SetupProjectileByWizardType(Projectile p)
    {
        switch (wizardType)
        {
            case WizardType.Fire:
                p.type = Projectile.ProjectileType.Damage;
                p.value = 1;
                // Fire mermisi biraz daha hızlı ve daha kısa ömürlü olabilir
                p.speed = projectileSpeed * 1.1f;
                break;

            case WizardType.Ice:
                p.type = Projectile.ProjectileType.Slow;
                p.value = 1;
                p.slowAmount = 0.4f;
                p.slowDuration = 2.5f;
                // Ice mermisi biraz daha yavaş ama uzun menzilli
                p.speed = projectileSpeed * 0.85f;
                p.lifeTime = 5f;
                break;
        }
    }

    Vector3 CalculatePredictiveDirection()
    {
        Rigidbody pRb = player.GetComponent<Rigidbody>();
        Vector3 targetPos = player.transform.position;

        if (pRb != null && pRb.linearVelocity.magnitude > 0.1f)
        {
            float dist = Vector3.Distance(firePoint.position, player.transform.position);
            float timeToReach = dist / projectileSpeed;
            targetPos += pRb.linearVelocity * timeToReach;
        }

        Vector3 dir = (targetPos - firePoint.position).normalized;
        dir.y = 0;
        return dir;
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

    // Strafe sırasında agent hızını normale döndürmek için
    // Start'ta agent.speed'i kaydetmeliyiz
    
}