using UnityEngine;

public class Weapon : MonoBehaviour
{
    public int damage = 1;
    private Collider weaponCollider;

    void Start()
    {
        // Başlangıçta collider'ı al ve kapat
        weaponCollider = GetComponent<Collider>();
        if (weaponCollider != null)
        {
            weaponCollider.enabled = false;
        }
    }

    // Bu fonksiyonu PlayerController çağıracak
    public void SetWeaponCollider(bool state)
    {
        if (weaponCollider != null)
        {
            weaponCollider.enabled = state;
        }
    }
}