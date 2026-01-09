using UnityEngine;

public class AlwaysFaceEachOtherByZ : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Transform player1;
    public Transform bot;

    [Header("Y Rotations")]
    public float facePlusZ_Y = 0f;
    public float faceMinusZ_Y = 180f;

    [Header("Deadzone")]
    public float zDeadzone = 0.02f;

    private CharacterHealth player1Health;
    private CharacterHealth botHealth;

    void Start()
    {
        if (player1 != null)
            player1Health = player1.GetComponent<CharacterHealth>();
        
        if (bot != null)
            botHealth = bot.GetComponent<CharacterHealth>();
    }

    void LateUpdate()
    {
        if (!player1 || !bot) return;

        // Ölü karakteri döndürme
        bool player1Dead = player1Health != null && !player1Health.IsAlive();
        bool botDead = botHealth != null && !botHealth.IsAlive();

        float dz = player1.position.z - bot.position.z;

        if (dz > zDeadzone)
        {
            if (!player1Dead) SetY(player1, faceMinusZ_Y);
            if (!botDead) SetY(bot, facePlusZ_Y);
        }
        else if (dz < -zDeadzone)
        {
            if (!player1Dead) SetY(player1, facePlusZ_Y);
            if (!botDead) SetY(bot, faceMinusZ_Y);
        }
    }

    static void SetY(Transform t, float y)
    {
        var e = t.eulerAngles;
        e.y = y;
        t.eulerAngles = e;
    }
}