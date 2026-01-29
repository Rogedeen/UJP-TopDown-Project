using System.Collections;
using UnityEngine;

public class OrbitWeapon : MonoBehaviour
{
    public float rotationSpeed;
    public Transform orbitTransform;
    public int damage;
    public float duration;
    public float cooldown;
    public bool isSkillActive = false;


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
        if (GameManager.isGameActive && Input.GetKeyDown(KeyCode.F) && !isSkillActive)
        {
            StartCoroutine(UseSkill());
        }

        if (isSkillActive) 
        {
            transform.RotateAround(orbitTransform.position, Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    IEnumerator UseSkill() 
    {
        isSkillActive = true;
        weaponRenderer.enabled = true;
        weaponCollider.enabled = true;   
        
        yield return new WaitForSeconds(duration);

        weaponRenderer.enabled = false;
        weaponCollider.enabled = false;
        isSkillActive = false;
    }
}
