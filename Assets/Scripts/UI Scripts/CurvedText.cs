using UnityEngine;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class CurvedText : MonoBehaviour
{
    private TMP_Text textComponent;

    [Header("Kavis Ayarları")]
    [Tooltip("Yazının kavis şeklini belirler. (Ortasını yukarı çekerek yay yapabilirsiniz)")]
    public AnimationCurve vertexCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
    
    [Tooltip("Kavisin ne kadar şiddetli/yüksek olacağı")]
    public float curveMultiplier = 10f;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (textComponent == null) return;
        
        // Önce Unity'ye yazıyı normal (düz) haliyle hesaplamasını söylüyoruz
        textComponent.ForceMeshUpdate();
        
        TMP_TextInfo textInfo = textComponent.textInfo;
        int characterCount = textInfo.characterCount;
        
        if (characterCount == 0) return;

        // Yazının toplam genişliğini en sol ve en sağ noktalarından buluyoruz
        float boundsMinX = textComponent.bounds.min.x;
        float boundsMaxX = textComponent.bounds.max.x;
        float width = boundsMaxX - boundsMinX;

        // Her bir harfi tek tek döngüye sokup büküyoruz
        for (int i = 0; i < characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;

            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            // Her harfin 4 köşesi (Vertex'i) vardır
            for (int j = 0; j < 4; j++)
            {
                Vector3 origin = vertices[vertexIndex + j];
                
                // Bu harf, tüm yazının % kaçıncı (satır başından sonuna) kısmında duruyor?
                float pct = (width > 0) ? (origin.x - boundsMinX) / width : 0f;
                
                // O yüzdelik konuma gelen yere AnimationCurve'dan kavis değerini çek
                float yOffset = vertexCurve.Evaluate(pct) * curveMultiplier;
                
                // Harfin Y eksenini (Yukarı/Aşağı) o kavis kadar kaydır
                vertices[vertexIndex + j] = new Vector3(origin.x, origin.y + yOffset, origin.z);
            }
        }

        // Değiştirilmiş / Bükülmüş yeni harf pozisyonlarını ekrana uygula
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}
