using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 1.0f;
    public float verticalInput;
    public float horizontalInput;
    public Weapon activeWeapon;

    private Rigidbody playerRb;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerRb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = new(horizontalInput, 0, verticalInput);
        if (moveDirection.magnitude > 1){
            moveDirection.Normalize();
        }

        Vector3 newVelocity = new(moveDirection.x * speed, playerRb.linearVelocity.y, moveDirection.z * speed);
        playerRb.linearVelocity = newVelocity;
        animator.SetFloat("moveSpeed", moveDirection.magnitude);

        RotateTowardsMouse();
    }

    public void RotateTowardsMouse() 
    {
        // 1. Kameradan farenin pozisyonuna bir ışın (Ray) oluştur
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 2. Işının yere çarpıp çarpmadığını kontrol etmek için bir Plane (Düzlem) tanımlayalım
        // (Y=0 düzleminde, yukarı bakan hayali bir yer)
        Plane groundPlane = new(Vector3.up, Vector3.zero);

        // 3. Eğer ışın bu düzleme çarpıyorsa
        if (groundPlane.Raycast(ray, out float rayDistance))
        {
            // 4. Çarpışma noktasını bul
            Vector3 pointToLook = ray.GetPoint(rayDistance);

            // 5. Karakterin sadece Y ekseninde dönmesi için bakılacak noktanın Y değerini eşitle
            Vector3 targetDirection = new(pointToLook.x, transform.position.y, pointToLook.z);

            // 6. Karakteri o yöne döndür (LookAt yerine daha yumuşak Quaternion.LookRotation da olur)
            transform.LookAt(targetDirection);
        }
    }
    void Update()
    {
        if (GameManager.isGameActive && Input.GetMouseButtonDown(0))
        {
            StartCoroutine(AttackRoutine());
        }
    }

    IEnumerator AttackRoutine() 
    {
        animator.SetBool("isAttacking", true); // Animasyonu başlat

        // Profesyonel Dokunuş: Animasyonun tam vuruş anına kadar minik bir bekleme
        yield return new WaitForSeconds(0.15f);

        activeWeapon.SetWeaponCollider(true); // Collider'ı aç

        // Kılıç savurma süresi (Karakterin animasyon hızına göre ayarla)
        yield return new WaitForSeconds(0.3f);

        animator.SetBool("isAttacking", false);
        activeWeapon.SetWeaponCollider(false); // Collider'ı kapat
    }
}
