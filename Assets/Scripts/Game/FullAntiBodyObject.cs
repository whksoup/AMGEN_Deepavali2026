using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class FullAntiBodyObject : MonoBehaviour
{
    private GameController owner;
    private float seekSpeed;
    private float turnRate;
    private float detectRadius;
    private float lifetime;
    private int killScore;
    private LayerMask virusMask;
    private TYPE targetType;           // <<< color to hunt

    private Rigidbody rb;
    private Transform target;
    private float findTimer;
    private const float findInterval = 0.25f;
    private float lifeTimer;

    public void Init(GameController owner, float seekSpeed, float turnRate, float detectRadius,
                     float lifetime, int killScore, LayerMask virusMask, TYPE targetType) // <<< added param
    {
        this.owner = owner;
        this.seekSpeed = seekSpeed;
        this.turnRate = turnRate;
        this.detectRadius = detectRadius;
        this.lifetime = lifetime;
        this.killScore = killScore;
        this.virusMask = virusMask;
        this.targetType = targetType; // <<< store color
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;

        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnEnable()
    {
        findTimer = 0f;
        lifeTimer = 0f;
    }

    private void FixedUpdate()
    {
        lifeTimer += Time.fixedDeltaTime;
        if (lifeTimer >= lifetime)
        {
            transform.DOScale(0f, 0.15f).OnComplete(() => Destroy(gameObject));
            enabled = false;
            return;
        }

        // reacquire nearest SAME-COLOR virus
        findTimer -= Time.fixedDeltaTime;
        if (findTimer <= 0f || target == null)
        {
            target = FindNearestVirusOfSameType();
            findTimer = findInterval;
        }

        if (target != null)
        {
            Vector3 desired = (target.position - transform.position).normalized * seekSpeed;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desired, turnRate * Time.fixedDeltaTime);

            if (rb.linearVelocity.sqrMagnitude > 0.01f)
                transform.forward = Vector3.Slerp(transform.forward, rb.linearVelocity.normalized, turnRate * Time.fixedDeltaTime);
        }
        else
        {
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, 1.5f * Time.fixedDeltaTime);
        }
    }

    private Transform FindNearestVirusOfSameType()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position, detectRadius, virusMask, QueryTriggerInteraction.Collide);

        float bestDist = float.MaxValue;
        Transform best = null;

        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (!col) continue;

            // robustly get the GameItem even if collider is on a child
            var gi = col.GetComponentInParent<GameItem>();
            if (gi == null) continue;
            if (gi.ItemType != targetType) continue;        // <<< color filter

            float d = (gi.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = gi.transform;
            }
        }
        return best;
    }

    private void OnTriggerEnter(Collider other)
    {
        // only viruses (layer mask)…
        if (((1 << other.gameObject.layer) & virusMask.value) == 0) return;

        // …of the same color
        var gi = other.GetComponentInParent<GameItem>();
        if (gi == null || gi.ItemType != targetType) return;

        // add score once
        if (owner != null) 
            owner.UpdateScore(killScore, this.transform.position);
        AudioManager.Instance?.PlaySFX("Destory");

        // stop both from moving / colliding again
        var virusRB = gi.GetComponent<Rigidbody>();
        if (virusRB)
        { virusRB.isKinematic = true; virusRB.linearVelocity = Vector3.zero; }
        var virusCols = gi.GetComponentsInChildren<Collider>(true);
        foreach (var c in virusCols)
        {
            if (c) { 
                c.enabled = false; 
            }
        }

        var myRB = GetComponent<Rigidbody>();
        if (myRB) 
        {
            myRB.isKinematic = true; 
            myRB.linearVelocity = Vector3.zero; 
        }
        var myCols = GetComponentsInChildren<Collider>(true);
        foreach (var c in myCols)
        {
            if (c)
            {
                c.enabled = false;
            }
        }
        transform.DOKill(false);
        DOTween.Sequence()
            .Append(transform.DOScale(1.15f, 0.06f))
            .Append(transform.DOScale(1.0f, 0.08f));

        // SHRINK + FADE both, then destroy
        if (owner != null)
        {
            owner.ShrinkFadeAndDestroy(gi.gameObject, 1f);
            owner.ShrinkFadeAndDestroy(gameObject, 1f);
        }
        else
        {
            // fallback, but keep the effect
            var t = transform;
            t.DOScale(0f, 0.35f);
            Destroy(gi.gameObject, 0.36f);
            Destroy(gameObject, 0.36f);
        }
    }
}
