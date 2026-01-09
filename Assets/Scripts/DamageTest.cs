using UnityEngine;

public class DamageTest : MonoBehaviour
{
    [Header("Test Ayarları")]
    public KeyCode damageKey = KeyCode.K;
    public KeyCode killKey = KeyCode.L;
    public float testDamage = 10f;

    private CharacterHealth health;

    void Start()
    {
        health = GetComponent<CharacterHealth>();
    }

    void Update()
    {
        if (health == null) return;

        // K tuşu - 10 hasar ver
        if (Input.GetKeyDown(damageKey))
        {
            health.TakeDamage(testDamage);
            Debug.Log("[Test] Hasar verildi: " + testDamage);
        }

        // L tuşu - Anında öldür
        if (Input.GetKeyDown(killKey))
        {
            health.TakeDamage(health.currentHealth);
            Debug.Log("[Test] Karakter öldürüldü!");
        }
    }
}
