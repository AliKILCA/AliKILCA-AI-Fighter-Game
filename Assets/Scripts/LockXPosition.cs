using UnityEngine;

public class LockXPosition : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("X pozisyonunu kilitlemek için işaretleyin")]
    public bool lockEnabled = true;
    
    [Header("Debug")]
    public bool showDebugLogs = false;

    private float lockedXPosition;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Başlangıç X pozisyonunu kaydet
        lockedXPosition = transform.position.x;
        
        if (showDebugLogs)
            Debug.Log("[LockX] " + gameObject.name + " - X pozisyonu kilitlendi: " + lockedXPosition);
    }

    void LateUpdate()
    {
        if (!lockEnabled) return;

        // X pozisyonu değiştiyse geri al
        if (Mathf.Abs(transform.position.x - lockedXPosition) > 0.001f)
        {
            Vector3 pos = transform.position;
            pos.x = lockedXPosition;
            transform.position = pos;

            // Rigidbody varsa onu da güncelle
            if (rb != null)
            {
                rb.position = pos;

                // X velocity'yi sıfırla
#if UNITY_6000_0_OR_NEWER
                Vector3 vel = rb.linearVelocity;
                rb.linearVelocity = new Vector3(0f, vel.y, vel.z);
#else
                Vector3 vel = rb.velocity;
                rb.velocity = new Vector3(0f, vel.y, vel.z);
#endif
            }

            if (showDebugLogs)
                Debug.Log("[LockX] " + gameObject.name + " - X pozisyonu düzeltildi");
        }
    }

    // X pozisyonunu yeniden ayarlamak için (gerekirse)
    public void SetLockedPosition(float newX)
    {
        lockedXPosition = newX;
        
        if (showDebugLogs)
            Debug.Log("[LockX] " + gameObject.name + " - Yeni X pozisyonu: " + newX);
    }

    // Mevcut kilitli pozisyonu almak için
    public float GetLockedPosition()
    {
        return lockedXPosition;
    }
}
