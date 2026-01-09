using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    private static BackgroundMusic instance;

    void Awake()
    {
        // Zaten varsa kendini yok et
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Tekil instance oluştur
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}