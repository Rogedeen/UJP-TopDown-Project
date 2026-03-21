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

    private Renderer weaponRenderer;
    private Collider weaponCollider;

    void Start()
    {
        weaponRenderer = GetComponent<Renderer>();
        weaponCollider = GetComponent<Collider>();

        weaponRenderer.enabled = false;
        weaponCollider.enabled = false;
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
        weaponRenderer.enabled = true;
        weaponCollider.enabled = true;
        
        // 1) Kullanım Süresi (Duration): Bar 1'den 0'a yavaşça boşalır
        float elapsedDuration = 0f;
        while (elapsedDuration < duration)
        {
            elapsedDuration += Time.deltaTime;
            currentCooldownRatio = 1f - Mathf.Clamp01(elapsedDuration / duration);
            yield return null;
        }

        weaponRenderer.enabled = false;
        weaponCollider.enabled = false;
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
