using UnityEngine;

public class FaceOpponent : MonoBehaviour
{
    [Header("Rakip Ayarı")]
    [Tooltip("Bakılacak rakip karakter")]
    public Transform opponent;

    [Header("Dönüş Ayarları")]
    [Tooltip("Z+ yönüne bakarken Y rotasyonu")]
    public float facingPositiveZ = 0f;
    [Tooltip("Z- yönüne bakarken Y rotasyonu")]
    public float facingNegativeZ = 180f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private bool isFacingPositiveZ = true;

    void Update()
    {
        if (opponent == null) return;

        // Rakibin Z pozisyonu benden büyük mü?
        bool shouldFacePositiveZ = opponent.position.z > transform.position.z;

        // Yön değiştiyse rotasyonu güncelle
        if (shouldFacePositiveZ != isFacingPositiveZ)
        {
            isFacingPositiveZ = shouldFacePositiveZ;
            UpdateRotation();
        }
    }

    private void UpdateRotation()
    {
        float targetYRotation = isFacingPositiveZ ? facingPositiveZ : facingNegativeZ;
        
        Vector3 rotation = transform.eulerAngles;
        rotation.y = targetYRotation;
        transform.eulerAngles = rotation;

        if (showDebugLogs)
            Debug.Log("[FaceOpponent] " + gameObject.name + " -> " + 
                      (isFacingPositiveZ ? "Z+" : "Z-") + " yönüne döndü");
    }

    // Başlangıçta doğru yöne bak
    void Start()
    {
        if (opponent != null)
        {
            isFacingPositiveZ = opponent.position.z > transform.position.z;
            UpdateRotation();
        }
    }
}
