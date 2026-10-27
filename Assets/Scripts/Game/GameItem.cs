using UnityEngine;
using DG.Tweening;

public class GameItem : MonoBehaviour
{
    [SerializeField]
    int score = 10;

    [SerializeField]
    TYPE itemType;
    public TYPE ItemType => itemType;

    [Header("Fade On Touch")]
    [SerializeField] float firstTouchAlpha = 0.6f;
    [SerializeField] float fadeDuration = 0.15f;

    [SerializeField]
    private bool nonClickable = false;

    GameController gameController;
    bool isTouched = false;

    private Vector3 randomAxis;
    private float randomSpeed;

    private Rigidbody rb;
    private MeshCollider meshCollider;
    Renderer[] renderers;

    void OnEnable()
    {
        // Pick a random axis (anywhere on the unit sphere)
        randomAxis = Random.onUnitSphere;

        // Pick a random speed (degrees per second)
        randomSpeed = Random.Range(10f, 50f);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        meshCollider = GetComponent<MeshCollider>();
        gameController = GameObject.Find("Game Controller").GetComponent<GameController>();
        renderers = GetComponentsInChildren<Renderer>(includeInactive: true); // NEW
    }

    // Update is called once per frame
    void Update()
    {
        if(isTouched || tag == "Virus" || nonClickable) 
            return;

        // Rotate around that axis
        transform.Rotate(randomAxis, randomSpeed * Time.deltaTime);
    }

    public void OnTouch()
    {
        if (isTouched || tag == "Virus" || nonClickable) 
            return;

        if(tag != "Virus" && !nonClickable)
            AudioManager.Instance?.PlaySFX("Antibody Part Collected");

        isTouched = true;
        FadeAllMaterialsTo(firstTouchAlpha, fadeDuration);

        //Set the rigid body to stop
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        //Close the mesh collider
        if(meshCollider != null )
            meshCollider.enabled = false;

        // score & collection only once
        if (gameController)
        {
            gameController.UpdateScore(score,transform.position);
            gameController.AddToCollected((int)itemType, this);
        }
    }

    void FadeAllMaterialsTo(float targetAlpha, float duration)
    {
        if (renderers == null) return;

        foreach (var r in renderers)
        {
            if (r == null) continue;

            // Use instance materials so we don't affect other objects using the same shared material
            var mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m == null) continue;

                // Prefer URP Lit _BaseColor, else fall back to _Color
                if (m.HasProperty("_BaseColor"))
                {
                    var c = m.GetColor("_BaseColor");
                    m.DOColor(new Color(c.r, c.g, c.b, targetAlpha), "_BaseColor", duration);
                }
                else if (m.HasProperty("_Color"))
                {
                    var c = m.GetColor("_Color");
                    m.DOColor(new Color(c.r, c.g, c.b, targetAlpha), "_Color", duration);
                }
                // else: shader has no color property to fade; skip
            }
        }
    }

}
