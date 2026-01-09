using UnityEngine;

public class FightingGameCameraController : MonoBehaviour
{
    [Header("Hedef ve Ayarlar")]
    public Transform player1;
    public Transform player2;

    [Header("Kamera Pozisyonu")]
    [Tooltip("Kameranın X pozisyonu (sabit - karakterlerin yanından bakar)")]
    public float cameraX = 10f;
    [Tooltip("Kameranın Y yüksekliği")]
    public float cameraY = 2f;
    [Tooltip("Takip yumuşaklığı")]
    public float smoothSpeed = 5f;

    [Header("Zoom Ayarları")]
    [Tooltip("Oyuncular yakınken kamera X mesafesi")]
    public float minCameraDistance = 8f;
    [Tooltip("Oyuncular uzakken kamera X mesafesi")]
    public float maxCameraDistance = 15f;
    [Tooltip("Zoom için minimum oyuncu mesafesi")]
    public float minPlayerDistance = 2f;
    [Tooltip("Zoom için maksimum oyuncu mesafesi")]
    public float maxPlayerDistance = 10f;

    [Header("Arena Sınırları")]
    public float minZ = -15f;
    public float maxZ = 15f;

    void Start()
    {
        if (player1 == null || player2 == null)
        {
            Debug.LogError("Player 1 veya Player 2 atanmamış!");
            enabled = false;
            return;
        }

        // Kamerayı Z eksenine bakacak şekilde döndür
        transform.rotation = Quaternion.Euler(0f, -90f, 0f);
    }

    void LateUpdate()
    {
        if (player1 == null || player2 == null) return;

        // 1. İki oyuncunun ortasını bul
        float midZ = (player1.position.z + player2.position.z) / 2f;
        
        // 2. Arena sınırlarını uygula
        midZ = Mathf.Clamp(midZ, minZ, maxZ);

        // 3. İki oyuncu arasındaki mesafeyi hesapla
        float playerDistance = Mathf.Abs(player1.position.z - player2.position.z);

        // 4. Mesafeye göre kamera uzaklığını ayarla (Zoom)
        float t = Mathf.InverseLerp(minPlayerDistance, maxPlayerDistance, playerDistance);
        float currentCameraDistance = Mathf.Lerp(minCameraDistance, maxCameraDistance, t);

        // 5. Hedef pozisyonu hesapla
        Vector3 targetPosition = new Vector3(currentCameraDistance, cameraY, midZ);

        // 6. Yumuşak geçiş
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}