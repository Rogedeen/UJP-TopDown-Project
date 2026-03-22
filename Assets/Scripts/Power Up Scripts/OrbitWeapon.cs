using System.Collections;
using UnityEngine;

public class OrbitWeapon : MonoBehaviour
{
    public float rotationSpeed;
    public Transform orbitTransform;
    public int damage;
    public float duration;
    public float cooldown;

    public bool canUseSkill = true;
    public bool isRotating = false;

    [Header("Juice (Polishing & Feedback)")]
    [Tooltip("Yetenek (Skill) kullanıldığı an patlayacak görsel efekt (VFX).")]
    public GameObject skillStartVfxPrefab;
    [Tooltip("Yetenek (Skill) kullanıldığında çalacak özel ses (SFX).")]
    public AudioClip skillStartSound;

    // UI için 0 ile 1 arasında doluluk oranı (1 = kullanıma hazır)
    public float currentCooldownRatio { get; private set; } = 1f;
    private float elapsedCooldown = 0f;

    public void AdvanceCooldown(float timeAmount)
    {
        // Eğer şu an bekleme süresindeysek (cooldown), barı ileri sar
        if (!canUseSkill && !isRotating)
        {
            elapsedCooldown += timeAmount;
            currentCooldownRatio = Mathf.Clamp01(elapsedCooldown / cooldown);
        }
    }

    private Renderer[] weaponRenderers;
    private Collider[] weaponColliders;

    void Awake()
    {
        weaponRenderers = GetComponentsInChildren<Renderer>();
        weaponColliders = GetComponentsInChildren<Collider>();

        SetVisibilityAndCollision(false);
    }

    private void SetVisibilityAndCollision(bool state)
    {
        if (weaponRenderers != null)
        {
            foreach (var r in weaponRenderers) 
                if (r != null) r.enabled = state;
        }
        if (weaponColliders != null)
        {
            foreach (var c in weaponColliders) 
                if (c != null && !c.isTrigger) c.enabled = state; // Trigger olmayanları kapatıp aç
                else if (c != null && c.isTrigger) c.enabled = state; // Trigger ise doğrudan çarpışmayı kapat
        }
    }

    void Update()
    {
        if (isRotating)
        {
            transform.RotateAround(orbitTransform.position, Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// PlayerController tarafından Skill tuşuna basıldığında çağrılır.
    /// Artık OrbitWeapon kendi input'unu okumaz — merkezi input yönetimi.
    /// </summary>
    public void ActivateSkill()
    {
        if (!GameManager.isGameActive || !canUseSkill || isRotating) return;
        StartCoroutine(SkillCycle());
    }

    IEnumerator SkillCycle()
    {
        canUseSkill = false;
        isRotating = true;
        SetVisibilityAndCollision(true);
        
        // GÖRSEL VE İŞİTSEL EFEKTLER (JUICE)
        if (skillStartVfxPrefab != null)
        {
            GameObject vfx = Instantiate(skillStartVfxPrefab, transform.position, Quaternion.identity, transform);
            Destroy(vfx, 3f);
        }

        if (skillStartSound != null)
        {
            AudioSource.PlayClipAtPoint(skillStartSound, transform.position);
        }
        
        // 1) Kullanım Süresi (Duration): Bar 1'den 0'a yavaşça boşalır
        float elapsedDuration = 0f;
        while (elapsedDuration < duration)
        {
            elapsedDuration += Time.deltaTime;
            currentCooldownRatio = 1f - Mathf.Clamp01(elapsedDuration / duration);
            yield return null;
        }

        SetVisibilityAndCollision(false);
        isRotating = false;

        // 2) Bekleme Süresi (Cooldown): Bar 0'dan 1'e yavaşça dolar
        elapsedCooldown = 0f;
        while (elapsedCooldown < cooldown)
        {
            elapsedCooldown += Time.deltaTime;
            currentCooldownRatio = Mathf.Clamp01(elapsedCooldown / cooldown);
            yield return null;
        }

        currentCooldownRatio = 1f;
        canUseSkill = true;
    }
}
