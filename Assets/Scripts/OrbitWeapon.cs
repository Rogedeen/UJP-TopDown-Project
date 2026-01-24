using UnityEngine;

public class OrbitWeapon : MonoBehaviour
{
    public float rotationSpeed;
    public Transform orbitTransform;
    public int damage;

    void Start()
    {
       
    }

    void Update()
    {
        if (GameManager.isGameActive) 
        {
                transform.RotateAround(orbitTransform.position, Vector3.up, rotationSpeed * Time.deltaTime);
        }              
    }
}
