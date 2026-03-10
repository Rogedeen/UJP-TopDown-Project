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
    private Vector2 moveInput;

    // Dash state
    private bool isDashing = false;
    private bool canDash = true;

    // Slow state
    private float originalSpeed;
    private Coroutine activeSlowCoroutine;

    // Animator hash'ler
    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int MoveSpeedHash = Animator.StringToHash("moveSpeed");
    private static readonly int IsAttackingHash = Animator.StringToHash("isAttacking");
    private static readonly int TakeDamageHash = Animator.StringToHash("takeDamage");
    private static readonly int IsDashingHash = Animator.StringToHash("isDashing");

    // OrbitWeapon referansı - Skill tuşu basıldığında tetiklenir
    private OrbitWeapon orbitWeapon;

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
    }

    void Start()
    {
        mainCamera = Camera.main;
        camScript = mainCamera.GetComponent<FollowPlayer>();
        originalSpeed = speed;

        // OrbitWeapon varsa bul
        orbitWeapon = GetComponentInChildren<OrbitWeapon>();

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
    }

    void OnDisable()
    {
        // Event aboneliklerini kaldır (memory leak önleme)
        attackAction.performed -= OnAttack;
        dashAction.performed -= OnDash;
        skillAction.performed -= OnSkill;

        inputActions.Disable();
    }

    void FixedUpdate()
    {
        if (isDashing) return; // Dash sırasında normal hareket yok

        // Move action'dan Vector2 oku: x = horizontal, y = vertical
        moveInput = moveAction.ReadValue<Vector2>();

        Vector3 moveDirection = new(moveInput.x, 0, moveInput.y);
        if (moveDirection.magnitude > 1) moveDirection.Normalize();

        Vector3 newVelocity = new(moveDirection.x * speed, playerRb.linearVelocity.y, moveDirection.z * speed);
        playerRb.linearVelocity = newVelocity;

        // Blend Tree'ye gönder — senin blend tree zaten Horizontal ve Vertical bekliyor
        // InverseTransformDirection: Dünya yönünü oyuncunun bakış yönüne çevirir
        // Böylece oyuncu fareye bakarken W'ye basınca "ileri", A'ya basınca "sola strafe" animasyonu oynar
        Vector3 localMove = transform.InverseTransformDirection(moveDirection);
        animator.SetFloat(HorizontalHash, localMove.x, 0.15f, Time.fixedDeltaTime);
        animator.SetFloat(VerticalHash, localMove.z, 0.15f, Time.fixedDeltaTime);
        animator.SetFloat(MoveSpeedHash, moveDirection.magnitude, 0.15f, Time.fixedDeltaTime);

        HandleLook();
    }

    /// <summary>
    /// Bakış yönü: Mouse veya Gamepad sağ analog.
    /// Mouse kullanılıyorsa → mouse pozisyonuna bak.
    /// Gamepad kullanılıyorsa → sağ analog yönüne bak.
    /// </summary>
    void HandleLook()
    {
        // Gamepad sağ analog kontrolü
        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        // Eğer gamepad sağ analog hareket ediyorsa, ona göre dön
        if (Gamepad.current != null && lookInput.sqrMagnitude > 0.1f)
        {
            Vector3 lookDir = new(lookInput.x, 0, lookInput.y);
            if (lookDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * 15f);
            }
        }
        else
        {
            // Mouse ile bak (varsayılan)
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
        if (!animator.GetBool(IsAttackingHash))
        {
            StartCoroutine(AttackRoutine());
        }
    }

    void OnDash(InputAction.CallbackContext ctx)
    {
        if (!GameManager.isGameActive || !canDash || isDashing) return;
        StartCoroutine(DashRoutine());
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
        animator.SetBool(IsAttackingHash, true);

        // Attack Layer'da animasyonu zorla başlat
        // (Input System callback zamanlaması nedeniyle bool tek başına yetmiyor)
        int attackLayerIndex = animator.GetLayerIndex("Attack Layer");
        if (attackLayerIndex >= 0)
        {
            yield return null;
            animator.Play("Attack", attackLayerIndex, 0f);
        }

        if (whooshSounds.Length > 0)
        {
            audioSource.PlayOneShot(whooshSounds[Random.Range(0, whooshSounds.Length)]);
        }

        yield return new WaitForSeconds(0.20f);

        GameObject vfxToSpawn = (activeWeapon.damage >= damageUpgradeThreshold) ? fireVFXPrefab : windVFXPrefab;

        if (vfxToSpawn != null && vfxSpawnPoint != null)
        {
            GameObject vfx = Instantiate(vfxToSpawn, vfxSpawnPoint.position, vfxSpawnPoint.rotation);
            Destroy(vfx, 1.5f);
        }

        List<Component> hitEnemiesInThisSwing = new();
        float timer = 0f;
        float attackDuration = 0.3f;

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

                        if (Physics.Raycast(transform.position + Vector3.up, directionToEnemy, out RaycastHit hit, distanceToEnemy))
                        {
                            if (hit.collider.CompareTag("Barrier"))
                            {
                                continue;
                            }
                        }
                        enemyBase.TakeDamage(activeWeapon.damage, transform.position);
                        hitEnemiesInThisSwing.Add(enemyBase);
                        camScript.TriggerShake(0.1f, 0.15f);
                    }
                }

                else if (col.CompareTag("Barrel") && col.TryGetComponent<ExplosiveBarrel>(out var barrel))
                {
                    if (!hitEnemiesInThisSwing.Contains(barrel))
                    {
                        barrel.TakeBarrelDamage(activeWeapon.damage);
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
}