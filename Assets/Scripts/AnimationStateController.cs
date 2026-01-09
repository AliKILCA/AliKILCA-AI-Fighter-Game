using UnityEngine;

public class AnimationStateController : MonoBehaviour
{

    Animator animator;
    float HareketHizi = 0.0f;
    public float acceleration = 2.0f;
    public float deceleration = 2.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        bool forwardPressed = Input.GetKey("d");
        bool backwardPressed = Input.GetKey("a");

        if(forwardPressed)
        {
            HareketHizi += Time.deltaTime * acceleration;
        }
        if(backwardPressed)
        {
            HareketHizi -= Time.deltaTime * acceleration;
        }

        animator.SetFloat("HareketHizi", HareketHizi);
    }
}
