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
    public float hitRadius = 2.5f; // Vuruş alanının genişliği (Dairenin çapı gibi düşün)
    public float hitOffset = 1.5f; // Vuruşun karakterin ne kadar önünde olacağı

    [Header("VFX Settings")]
    public GameObject windVFXPrefab; // Normal atak efekti
    public GameObject fireVFXPrefab; // Güçlendirilmiş atak efekti
    public Transform vfxSpawnPoint;  // Efektin çıkacağı nokta (Genelde karakterin önü)
    public int damageUpgradeThreshold = 2;

    [Header("Sound Settings")]
    public AudioSource audioSource;
    public AudioClip[] whooshSounds;

    private Rigidbody playerRb;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerRb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Atak kontrolünü Update'te yapman doğru
        if (GameManager.isGameActive && Input.GetMouseButtonDown(0))
        {
            // Eğer zaten saldırıyorsak tekrar başlatma (opsiyonel ama iyidir)
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

        // --- 1. SES EFEKTİ ---
        if (whooshSounds.Length > 0)
        {
            audioSource.PlayOneShot(whooshSounds[Random.Range(0, whooshSounds.Length)]);
        }

        // --- 2. ANTICIPATION (Vuruş Öncesi Bekleme) ---
        yield return new WaitForSeconds(0.20f);

        // --- 3. VFX SEÇİMİ VE DOĞURMA ---
        // Konsoldan hasarı kontrol et:
        Debug.Log("Vuruş Anındaki Hasar: " + activeWeapon.damage);

        GameObject vfxToSpawn = (activeWeapon.damage >= damageUpgradeThreshold) ? fireVFXPrefab : windVFXPrefab;

        if (vfxToSpawn != null && vfxSpawnPoint != null)
        {
            GameObject vfx = Instantiate(vfxToSpawn, vfxSpawnPoint.position, vfxSpawnPoint.rotation);
            Destroy(vfx, 1.5f);
        }

        // --- 4. SÜPÜRME TARAMASI (SWEEP) ---
        List<Enemy> hitEnemiesInThisSwing = new List<Enemy>();
        float timer = 0f;
        float attackDuration = 0.3f;

        while (timer < attackDuration)
        {
            timer += Time.deltaTime;
            Vector3 hitCenter = transform.position + transform.forward * hitOffset;
            Collider[] hitColliders = Physics.OverlapSphere(hitCenter, hitRadius);

            foreach (var col in hitColliders)
            {
                if (col.CompareTag("Enemy") && col.TryGetComponent<Enemy>(out var enemy))
                {
                    if (!hitEnemiesInThisSwing.Contains(enemy))
                    {
                        // Hasar veriyoruz (Knockback için oyuncu pozisyonunu gönderiyoruz)
                        enemy.TakeDamage(activeWeapon.damage, transform.position);
                        hitEnemiesInThisSwing.Add(enemy);
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