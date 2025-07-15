using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    /* Determines what behavior a projectile follows (customizable)
     * 0 - default
     * 1 - bounce example
     * 2 - crackshot example
     */
    [Header("Projectile Mode")]
    public int ai = 0;
    public GameObject[] otherObjects;
    // contains objects for this attack to spawn (good for splitting, explosions, etc.)
    // also good for storing child objects

    /*
     * Modifiers
     * These are controlled by animation curves to allow for greater customization
     * All curves will only be read from 0 to 1
     * Time based will change the respective stat based on the percent of lifespan that has passed
     * Range based will pick a random value on the curve from 0 to 1
     */
    [Header("Normal Stat Modifications")]
    public AnimationCurve speed; // time based
    public bool speedEnabled;
    public AnimationCurve wavinessAmplitude; // time based
    public AnimationCurve wavinessFrequency; // time based (value is in hertz)
    public bool wavinessEnabled;
    public AnimationCurve curviness; // time based (in units of full rotations)
    public bool curvinessEnabled;
    public AnimationCurve size; // time based
    public bool sizeEnabled;
    public AnimationCurve gravity; // time based
    public bool gravityEnabled; 
    public AnimationCurve spread; // range based
    public bool spreadEnabled;
    public float lifespan;
    public int quantity;
    public float damage;
    public float timeBetweenAttacks;
    public bool autofire; // toggles whether the attack can be held down to fire

    // components
    [Header("Other Components")]
    public bool friendly;
    public bool piercing;
    public bool decoupled;
    public float spreadRandomness; // toggles whether projectiles distributed evenly across the curve or randomly positioned (0 for even, 1 for fully random)
    public Collider2D hitbox;
    public SpriteRenderer sprite;
    public Animator anim;
    public bool hasDeathAnim; // set this to true if an animation exists in the animator titled "die"
    public ParticleSystem particles; // particles to spawn on death (optional)
    public float deathWaitTime; // how long the projectile waits before being deleted from the scene

    // internal components
    private float time;
    private float verticalVelocity;
    private bool destroyed;
    private LayerMask groundMask;
    [HideInInspector] public static int HITBOX_HIT = 0;
    [HideInInspector] public static int GROUND_HIT = 1;

    void Start()
    {
        groundMask = LayerMask.NameToLayer("Ground");
        // add a custom startup for different ais
        switch (ai)
        {
            default:
                break;
        }
    }

    void Update()
    {
        // increment time
        time += Time.deltaTime;

        // end projectile by time
        if (!destroyed && time > lifespan)
        {
            destroyProjectile();
        }

        // handle dead projectile
        if (destroyed)
        {
            if (time > deathWaitTime) Destroy(gameObject);
            return;
        }

        // allow for custom projectile behavior
        switch (ai)
        {
            default:
                doAI0();
                break;
        }

        doAI0();
    }

    private void doAI0()
    {
        // evaluate curves
        float progress = time / lifespan;
        // speed
        if (speedEnabled) transform.Translate(Vector3.right * speed.Evaluate(progress) * Time.deltaTime);
        // waviness
        if (wavinessEnabled)
        {
            float waveAmpFinal = Time.deltaTime * wavinessAmplitude.Evaluate(progress);
            float waveEval = time * wavinessFrequency.Evaluate(progress) * Mathf.PI * 2;
            transform.Translate(Vector3.up * Mathf.Cos(waveEval) * waveAmpFinal);
        }
        // curviness
        if (curvinessEnabled) transform.Rotate(Vector3.forward * curviness.Evaluate(progress) * Time.deltaTime * 360);
        // size
        if (sizeEnabled) transform.localScale = Vector3.one * size.Evaluate(progress);

        // do gravity
        if (gravityEnabled)
        {
            verticalVelocity -= gravity.Evaluate(progress) * Time.deltaTime;
            transform.Translate(0, verticalVelocity * Time.deltaTime, 0, Space.World);
        }
    }

    public void tryDestroy(int source, Collider2D hit = null)
    {
        // trigger a destruction for non-piercing projectiles (triggered by hits w/ player or walls)
        switch (ai)
        {
            case 1:
                // bounce on floor
                if(source == GROUND_HIT)
                {
                    // check if the collission was vertical
                    Collider2D checkCollider = otherObjects[0].GetComponent<Collider2D>();
                    List<Collider2D> hits = new List<Collider2D>();
                    ContactFilter2D dummy = new ContactFilter2D();
                    checkCollider.enabled = true;
                    bool isBounce = false;
                    Physics2D.OverlapBox(checkCollider.bounds.center, checkCollider.bounds.size, 0, dummy, hits);
                    foreach (Collider2D c in hits)
                    {
                        if (c == hit) isBounce = true;
                    }
                    Debug.DrawLine(checkCollider.bounds.center + checkCollider.bounds.size / 2, checkCollider.bounds.center - checkCollider.bounds.size / 2, Color.red, 3);
                    checkCollider.enabled = false;

                    // do logic
                    if (isBounce)
                    {
                        // bounce
                        verticalVelocity = Mathf.Abs(verticalVelocity) * .95f;
                        // move a bit to prevent multi-hits
                        transform.Translate(0, verticalVelocity * Time.deltaTime, 0, Space.World); 
                    } else
                    {
                        // hit wall and die
                        destroyProjectile();
                    }
                }
                break;
            default:
                if (!piercing) destroyProjectile();
                break;
        }
    }

    void destroyProjectile()
    {
        // allow for custom death behavior (like explosions)
        switch (ai)
        {
            default:
                break;
        }

        // place object into non-colliding mode for any despawn animations/effects
        destroyed = true;
        if(hitbox) hitbox.enabled = false;
        if (hasDeathAnim)
        {
            anim.Play("die");
        } else
        {
            sprite.enabled = false;
        }
        if (particles) particles.Play();
        time = 0;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // destroy projectiles on contact with any ground
        if (collision.gameObject.layer != groundMask) return;
        tryDestroy(GROUND_HIT, collision);
    }
}
