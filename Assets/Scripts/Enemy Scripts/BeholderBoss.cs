using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BeholderBoss : EnemyBase
{
    public enum BeholderState { Spawning, Chasing, ChargingUp, BeamSweep, Dizzy, Dead }

    [Header("Beholder Settings")]
    public string bossName = "The Eye of Terror";
    public BeholderState currentState = BeholderState.Spawning;
    
    [Header("Movement Settings")]
    public float moveSpeed = 3.5f;
    [Tooltip("Oyuncuya ne kadar yaklaşınca durup ateş etmeye hazırlansın?")]
    public float stoppingDistance = 6f;

    [Header("Beam Attack Settings")]
    public Transform eyeFirePoint; 
    public GameObject beamVfxPrefab; 
    
    public float beamDamageFrequency = 0.2f;
    public int beamDamage = 1;
    
    [Tooltip("Lazer atışından önceki odaklanma (Charge) süresi.")]
    public float chargeUpDuration = 2f;
    [Tooltip("Lazer saldırısının sahnede kalacağı toplam saniye.")]
    public float sweepDuration = 4f;
    public float sweepRotationSpeed = 45f;
    
    [Header("Dizzy Settings")]
    [Tooltip("Saldırı bittikten sonra sersemleyip (Dizzy) hasara açık kalacağı süre.")]
    public float dizzyDuration = 4f;
    
    private GameObject currentBeam;
    private float damageTimer = 0f;

    // Animator Hashleri (String aramalarından kurtulmak için)
    private static readonly int SpeedHash = Animator.StringToHash("speed_f");
    private static readonly int AttackStartHash = Animator.StringToHash("AttackStart");
    private static readonly int IsAttackingLoopHash = Animator.StringToHash("isAttacking");
    private static readonly int IsDizzyHash = Animator.StringToHash("isDizzy");

    protected override void Start()
    {
        base.Start();
        dealsContactDamage = false; 
        
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = stoppingDistance;
        }

        if (BossUIManager.Instance != null)
        {
            BossUIManager.Instance.ShowBoss(bossName, maxHealth);
        }

        StartCoroutine(BossRoutine());
    }

    private void Update()
    {
        // Chasing (Kovalama) state'inde oyuncuyu takip et
        if (currentState == BeholderState.Chasing && player != null && health > 0 && !isKnockedBack)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            
            if (dist > stoppingDistance)
            {
                agent.isStopped = false;
                agent.SetDestination(player.transform.position);
            }
            else
            {
                agent.isStopped = true;
                // Hedefe yaklaştıysa lazer için yüzünü oyuncuya dön (Y ekseninde)
                Vector3 lookPos = player.transform.position - transform.position;
                lookPos.y = 0;
                if(lookPos.sqrMagnitude > 0.1f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookPos);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
                }
            }

            if (animator != null)
                animator.SetFloat(SpeedHash, agent.velocity.magnitude);
        }
        else if (agent != null && agent.isActiveAndEnabled)
        {
            // Diğer tüm statelerde (Charge, Lazer, Dizzy) boss olduğu yerde durur
            agent.isStopped = true;
            if (animator != null)
                animator.SetFloat(SpeedHash, 0f);

            // Şarj olurken oyuncuyu izlemeye (hedef almaya) devam et
            if (currentState == BeholderState.ChargingUp && player != null)
            {
                Vector3 lookPos = player.transform.position - transform.position;
                lookPos.y = 0;
                if (lookPos.sqrMagnitude > 0.1f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookPos);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
                }
            }
        }
    }

    public override void TakeDamage(int damage, Vector3 knockbackSource, float knockbackMultiplier = 1f)
    {
        // Boss geri itilmez (knockbackMultiplier = 0f)
        base.TakeDamage(damage, knockbackSource, 0f); 
        
        if (health > 0 && BossUIManager.Instance != null)
            BossUIManager.Instance.UpdateHealth(health);
    }

    protected override IEnumerator DieRoutine()
    {
        currentState = BeholderState.Dead;
        
        if (currentBeam != null) Destroy(currentBeam);
        if (BossUIManager.Instance != null) BossUIManager.Instance.HideBoss();
        
        yield return StartCoroutine(base.DieRoutine());
    }

    private IEnumerator BossRoutine()
    {
        yield return new WaitForSeconds(2f); // Spawn olma payı
        
        while (health > 0)
        {
            // 1. CHASE (Yaklaşma Evresi)
            currentState = BeholderState.Chasing;
            
            // Rastgele bir kovalamaca süresi yerine, oyuncuya yaklaşana kadar inatla kovala!
            float chaseTimer = 0f;
            while (health > 0 && currentState == BeholderState.Chasing)
            {
                chaseTimer += Time.deltaTime;
                float dist = 999f;
                if (player != null) dist = Vector3.Distance(transform.position, player.transform.position);

                // En az 2 saniye kovalasın. Oyuncuya yeterince yaklaşmışsa saldırıya geç!
                if (chaseTimer > 2f && dist <= stoppingDistance + 5f)
                {
                    break;
                }
                
                // Oyuncu çok iyi kaçıyorsa en fazla 12 saniye sonra yorulup olduğu yerden lazer atsın.
                if (chaseTimer > 12f)
                {
                    break;
                }
                
                yield return null;
            }
            
            if (currentState == BeholderState.Dead) break;

            // 2. CHARGE UP (Lazer Öncesi Şarj Animasyonu)
            currentState = BeholderState.ChargingUp;
            SafeSetTrigger(animator, AttackStartHash);
            
            // Update fonksiyonu şarj süresince zaten oyuncuyu pürüzsüzce takip edecek
            yield return new WaitForSeconds(chargeUpDuration);

            if (currentState == BeholderState.Dead) break;

            // 3. BEAM SWEEP (Lazer Atışı ve Sürekli Animasyon)
            yield return StartCoroutine(BeamSweepAttack());
            
            if (currentState == BeholderState.Dead) break;

            // 4. DIZZY (Sersemleme / Yorulma Animasyonu)
            currentState = BeholderState.Dizzy;
            SafeSetBool(animator, IsDizzyHash, true);
            
            yield return new WaitForSeconds(dizzyDuration);
            
            SafeSetBool(animator, IsDizzyHash, false);
        }
    }

    private IEnumerator BeamSweepAttack()
    {
        currentState = BeholderState.BeamSweep;
        // Ateş etme (tutma) animasyonunu aktif et
        SafeSetBool(animator, IsAttackingLoopHash, true);

        if (beamVfxPrefab != null && eyeFirePoint != null)
        {
            currentBeam = Instantiate(beamVfxPrefab, eyeFirePoint.position, eyeFirePoint.rotation, eyeFirePoint);
        }

        float elapsed = 0f;
        damageTimer = 0f;

        while (elapsed < sweepDuration)
        {
            elapsed += Time.deltaTime;
            
            // Lazer atarken oyuncuyu amansızca takip et!
            // sweepRotationSpeed burada takip hızı (zorluk) olarak kullanılır.
            if (player != null)
            {
                Vector3 lookPos = player.transform.position - transform.position;
                lookPos.y = 0;
                if (lookPos.sqrMagnitude > 0.1f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookPos);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, sweepRotationSpeed * Time.deltaTime);
                }
            }

            HandleBeamDamage();

            yield return null; 
        }

        // Lazer bitti
        if (currentBeam != null)
        {
            Destroy(currentBeam);
        }

        // Ateş etme loop'unu kapat
        SafeSetBool(animator, IsAttackingLoopHash, false);
    }

    private void HandleBeamDamage()
    {
        if (eyeFirePoint == null) return;

        damageTimer += Time.deltaTime;
        bool canDealDamage = false;

        // Saniyede X kere hasar tick atmasını sağla
        if (damageTimer >= beamDamageFrequency)
        {
            canDealDamage = true;
            damageTimer = 0f;
        }

        // Işını gönder ve çarptığı BÜTÜN objeleri al (SphereCastAll)
        // Yaklaştıran mermi mantığı için çarptığı objeleri mesafelerine göre yakından-uzağa sırala
        RaycastHit[] hits = Physics.SphereCastAll(eyeFirePoint.position, 1.5f, eyeFirePoint.forward, 30f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.isTrigger || hit.collider.CompareTag("Enemy") || hit.collider.gameObject == gameObject) 
                continue; // Kendini, diğer yaratıkları ve triggerları(hayalet alanları) yoksay

            if (hit.collider.CompareTag("Barrier") || hit.collider.gameObject.layer == LayerMask.NameToLayer("Environment"))
            {
                break; // Barikata (kapılara vs) veya duvara çarptı. Işının GÜCÜ burada KIRILDI, daha öteye gidemez!
            }

            if (canDealDamage)
            {
                if (hit.collider.CompareTag("Player"))
                {
                    PlayerController pc = hit.collider.GetComponent<PlayerController>();
                    // İP ATLAMA: Oyuncu dash atıyorsa içinden geçer (hasar almaz)
                    if (pc != null && !pc.IsDashing)
                    {
                        PlayerHealth ph = hit.collider.GetComponent<PlayerHealth>();
                        if (ph != null) ph.TakeDamage(beamDamage);
                    }
                }
                else if (hit.collider.CompareTag("Barrel"))
                {
                    ExplosiveBarrel barrel = hit.collider.GetComponent<ExplosiveBarrel>();
                    // Lazeri yiyen varil patlar!
                    if (barrel != null) barrel.TakeDamage(beamDamage, transform.position, 1f);
                }
            }
        }
    }
}
