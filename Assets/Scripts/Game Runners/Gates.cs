using UnityEngine;

public class Gates : MonoBehaviour
{
    public bool isActive;





    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Barrier")) 
        {
            isActive = false;
        }
    }
}
