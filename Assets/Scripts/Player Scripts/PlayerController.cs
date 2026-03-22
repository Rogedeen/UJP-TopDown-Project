using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5.0f;
    [SerializeField] private float sprintSpeedMultiplier = 1.5f;

    [Header("Energy (Charge) Settings")]
    public float maxEnergy = 100f;
    [SerializeField] private float energyRegenRate = 10f;
    [SerializeField] private float sprintEnergyCost = 20f;
    [SerializeField] private float dashEnergyCost = 30f;
    [SerializeField] private float exhaustionDelay = 2f;
    [SerializeField] private float regenDelay = 1f;

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

    public void IncreaseRegenRate(float amount)
    {
        energyRegenRate += amount;
    }

    [Header("Combo System Settings")]
    public int maxCombo = 3;
    public float comboResetTime = 1.0f;
    [SerializeField] private float heavyFinisherKnockbackMultiplier = 3.0f;
    
    public Vector3[] comboSlashAngles = new Vector3[] {
        new Vector3(0, 0, 0),
        new Vector3(0, 0, 180),
        new Vector3(0, 0, -90),
        new Vector3(0, 0, 90)
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
    public Light nightLight;

    [Header("Sound Settings")]
    public AudioSource audioSource;
    public AudioClip[] whooshSounds;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    private Rigidbody playerRb;
    private Animator animator;
    private FollowPlayer camScript;
    private Camera mainCamera;
    private PlayerHealth playerHealth;

    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction dashAction;
    private InputAction skillAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private Vector2 moveInput;

    private bool isDashing = false;
    private bool canDash = true;

    private float originalSpeed;
    private Coroutine activeSlowCoroutine;

    [Header("Push Action")]
    public float pushSpeedMultiplier = 0.3f;
    private bool isPushingBarrier = false;
    private float pushTimer = 0f;

    private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
    private static readonly int VerticalHash = Animator.StringToHash("Vertical");
    private static readonly int MoveSpeedHash = Animator.StringToHash("moveSpeed");
    private static readonly int IsAttackingHash = Animator.StringToHash("isAttacking");
    private static readonly int TakeDamageHash = Animator.StringToHash("takeDamage");
    private static readonly int IsDashingHash = Animator.StringToHash("isDashing");
    private static readonly int IsSprintingHash = Animator.StringToHash("isSprinting");
    private static readonly int ComboStepHash = Animator.StringToHash("comboStep");
    private static readonly int IsPushingHash = Animator.StringToHash("isPushing");

    private OrbitWeapon orbitWeapon;
    public OrbitWeapon ActiveOrbitWeapon => orbitWeapon;
    public bool IsDashing => isDashing;

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerRb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();

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

        orbitWeapon = GetComponentInChildren<OrbitWeapon>();

        if (nightLight != null && DayNightManager.Instance != null && DayNightManager.Instance.timePhases != null)
        {
            int lastPhase = DayNightManager.Instance.timePhases.Length - 1;
            nightLight.enabled = (DayNightManager.Instance.CurrentPhaseIndex >= lastPhase);
        }

        ValidateAnimatorParameters();
    }

    void ValidateAnimatorParameters()
    {
        if (animator == null)
        {
            Debug.LogError("[PlayerController] Animator component bulunamadı!", gameObject);
            return;
        }

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
                        Debug.LogWarning($"[PlayerController] Parametre '{name}' var ama tipi yanlış! Beklenen: {type}, Bulunan: {param.type}", gameObject);
                    }
                    break;
                }
            }

            if (!found)
            {
                Debug.LogError($"[PlayerController] Animator'da '{name}' parametresi YOK!", gameObject);
            }
        }
    }

    void OnEnable()
    {
        inputActions.Enable();
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
            int lastPhase = DayNightManager.Instance.timePhases.Length - 1;
            nightLight.enabled = (newPhaseIndex == lastPhase);
        }
    }

    void Update()
    {
        if (!GameManager.isGameActive) return;

        if (comboStep > 0 && Time.time - lastAttackTime > comboResetTime)
        {
            comboStep = 0;
            if (animator != null) animator.SetInteger(ComboStepHash, comboStep);
        }
    }

    void OnSkill(InputAction.CallbackContext ctx)
    {
        if (!GameManager.isGameActive) return;

        if (orbitWeapon == null)
            orbitWeapon = GetComponentInChildren<OrbitWeapon>();

        if (orbitWeapon != null)
            orbitWeapon.ActivateSkill();
    }
}