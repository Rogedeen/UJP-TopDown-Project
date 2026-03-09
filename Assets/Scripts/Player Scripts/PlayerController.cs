using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5.0f;
    public float verticalInput;
    public float horizontalInput;

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

    private Rigidbody playerRb;
    private Animator animator;
    private FollowPlayer camScript;
    private Camera mainCamera;

    private float originalSpeed;
    private Coroutine activeSlowCoroutine;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerRb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        camScript = mainCamera.GetComponent<FollowPlayer>();
        originalSpeed = speed;
    }

    void Update()
    {
        if (GameManager.isGameActive && Input.GetMouseButtonDown(0))
        {
            if (!animator.GetBool("isAttacking"))
            {
                StartCoroutine(AttackRoutine());
            }
        }
    }

    void FixedUpdate()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = new(horizontalInput, 0, verticalInput);
        if (moveDirection.magnitude > 1) moveDirection.Normalize();

        Vector3 newVelocity = new(moveDirection.x * speed, playerRb.linearVelocity.y, moveDirection.z * speed);
        playerRb.linearVelocity = newVelocity;

        Vector3 localMove = transform.InverseTransformDirection(moveDirection);

        animator.SetFloat("Horizontal", localMove.x, 0.15f, Time.fixedDeltaTime);
        animator.SetFloat("Vertical", localMove.z, 0.15f, Time.fixedDeltaTime);
        animator.SetFloat("moveSpeed", moveDirection.magnitude, 0.15f, Time.fixedDeltaTime);


        RotateTowardsMouse();
    }

    public void TakeDamageEffect()
    {
        if (animator != null)
        {
            animator.SetTrigger("takeDamage"); 
        }

        if (camScript != null) camScript.TriggerShake(0.15f, 0.2f);

    }

    public void RotateTowardsMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float rayDistance))
        {
            Vector3 pointToLook = ray.GetPoint(rayDistance);
            Vector3 targetDirection = new(pointToLook.x, transform.position.y, pointToLook.z);
            transform.LookAt(targetDirection);
        }
    }

    IEnumerator AttackRoutine()
    {
        animator.SetBool("isAttacking", true);

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

        animator.SetBool("isAttacking", false);
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
}