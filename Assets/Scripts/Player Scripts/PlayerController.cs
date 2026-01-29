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
    public AudioClip[] whoosSounds;

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

        // 1. Bu savurmada vurduğumuz düşmanları aklımızda tutmak için bir liste
        List<Enemy> hitEnemiesInThisSwing = new();

        int randomIndex = Random.Range(0, whoosSounds.Length);
        audioSource.PlayOneShot(whoosSounds[randomIndex]);

        // 2. Savurma başlamadan önceki kısa bekleme (Anticipation)
        yield return new WaitForSeconds(0.30f);

        GameObject vfxToSpawn = (activeWeapon.damage >= damageUpgradeThreshold) ? fireVFXPrefab : windVFXPrefab;

        

        if (vfxToSpawn != null)
        {
            // Efekti karakterin önünde oluştur
            GameObject vfxInstance = Instantiate(vfxToSpawn, vfxSpawnPoint.position, vfxSpawnPoint.rotation);

            // Profesyonel Not: Efektler sahnede birikmesin diye 2 saniye sonra yok et
            Destroy(vfxInstance, 2f);
        }

        // 3. TARAMA BAŞLIYOR: 0.3 saniye boyunca her karede kontrol et
        float timer = 0f;
        float attackDuration = 0.3f; // Savurmanın ne kadar süreceği

        while (timer < attackDuration)
        {
            timer += Time.deltaTime;

            // Vuruş alanını hesapla
            Vector3 hitCenter = transform.position + transform.forward * hitOffset;
            Collider[] hitColliders = Physics.OverlapSphere(hitCenter, hitRadius);

            foreach (var col in hitColliders)
            {
                if (col.CompareTag("Enemy") && col.TryGetComponent<Enemy>(out var enemy))
                {
                    // EĞER bu düşmana bu savurmada daha önce vurmadıysak hasar ver
                    if (!hitEnemiesInThisSwing.Contains(enemy))
                    {
                        enemy.TakeDamage(activeWeapon.damage, gameObject.transform.position);
                        hitEnemiesInThisSwing.Add(enemy); // Listeye ekle ki bir daha vurmayalım
                        Debug.Log(enemy.name + " savurma sırasında yakalandı!");
                    }
                }
            }

            // Bir sonraki kareye kadar bekle
            yield return null;
        }

        // 4. Savurma bitti
        animator.SetBool("isAttacking", false);
    }

    // --- KRAL DOKUNUŞU: HIT EVENT ---
    // Bu fonksiyonu Animasyon Penceresinden (Animation Event) çağıracaksın!
    public void HitEvent()
    {
        // 1. Vuruş merkezini hesapla (Karakterin tam önünde)
        Vector3 hitCenter = transform.position + transform.forward * hitOffset;

        // 2. Bu hayali kürenin içindeki her şeyi yakala
        Collider[] hitColliders = Physics.OverlapSphere(hitCenter, hitRadius);

        foreach (var hitCollider in hitColliders)
        {
            // 3. Eğer çarptığımız şey düşmansa hasar ver
            if (hitCollider.CompareTag("Enemy"))
            {
                // TryGetComponent kullanarak daha güvenli hasar verme
                if (hitCollider.TryGetComponent<Enemy>(out var enemy))
                {
                    enemy.TakeDamage(activeWeapon.damage, gameObject.transform.position);
                    Debug.Log(hitCollider.name + " alan hasarı yedi!");
                }
            }
        }
    }

    // Editörde vuruş alanını görmek için (Sahnede kırmızı bir küre çizer)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 hitCenter = transform.position + transform.forward * hitOffset;
        Gizmos.DrawWireSphere(hitCenter, hitRadius);
    }
}