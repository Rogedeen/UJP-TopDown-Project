using UnityEngine;

public class OrbitWeapon : MonoBehaviour
{
    public float rotationSpeed;
    public Transform orbitTransform;
    void Start()
    {
       
    }

    void Update()
    {
        transform.RotateAround(orbitTransform.position, Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
