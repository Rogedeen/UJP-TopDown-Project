using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// NEW INPUT SYSTEM NASIL ÇALIŞIYOR?
/// ─────────────────────────────────
/// Eski sistem: Input.GetKey("space") → her frame "space basılı mı?" diye soruyorduk
/// Yeni sistem: InputAction "Dash" → Space veya Gamepad B basıldığında BİZE HABER VERİYOR
/// 
/// Avantaj: Tuş atamasını kod değiştirmeden değiştirebilirsin (InputSystem_Actions dosyasından)
/// Gamepad desteği otomatik gelir, rebinding yapılabilir.
/// 
/// KULLANIM:
/// 1. InputActionAsset'i Inspector'dan ata (InputSystem_Actions dosyasını sürükle)
/// 2. Her action (Move, Attack, Dash, Skill) otomatik bulunur
/// 3. Action'lar OnEnable'da aktif, OnDisable'da deaktif edilir
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5.0f;
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;

    [Header("Energy (Charge) Settings")]
    public float maxEnergy = 100f;
    [SerializeField] private float energyRegenRate = 10f; // Saniyede dolan miktar
    [SerializeField] private float sprintEnergyCost = 20f; // Saniyede harcanan
    [SerializeField] private float dashEnergyCost = 30f; // Anlık harcanan
    [SerializeField] private float exhaustionDelay = 2f; // Enerji tamamen bitince beklenilen süre
    [SerializeField] private float regenDelay = 1f; // Enerji harcandıktan sonra dolmaya başlaması için bekleme

    // Enerji durumu (HUD için okunabilir)
    private float currentEnergy;
    private float exhaustionTimer = 0f;
    private float regenTimer = 0f;
    public float CurrentEnergy => currentEnergy;
    public float MaxEnergy => maxEnergy;

    public void RestoreEnergy(float amount)
    {
        currentEnergy += amount;
        if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;
    }

    /// <summary>
    /// StatUpgrade üzerinden enerji yenilenme hızını (pasif) artırmak için.
    /// </summary>
    public void IncreaseRegenRate(float amount)
    {
        energyRegenRate += amount;
    }

    [Header("Combo System Settings")]
    public int maxCombo = 3;
    public float comboResetTime = 1.0f; // 1 saniye içinde atak yapılmazsa kombo sıfırlanır
    [SerializeField] private float heavyFinisherKnockbackMultiplier = 3.0f;
    
    [Tooltip("Her kombo adımı için X, Y, Z dönme açıları (Örn: 0,0,0 Yatay ise, 0,0,90 Dikey olabilir).")]
    public Vector3[] comboSlashAngles = new Vector3[] {
        new Vector3(0, 0, 0),     // Combo 1 
        new Vector3(0, 0, 180),   // Combo 2 
        new Vector3(0, 0, -90),   // Combo 3 (Chop/Dikey)
        new Vector3(0, 0, 90)     // Combo 4 (Finisher vb.)
    };

    private int comboStep = 0;
    private float lastAttackTime = 0f;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Attack Settings")]
    public Weapon activeWeapon;
    public float hitRadius = 2.5f;
    public float hitOffset = 1.5f;

    [Header("VFX Settings")]
    public GameObject windVFXPrefab;
    public GameObject fireVFXPrefab;
    public Transform vfxSpawnPoint;
    public int damageUpgradeThreshold = 2;

    [Header("Night Settings")]
    [Tooltip("Gece olunca otomatik yanan karakter ışığı (Point Light)")]
    public Light nightLight;

    [Header("Sound Settings")]
    public AudioSource audioSource;
    public AudioClip[] whooshSounds;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    // Component referansları
    private Rigidbody playerRb;
    private Animator animator;
    private FollowPlayer camScript;
    private Camera mainCamera;
    private PlayerHealth playerHealth;

    // Input state
    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction dashAction;
    private InputAction skillAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private Vector2 moveInput;

    // Dash state
    private bool isDashing = false;
    private bool canDash = true;

    // Slow state
    private float originalSpeed;
    private Coroutine activeSlowCoroutine;

    // ─── PUSH MEKANİĞİ ───
    [Header("Push Action")]
    [Tooltip("Kapı iterken oyuncunun hızını ne kadar düşürecek (Örn: 0.3 = %30 hız)")]
    public float pushSpeedMultiplier = 0.3f;
    private bool isPushingBarrier = false;
    private float pushTimer = 0f;

    // Animator hash'ler
    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int MoveSpeedHash = Animator.StringToHash("moveSpeed");
    private static readonly int IsAttackingHash = Animator.StringToHash("isAttacking");
    private static readonly int TakeDamageHash = Animator.StringToHash("takeDamage");
    private static readonly int IsDashingHash = Animator.StringToHash("isDashing");
    private static readonly int IsSprintingHash = Animator.StringToHash("isSprinting");
    private static readonly int ComboStepHash = Animator.StringToHash("comboStep");
    private static readonly int IsPushingHash = Animator.StringToHash("isPushing");

    // OrbitWeapon referansı - Skill tuşu basıldığında tetiklenir
    private OrbitWeapon orbitWeapon;
    public OrbitWeapon ActiveOrbitWeapon => orbitWeapon;

    void Awake()
    {
        // Animator'ı en erken Awake'de al — OnEnable'daki callback'lerden ÖNCE hazır olmalı
        animator = GetComponent<Animator>();
        playerRb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();

        // Input action'ları bul
        var playerMap = inputActions.FindActionMap("Player");
        moveAction = playerMap.FindAction("Move");
        attackAction = playerMap.FindAction("Attack");
        dashAction = playerMap.FindAction("Dash");
        skillAction = playerMap.FindAction("Skill");
        lookAction = playerMap.FindAction("Look");
        sprintAction = playerMap.FindAction("Sprint");
    }

    void Start()
    {
        mainCamera = Camera.main;
        camScript = mainCamera.GetComponent<FollowPlayer>();
        originalSpeed = speed;
        currentEnergy = maxEnergy;

        // OrbitWeapon varsa bul
        orbitWeapon = GetComponentInChildren<OrbitWeapon>();

        // Başlangıçta gece ışığı durumunu kontrol et
        if (nightLight != null && DayNightManager.Instance != null && DayNightManager.Instance.timePhases != null)
        {
            int lastPhase = DayNightManager.Instance.timePhases.Length - 1;
            nightLight.enabled = (DayNightManager.Instance.CurrentPhaseIndex >= lastPhase);
        }

        // ─── PARAMETRE DOĞRULAMASI ───
        // Animator Controller'da hangi parametrelerin eksik olduğunu tespit et
        ValidateAnimatorParameters();
    }

    /// <summary>
    /// Animator Controller'daki parametreleri kontrol eder.
    /// Eksik parametreleri konsola yazdırarak sorunları hızlıca bulmamızı sağlar.
    /// </summary>
    void ValidateAnimatorParameters()
    {
        if (animator == null)
        {
            Debug.LogError("[PlayerController] Animator component bulunamadı!", gameObject);
            return;
        }

        // Kontrol edilecek parametreler ve isimleri
        var requiredParams = new (int hash, string name, AnimatorControllerParameterType type)[]
        {
            (HorizontalHash, "Horizontal", AnimatorControllerParameterType.Float),
            (VerticalHash, "Vertical", AnimatorControllerParameterType.Float),
            (MoveSpeedHash, "moveSpeed", AnimatorControllerParameterType.Float),
            (IsAttackingHash, "isAttacking", AnimatorControllerParameterType.Bool),
            (TakeDamageHash, "takeDamage", AnimatorControllerParameterType.Trigger),
            (IsDashingHash, "isDashing", AnimatorControllerParameterType.Bool),
            (IsSprintingHash, "isSprinting", AnimatorControllerParameterType.Bool),
            (ComboStepHash, "comboStep", AnimatorControllerParameterType.Int),
        };

        foreach (var (hash, name, type) in requiredParams)
        {
            bool found = false;
            foreach (var param in animator.parameters)
            {
                if (param.nameHash == hash)
                {
                    found = true;
                    if (param.type != type)
                    {
                        Debug.LogWarning($"[PlayerController] Parametre '{name}' var ama tipi yanlış! " +
                                         $"Beklenen: {type}, Bulunan: {param.type}", gameObject);
                    }
                    break;
                }
            }

            if (!found)
            {
                Debug.LogError($"[PlayerController] ❌ Animator'da '{name}' ({type}) parametresi YOK! " +
                               $"Animator Controller'a bu parametreyi ekle.", gameObject);
            }
        }
    }

    void OnEnable()
    {
        // Action'ları aktif et — bu olmadan input okunmaz!
        inputActions.Enable();

        // Event'lere abone ol
        attackAction.performed += OnAttack;
        dashAction.performed += OnDash;
        skillAction.performed += OnSkill;

        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        }
    }

    void OnDisable()
    {
        // Event aboneliklerini kaldır (memory leak önleme)
        attackAction.performed -= OnAttack;
        dashAction.performed -= OnDash;
        skillAction.performed -= OnSkill;

        if (DayNightManager.Instance != null)
        {
            DayNightManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }

        inputActions.Disable();
    }

    private void HandlePhaseChanged(int newPhaseIndex)
    {
        if (nightLight != null && DayNightManager.Instance != null && DayNightManager.Instance.timePhases != null)
        {
            // Eğer son faza (Genelde Gece'dir) geldiysek ışığı yak
            int lastPhase = DayNightManager.Instance.timePhases.Length - 1;
            nightLight.enabled = (newPhaseIndex == lastPhase);
        }
    }

    void FixedUpdate()
    {
        isPushingBarrier = (pushTimer > 0f);
        if (pushTimer > 0f) pushTimer -= Time.fixedDeltaTime;

        if (isDashing) return; // Dash sırasında normal hareket yok

        // Move action'dan Vector2 oku
        moveInput = moveAction.ReadValue<Vector2>();

        // ─── GHOST INPUT DÜZELTMESİ (Deadzone) ───
        // Bilgisayara takılı direksiyon veya sanal joystickler bazen mikroskobik
        // veya sabit hatalı input gönderebilir. Küçük inputları yoksay:
        if (moveInput.sqrMagnitude < 0.05f)
        {
            moveInput = Vector2.zero;
        }

        Vector3 moveDirection = new(moveInput.x, 0, moveInput.y);
        if (moveDirection.magnitude > 1) moveDirection.Normalize();

        // ─── SPRİNT VE ENERJİ SİSTEMİ ───
        bool isTryingToSprint = sprintAction.IsPressed() && moveDirection.sqrMagnitude > 0.1f;
        float currentSpeed = speed;
        bool isSprinting = false;

        // Exhaustion (Tamamen Yorulma) cezasını bekleme kontrolü
        if (exhaustionTimer > 0)
        {
            exhaustionTimer -= Time.fixedDeltaTime;
        }
        
        // Normal Regen bekleme kontrolü
        if (regenTimer > 0)
        {
            regenTimer -= Time.fixedDeltaTime;
        }

        // Hız ve Enerji Önceliği (Saldırı > İtme > Koşma > Dinlenme)
        if (isAttacking)
        {
            currentSpeed *= 0.5f;
            animator.SetBool(IsPushingHash, false);
        }
        else if (isPushingBarrier)
        {
            currentSpeed *= pushSpeedMultiplier;
            animator.SetBool(IsPushingHash, true);
        }
        else
        {
            animator.SetBool(IsPushingHash, false);

            if (isTryingToSprint && currentEnergy > 0 && exhaustionTimer <= 0)
            {
                isSprinting = true;
                currentSpeed *= sprintSpeedMultiplier;
                currentEnergy -= sprintEnergyCost * Time.fixedDeltaTime;
                
                // Sprint atıldığı için normal bekleme süresini (1 sn) sıfırla
                regenTimer = regenDelay;

                if (currentEnergy <= 0)
                {
                    currentEnergy = 0;
                    exhaustionTimer = exhaustionDelay;
                }
            }
            else if (!isTryingToSprint && exhaustionTimer <= 0 && regenTimer <= 0)
            {
                // Kullanılmıyorsa ve bekleme süreleri bittiyse enerjiyi doldur
                if (currentEnergy < maxEnergy)
                {
                    currentEnergy += energyRegenRate * Time.fixedDeltaTime;
                    if (currentEnergy > maxEnergy) currentEnergy = maxEnergy;
                }
            }
        }

        Vector3 newVelocity = new(moveDirection.x * currentSpeed, playerRb.linearVelocity.y, moveDirection.z * currentSpeed);
        playerRb.linearVelocity = newVelocity;

        Vector3 localMove = transform.InverseTransformDirection(moveDirection);
        animator.SetFloat(HorizontalHash, localMove.x, 0.15f, Time.fixedDeltaTime);
        animator.SetFloat(VerticalHash, localMove.z, 0.15f, Time.fixedDeltaTime);
        animator.SetFloat(MoveSpeedHash, moveDirection.magnitude, 0.15f, Time.fixedDeltaTime);
        
        // Karakter herhangi bir yöne hareket ediyorsa ve sprint tuşuna basılıysa sprint animasyonunu tetikle
        bool isMoving = moveDirection.sqrMagnitude > 0.1f;
        animator.SetBool(IsSprintingHash, isSprinting && isMoving);

        // Eğer saldırı halindeysek farenin / tetikçinin olduğu yöne dön (Kesin İsabet İçin)
        if (animator.GetBool(IsAttackingHash))
        {
            HandleLook();
        }
        else if (isMoving)
        {
            // Saldırmıyorsak ve hareket ediyorsak, sadece GİTTİĞİMİZ YÖNE (WASD) doğru dön
            // Bu, "Moonwalk" veya garip strafe animasyonları hissini yok edip 
            // karakteri saf bir Action-RPG (Hades, Bastion) akıcılığına kavuşturur.
            Quaternion targetRot = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * 15f);
        }
    }

    /// <summary>
    /// Bakış yönü: Mouse veya Gamepad sağ analog.
    /// Mouse hareket ediyorsa HER ZAMAN mouse önceliklidir.
    /// </summary>
    void HandleLook()
    {
        bool isMouseMoving = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.1f;

        bool gamepadRightStickActive = Gamepad.current != null &&
            Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.1f;

        if (gamepadRightStickActive && !isMouseMoving)
        {
            Vector2 stickInput = Gamepad.current.rightStick.ReadValue();
            Vector3 lookDir = new(stickInput.x, 0, stickInput.y);
            if (lookDir != Vector3.zero)
            {
                // Saldırı anında döndüğü için Slerp hızını oldukça yüksek tutarak hedefe "snap" edebiliriz
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * 25f);
            }
        }
        else
        {
            // Farenin bulunduğu yere ışınlanarak (LookAt ile) anında dön
            RotateTowardsMouse();
        }
    }

    public void RotateTowardsMouse()
    {
        // Mouse pozisyonunu yeni input system'den oku
        Vector2 mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        Plane groundPlane = new(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float rayDistance))
        {
            Vector3 pointToLook = ray.GetPoint(rayDistance);
            Vector3 targetDirection = new(pointToLook.x, transform.position.y, pointToLook.z);
            transform.LookAt(targetDirection);
        }
    }

    // ─── INPUT EVENT HANDLER'LARI ───
    // Bu metodlar tuşa basıldığında otomatik çağrılır

    void OnAttack(InputAction.CallbackContext ctx)
    {
        if (!GameManager.isGameActive) return;
        
        // Atak komutu verildiğinde ilk iş olarak imlece (Mouse) bir kez bakılmasını sağla
        // Bu, klavye ile ileri giderken (WASD) hemen arkasındaki adama anında sırtını dönüp vurmasını sağlar
        HandleLook();

        // Eğer kombo devam edebilecek durumdaysa (ve aktif bir atak animasyonu beklemesinde değilse)
        if (!animator.GetBool(IsAttackingHash) && comboStep < maxCombo)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    void OnDash(InputAction.CallbackContext ctx)
    {
        if (!GameManager.isGameActive || !canDash || isDashing) return;
        
        // Enerji yetiyor mu kontrol et
        if (currentEnergy < dashEnergyCost) return;

        StartCoroutine(DashRoutine());
    }

    void Update()
    {
        if (!GameManager.isGameActive) return;

        // Kombo sıfırlama süresi kontrolü
        if (comboStep > 0 && Time.time - lastAttackTime > comboResetTime)
        {
            comboStep = 0;
            if (animator != null) animator.SetInteger(ComboStepHash, comboStep);
        }
    }

    void OnSkill(InputAction.CallbackContext ctx)
    {
        if (!GameManager.isGameActive) return;

        // OrbitWeapon'ı tetikle
        if (orbitWeapon == null)
            orbitWeapon = GetComponentInChildren<OrbitWeapon>();

        if (orbitWeapon != null)
            orbitWeapon.ActivateSkill();
    }

    // ─── DASH MEKANİĞİ ───

    IEnumerator DashRoutine()
    {
        isDashing = true;
        canDash = false;
        
        // Dash enerjisini düş
        currentEnergy -= dashEnergyCost;
        if (currentEnergy <= 0)
        {
            currentEnergy = 0;
            exhaustionTimer = exhaustionDelay; // Dash ile enerji sıfırlanırsa da ceza al
        }
        else
        {
            regenTimer = regenDelay; // Dash kullanıldığında 1 saniye bekle
        }

        // Doğrudan klavye/gamepad durumunu oku
        Vector2 currentInput = ReadCurrentMovementInput();
        Vector3 dashDirection;

        if (currentInput.sqrMagnitude > 0.1f)
        {
            dashDirection = new Vector3(currentInput.x, 0, currentInput.y).normalized;
        }
        else
        {
            dashDirection = transform.forward;
        }

        // Lokal yön hesapla (oyuncunun baktığı yöne göre)
        Vector3 localDir = transform.InverseTransformDirection(dashDirection);

        // Baskın yöne göre doğru animasyonu seç
        // |z| > |x| → ileri/geri, |x| > |z| → sağ/sol
        string dashStateName;
        if (Mathf.Abs(localDir.z) >= Mathf.Abs(localDir.x))
            dashStateName = localDir.z >= 0 ? "DashForward" : "DashBack";
        else
            dashStateName = localDir.x >= 0 ? "DashRight" : "DashLeft";

        // Attack Layer'ı dash sırasında kapat
        int attackLayerIndex = animator.GetLayerIndex("Attack Layer");
        if (attackLayerIndex >= 0)
            animator.SetLayerWeight(attackLayerIndex, 0f);

        animator.SetBool(IsDashingHash, true);

        // Bir frame bekle + doğrudan o state'i oynat
        yield return null;
        animator.Play(dashStateName, 0, 0f);

        // Player collider'ını kapat
        Collider playerCollider = GetComponent<Collider>();
        if (playerCollider != null)
            playerCollider.enabled = false;

        // Dash hareketi
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            elapsed += Time.fixedDeltaTime;
            playerRb.linearVelocity = new Vector3(
                dashDirection.x * dashSpeed,
                playerRb.linearVelocity.y,
                dashDirection.z * dashSpeed
            );
            yield return new WaitForFixedUpdate();
        }

        // Dash bitti — isDashing=false ile Animator transition'ı tetiklenir
        isDashing = false;
        if (playerCollider != null)
            playerCollider.enabled = true;
        animator.SetBool(IsDashingHash, false);

        // Attack layer'ı geri aç
        if (attackLayerIndex >= 0)
            animator.SetLayerWeight(attackLayerIndex, 1f);

        // Cooldown
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // ─── SALDIRI SİSTEMİ ───

    IEnumerator AttackRoutine()
    {
        // Zamanlayıcıyı ve komboyu güncelle
        lastAttackTime = Time.time;
        comboStep++;
        
        animator.SetInteger(ComboStepHash, comboStep);
        animator.SetBool(IsAttackingHash, true);

        // Attack layer index bul, eğer 4 farklı animasyon (Attack1, Attack2 vs.) bağlanacaksa,
        // state adları "Attack1", "Attack2" kurallarına uymalı. 
        // Kullanıcı Editörden bu ayarları yapana kadar Play ile tetiklemiyoruz (Animator "comboStep" ile kendisi geçecek), 
        // Ama yine de eski düzen için fallback olarak "Attack" + comboStep çağırabiliriz.
        int attackLayerIndex = animator.GetLayerIndex("Attack Layer");
        if (attackLayerIndex >= 0)
        {
            yield return null;
            animator.Play("Attack" + comboStep, attackLayerIndex, 0f);
        }

        if (whooshSounds.Length > 0)
        {
            // Kombo 1 için Index 0, Kombo 2 için Index 1 çalar. 
            // array'den taşmamak için Modulo (%) kullanıyoruz.
            int soundIndex = (comboStep - 1) % whooshSounds.Length;
            audioSource.PlayOneShot(whooshSounds[soundIndex]);
        }

        yield return new WaitForSeconds(0.20f);

        GameObject vfxToSpawn = (activeWeapon.damage >= damageUpgradeThreshold) ? fireVFXPrefab : windVFXPrefab;

        if (vfxToSpawn != null && vfxSpawnPoint != null)
        {
            // vfxSpawnPoint'in içine (child) olarak Instantiate ediyoruz ki kılıcı/karakteri takip etsin
            GameObject vfx = Instantiate(vfxToSpawn, vfxSpawnPoint.position, vfxSpawnPoint.rotation, vfxSpawnPoint);
            
            // Kombo adımına göre yerel (local) rotasyonu ayarlıyoruz
            if (comboSlashAngles.Length > 0)
            {
                int angleIndex = (comboStep - 1) % comboSlashAngles.Length;
                vfx.transform.localEulerAngles = comboSlashAngles[angleIndex];
            }

            Destroy(vfx, 1.5f);
        }

        List<Component> hitEnemiesInThisSwing = new();
        float timer = 0f;
        float attackDuration = 0.3f; // Atak hit süresi
        float currentKnockbackMultiplier = (comboStep == 4) ? heavyFinisherKnockbackMultiplier : 1f;

        while (timer < attackDuration)
        {
            timer += Time.deltaTime;
            Vector3 hitCenter = transform.position + transform.forward * hitOffset;
            Collider[] hitColliders = Physics.OverlapSphere(hitCenter, hitRadius);

            foreach (var col in hitColliders)
            {
                if (col.CompareTag("Enemy") && col.TryGetComponent<EnemyBase>(out var enemyBase))
                {
                    if (!hitEnemiesInThisSwing.Contains(enemyBase))
                    {
                        Vector3 directionToEnemy = col.transform.position - transform.position;
                        float distanceToEnemy = directionToEnemy.magnitude;

                        // Duvar arkasından vurmamak için Raycast
                        if (Physics.Raycast(transform.position + Vector3.up, directionToEnemy, out RaycastHit hit, distanceToEnemy))
                        {
                            if (hit.collider.CompareTag("Barrier"))
                            {
                                continue;
                            }
                        }
                        
                        enemyBase.TakeDamage(activeWeapon.damage, transform.position, currentKnockbackMultiplier);
                        hitEnemiesInThisSwing.Add(enemyBase);
                        
                        // 4. vuruşta ekran daha şiddetli sarsılsın
                        float shakeMag = (comboStep == 4) ? 0.3f : 0.1f;
                        float shakeDur = (comboStep == 4) ? 0.25f : 0.15f;
                        camScript.TriggerShake(shakeMag, shakeDur);
                    }
                }

                else if (col.CompareTag("Barrel") && col.TryGetComponent<ExplosiveBarrel>(out var barrel))
                {
                    if (!hitEnemiesInThisSwing.Contains(barrel))
                    {
                        barrel.TakeDamage(activeWeapon.damage, transform.position, currentKnockbackMultiplier);
                        hitEnemiesInThisSwing.Add(barrel);
                    }
                }
            }
            yield return null;
        }

        animator.SetBool(IsAttackingHash, false);
    }

    // ─── DAMAGE & EFFECTS ───

    public void TakeDamageEffect()
    {
        if (animator != null)
        {
            animator.SetTrigger(TakeDamageHash);
        }

        if (camScript != null) camScript.TriggerShake(0.15f, 0.2f);
    }

    public IEnumerator ApplySlow(float slowModifier, float slowDuration)
    {
        if (activeSlowCoroutine != null)
        {
            StopCoroutine(activeSlowCoroutine);
            speed = originalSpeed;
        }

        speed = originalSpeed * slowModifier;

        yield return new WaitForSecondsRealtime(slowDuration);

        speed = originalSpeed;
        activeSlowCoroutine = null;
    }

    public void StartSlow(float slowModifier, float slowDuration)
    {
        if (activeSlowCoroutine != null)
        {
            StopCoroutine(activeSlowCoroutine);
        }
        activeSlowCoroutine = StartCoroutine(ApplySlow(slowModifier, slowDuration));
    }

    /// <summary>
    /// Doğrudan klavye/gamepad durumunu okur.
    /// moveAction.ReadValue() callback zamanlamasında güvenilir olmayabiliyor,
    /// bu yüzden dash yönü için bu metodu kullanıyoruz.
    /// </summary>
    Vector2 ReadCurrentMovementInput()
    {
        Vector2 input = Vector2.zero;

        // Önce gamepad kontrol et
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.1f)
                return stick;
        }

        // Klavyeden oku
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1;
        }

        if (input.sqrMagnitude > 1f) input.Normalize();
        return input;
    }

    // Dash durumunu dışarıdan kontrol etmek için
    public bool IsDashing => isDashing;

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Barrier"))
        {
            if (moveInput.magnitude > 0.1f)
            {
                Vector3 dirToBarrier = (collision.transform.position - transform.position).normalized;
                if (Vector3.Dot(transform.forward, dirToBarrier) > 0.5f)
                {
                    pushTimer = 0.15f;
                }
            }
        }
    }
}