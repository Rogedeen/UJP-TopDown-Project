using System.Collections.Generic;
using UnityEngine;

public class CameraOcclusionFader : MonoBehaviour
{
    [Header("Referanslar")]
    public Transform playerTransform;

    [Header("Ayarlar")]
    // Sadece bu layer'daki objeler şeffaflaşsın
    // Inspector'dan duvarların layer'ını ata
    public LayerMask wallLayer;
    public float fadedAlpha = 0.05f;  // Tamamen kaybetme, hafif siluet kalsın
    public float fadeSpeed = 8f;

    // Şu an şeffaf olan materyalleri takip ediyoruz
    // Bir önceki frame'de şeffaf olup bu frame'de olmayanları geri getirmek için
    private Dictionary<Material, float> fadedMaterials = new();

    void Update()
    {
        // Önce geçen frame'de şeffaflaştırdığımız tüm materyalleri geri getir
        // Sonra yeniden kontrol edeceğiz hangilerinin şeffaf kalması gerektiğini
        foreach (var mat in fadedMaterials.Keys)
        {
            Color c = mat.color;
            c.a = Mathf.Lerp(c.a, 1f, Time.deltaTime * fadeSpeed);
            mat.color = c;
        }

        // Kameradan oyuncuya doğru bir SphereCast at
        // SphereCast, Raycast'ten daha geniş bir alan tarıyor
        // ince duvarları kaçırmamak için kullanıyoruz
        Vector3 direction = playerTransform.position - transform.position;
        float distance = direction.magnitude;

        RaycastHit[] hits = Physics.RaycastAll(
            transform.position,
            direction.normalized,
            distance,
            wallLayer
        );

        // Yeni bir dictionary oluştur, sadece bu frame'de engel olan duvarları tutacak
        Dictionary<Material, float> newFaded = new();

        foreach (RaycastHit hit in hits)
        {
            Renderer r = hit.collider.GetComponent<Renderer>();
            if (r == null) continue;

            // Her renderer'ın materyalinin kopyasını al
            // "mat" değil "material" kullanıyoruz, bu otomatik kopya oluşturur
            Material mat = r.material;
            Color c = mat.color;
            c.a = Mathf.Lerp(c.a, fadedAlpha, Time.deltaTime * fadeSpeed);
            mat.color = c;

            newFaded[mat] = fadedAlpha;
        }

        fadedMaterials = newFaded;
    }
}
