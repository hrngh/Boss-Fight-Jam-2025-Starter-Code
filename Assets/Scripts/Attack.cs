using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    /* Determines what behavior a projectile follows (customizable)
     * 0 - default
     * 1 - bounce example
     * 2 - splitshot example
     */
    [Header("Projectile Mode")]
    public int ai = 0;
    public GameObject[] childrenPrefabs; // contains objects for this attack to spawn (good for splitting, explosions, etc.)

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

    void Start()
    {

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
            transform.Translate(0, verticalVelocity * Time.deltaTime, 0, Space.World); // gravity
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
}
