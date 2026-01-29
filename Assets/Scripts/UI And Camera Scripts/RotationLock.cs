using UnityEngine;

public class RotationLock : MonoBehaviour
{
    void LateUpdate()
    {
        // Karakterin ne kadar dönerse dönsün, bu objenin dünyadaki rotasyonunu 
        // hep 'sıfır'da (başlangıç açısında) tutar.
        transform.rotation = Quaternion.identity;
    }
}
