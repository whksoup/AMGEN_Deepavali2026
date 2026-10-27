using System.Collections;
using System.Globalization;
using UnityEngine;

public class Flower : MonoBehaviour
{
    Animator animator;
    [SerializeField]
    float timeToClose = 5.0f; // Time in seconds before the flower closes again
    float closeTimer = 0.0f;
    AnimatorStateInfo currentState;

    [SerializeField]
    string text1 = "Flower";
    [SerializeField]
    string text2 = "Flower";

    public string Text1

    {
        get { return text1; }
    }

    public string Text2

    {
        get { return text2; }
    }

    IEnumerator StartAnimationWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        GetComponent<Animator>().SetTrigger("Start");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        float randomOffset = Random.Range(0f, 1f); // normalized time (0–1)
        animator.Play("Idle", -1, randomOffset);

        closeTimer = 0.0f;
        currentState = animator.GetCurrentAnimatorStateInfo(0);
    }

    // Update is called once per frame
    void Update()
    {
        currentState = animator.GetCurrentAnimatorStateInfo(0);
        if(currentState.IsName("Idle Bloom"))
        {
            closeTimer += Time.deltaTime;
            if (closeTimer >= timeToClose)
            {
                animator.SetTrigger("Close");
                closeTimer = 0.0f; // Reset the timer after closing
            }
        }
    }

    public void OnTouched()
    {
        currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (currentState.IsName("Idle"))
        {
            animator.SetTrigger("Touched");
            closeTimer = 0.0f;
        }
    }
}
