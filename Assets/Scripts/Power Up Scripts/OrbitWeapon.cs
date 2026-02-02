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
        if (GameManager.isGameActive && Input.GetKeyDown(KeyCode.F) && canUseSkill && !isRotating)
        {
            StartCoroutine(SkillCycle());
        }

        if (isRotating) 
        {
            transform.RotateAround(orbitTransform.position, Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    IEnumerator SkillCycle() 
    {
        canUseSkill = false;
        isRotating = true;
        weaponRenderer.enabled = true;
        weaponCollider.enabled = true;   
        
        yield return new WaitForSeconds(duration);

        weaponRenderer.enabled = false;
        weaponCollider.enabled = false;
        isRotating = false;

        yield return new WaitForSeconds(cooldown);
        canUseSkill = true;
    }
}
