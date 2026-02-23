using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    void Start()
    {
        animator = GetComponent<Animator>();
        playerRb = GetComponent<Rigidbody>();
        camScript = Camera.main.GetComponent<FollowPlayer>();
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
        // Hareket ve Dönüş
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = new(horizontalInput, 0, verticalInput);
        if (moveDirection.magnitude > 1) moveDirection.Normalize();

        Vector3 newVelocity = new(moveDirection.x * speed, playerRb.linearVelocity.y, moveDirection.z * speed);
        playerRb.linearVelocity = newVelocity;

        animator.SetFloat("moveSpeed", moveDirection.magnitude * speed);

        RotateTowardsMouse();
    }

    public void RotateTowardsMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
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


        Debug.Log("Vuruş Anındaki Hasar: " + activeWeapon.damage);

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
                        /*
                        Time.timeScale = 0;
                        yield return new WaitForSecondsRealtime(0.1f);
                        Time.timeScale = 1;
                        //hit stop denedim ama pek begenmedim
                        */
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


    /*private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 hitCenter = transform.position + transform.forward * hitOffset;
        Gizmos.DrawWireSphere(hitCenter, hitRadius);
    }*/
}