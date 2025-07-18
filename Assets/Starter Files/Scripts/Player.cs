using UnityEngine;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    /*****************
     * MODIFICATIONS *
     ****************/
    [Header("Movement Modifications")]
    public float inputBufferTime; // how long inputs are held for
    public float coyoteTime; // how long after being ungrounded that you are still considered grounded
    public float hardFallSpeed; // how fast the player must be falling before it's considered a hard fall
    public float topHorizontalSpeed;
    public float jumpSpeed;
    public float gravityScale;
    public float jumpMaxExtensionTime; // how long the jump button can be held to extend a jump
    public float dashSpeed;
    public float dashTime;
    public float dashDowntime; // time between dashes
    public float dashGravityModifier; // the strength of gravity while dashing
    public bool dashInvulnerable; // whether the dash is invulnerable

    [Header("Toggle Modifications")]
    public bool attackWithMouse; // toggles whether attacks are handled by mouse clicks
    public bool enableDash;
    public bool enableDoubleJump;
    public bool enableDieAnim; // toggles whether the player has an animation when dying
    public bool enableAirAttackAnimOne;
    public bool enableAirAttackAnimTwo;

    [Header("Input Modifications")] // NOTE: to change L/R, go to Editor -> Project Settings -> Input Manager and change the axis there
    public KeyCode attackOneKey; // NOTE: only works when attack with mouse is false
    public KeyCode attackTwoKey; // NOTE: only works when attack with mouse is false
    public KeyCode jumpKey;
    public KeyCode downKey; // used for platforms
    public KeyCode dashKey;

    [Header("Attack Prefabs")] // NOTE: make sure these prefabs contain the "Attack.cs" script, and have the player as the transform parent (projectiles are detached in script)
    public GameObject attackOnePrefab;
    public GameObject attackTwoPrefab;
    private Attack attackOneScript;
    private Attack attackTwoScript;

    /**
     * Include an alternate Animation Controller here to allow for assymetry
     * Leave null to just mirror the contents of the sprite
     */
    [Header("Assymetric Player Art")] 
    public RuntimeAnimatorController controllerAlt;
    private RuntimeAnimatorController controllerMain;

    /***********
     * GENERAL *
     **********/
    [Header("Misc")]
    public bool canInput; // toggles the player's ability to input (good for cutscenes and such)

    /*
     * This is a built-in place to add whatever mechanic you would like to the fight.
     * It can be whatever you want, or nothing at all. This is entirely up to you, the jammer.
     */
    public KeyCode specialKey;
    void doSpecialKey()
    {
        // Put code here!
    }

    // player states
    private float bufferedJumpTimer;
    private BufferedAttack bufferedAttack;
    private float bufferTimer;
    private float jumpHoldTimer;
    private float attackTimer;
    private float dashTimer;
    private bool grounded;
    private int groundLayers;
    private int platformLayer;
    private HashSet<Collider2D> disabledPlatforms;
    private ContactFilter2D platformFilter;
    private bool canDoubleJump;
    public enum BufferedAttack
    {
        ONE,
        TWO,
        NONE
    }

    // player components
    public Animator anim;
    public SpriteRenderer sprite;
    public Rigidbody2D rb;
    public Collider2D mainCollider;
    public Collider2D groundCheckCollider;
    public PlayerHealth healthScript;
    public PlayerSounds sound;

    // handle other
    private float dir;

    void Start()
    {
        // set initial values
        dir = 1; 
        groundLayers = LayerMask.GetMask(new string[] { "Ground", "Platform" });
        platformFilter = new ContactFilter2D();
        platformLayer = LayerMask.NameToLayer("Platform");
        rb.gravityScale = gravityScale;
        disabledPlatforms = new HashSet<Collider2D>();
        // grab attack scripts from prefabs
        // make sure to update the attack scripts if the prefabs ever change during runtime!
        attackOnePrefab.TryGetComponent<Attack>(out attackOneScript);
        if (!attackOneScript) Debug.Log("Check your attackOnePrefab for the Attack script");
        attackTwoPrefab.TryGetComponent<Attack>(out attackTwoScript);
        if (!attackTwoScript) Debug.Log("Check your attackTwoPrefab for the Attack script");
        // grab default animator controller if needed
        if (controllerAlt) controllerMain = anim.runtimeAnimatorController;
    }

    void Update()
    {
        // check ground
        checkGround();

        // normal inputs
        if (canInput && dashTimer <= 0)
        {
            doMovement();
            doAttacks();
            doSpecialKey();
        }

        // dashing
        doDash();

        // timers
        handleTimers();
    }

    private void handleTimers()
    {
        bufferTimer -= Time.unscaledDeltaTime;
        bufferedJumpTimer -= Time.unscaledDeltaTime;
        jumpHoldTimer -= Time.deltaTime;
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                doAttackBuffer();
            }
        }
        else
        {
            attackTimer -= Time.deltaTime;
        }
        if (dashTimer > 0)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                anim.SetBool("dashing", false);
            }
        }
        else
        {
            dashTimer -= Time.deltaTime;
        }
    }

    // handle movement/jumping/platforms
    void doMovement()
    {
        // jumping
        doJumping();
        anim.SetBool("upward", rb.velocity.y >= 0);

        // handle L/R
        doHorizontalMovement();

        // platform dropping/reinstating
        reinstatePlatforms();
        if (Input.GetKeyDown(jumpKey) && Input.GetKey(downKey)) disablePlatforms();
    }

    void doHorizontalMovement()
    {
        float horizInput = Input.GetAxis("Horizontal") * topHorizontalSpeed;
        // handle direction
        if ((horizInput > 0.01f && dir == -1) || (horizInput < -0.01f && dir == 1))
        {
            dir = Mathf.Sign(horizInput);
            if (controllerAlt) anim.runtimeAnimatorController = dir == 1 ? controllerMain : controllerAlt;
            transform.localScale = new Vector3(dir * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        // do movement
        rb.velocity = new Vector2(horizInput, rb.velocity.y);
        anim.SetBool("moving", Mathf.Abs(horizInput) > 0.01f);
    }

    // call this through animation triggers
    void doStepSound()
    {
        AudioManager.Instance.PlaySFX(sound.stepSound, sound.stepSoundVol);
    }

    void doJumping()
    {
        // check jump input (and validate)
        if (Input.GetKeyDown(jumpKey) && !Input.GetKey(downKey))
        {
            if (grounded || canDoubleJump)
            {
                AudioManager.Instance.PlaySFX(sound.jumpSound, sound.jumpSoundVol);
                if (!grounded)
                {
                    canDoubleJump = false;
                } else
                {
                    anim.Play("jump");
                }
                jumpHoldTimer = jumpMaxExtensionTime;
            }
            else
            {
                bufferedJumpTimer = inputBufferTime;
            }
        }
        // enable jump to be held
        if (jumpHoldTimer > 0)
        {
            if (Input.GetKey(jumpKey))
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
            }
            else
            {
                jumpHoldTimer = 0;
            }
        }
    }
    
    private void doDash()
    {
        if (enableDash && Input.GetKeyDown(dashKey) && dashTimer <= -dashDowntime)
        {
            dashTimer = dashTime;
            AudioManager.Instance.PlaySFX(sound.dashSound, sound.dashSoundVol);
            anim.Play("dash");
            anim.SetBool("dashing", true);
            rb.velocity = new Vector2(dashSpeed * dir, 0);
        }
        if (dashTimer > 0)
        {
            float yVel = rb.velocity.y + Physics2D.gravity.y * rb.gravityScale * Time.deltaTime * dashGravityModifier;
            rb.velocity = new Vector2(dashSpeed * dir, yVel);
        }
    }

    void checkGround()
    {
        // check if grounded
        bool wasGrounded = grounded;
        // make sure the "ground" is not a disabled platform
        bool nowGrounded = false;
        List<Collider2D> hits = new List<Collider2D>();
        groundCheckCollider.OverlapCollider(platformFilter, hits);
        foreach (Collider2D hit in hits)
        {
            if (((groundLayers>>hit.gameObject.layer)&1) == 0) continue;
            if (!disabledPlatforms.Contains(hit))
            {
                nowGrounded = true;
                break;
            }
        }
        // do logic based on groundedness
        if (nowGrounded)
        {
            // reset grounded var
            grounded = true;
            // do jump if buffered
            if (bufferedJumpTimer > 0 && !Input.GetKey(downKey))
            {
                jumpHoldTimer = jumpMaxExtensionTime;
            }
            // handle double jump
            if (enableDoubleJump) canDoubleJump = true;
            // handle animations/sound
            anim.SetBool("airborne", false);
            if (!wasGrounded)
            {
                if(rb.velocity.y < -hardFallSpeed) AudioManager.Instance.PlaySFX(sound.landSound, sound.landSoundVol);
                if(!healthScript.dead) anim.Play("land");
            }
        } else
        {
            // increment airborne time and set ungrounded
            grounded = false;
            anim.SetBool("airborne", true);
        }
    }

    private void reinstatePlatforms()
    {
        if (disabledPlatforms.Count != 0)
        {
            HashSet<Collider2D> remainingPlatforms;
            remainingPlatforms = new HashSet<Collider2D>();
            Collider2D[] hits = new Collider2D[disabledPlatforms.Count];
            foreach (Collider2D c in disabledPlatforms)
            {
                if (!groundCheckCollider.IsTouching(c))
                {
                    Physics2D.IgnoreCollision(mainCollider, c, false);
                }
                else
                {
                    remainingPlatforms.Add(c);
                }
            }
            disabledPlatforms = remainingPlatforms;
        }
    }

    void disablePlatforms()
    {
        //disable colliders
        List<Collider2D> hits = new List<Collider2D>();
        groundCheckCollider.OverlapCollider(platformFilter, hits);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject.layer != platformLayer) continue;
            Physics2D.IgnoreCollision(mainCollider, hit);
            disabledPlatforms.Add(hit);
        }
    }

    public bool isInvulnerable()
    {
        return dashInvulnerable && dashTimer > 0;
    }

    // handle attacking
    void doAttacks()
    {
        // check inputs
        bool input1;
        bool input2;
        if (attackWithMouse)
        {
            input1 = (attackOneScript.autofire && Input.GetMouseButton(0)) || (!attackOneScript.autofire && Input.GetMouseButtonDown(0));
            input2 = (attackTwoScript.autofire && Input.GetMouseButton(1)) || (!attackTwoScript.autofire && Input.GetMouseButtonDown(1));
        } else
        {
            input1 = (attackOneScript.autofire && Input.GetKey(attackOneKey)) || (!attackOneScript.autofire && Input.GetKeyDown(attackOneKey));
            input2 = (attackTwoScript.autofire && Input.GetKey(attackTwoKey)) || (!attackTwoScript.autofire && Input.GetKeyDown(attackTwoKey));
        }
        // handle attack 1
        if (input1)
        {
            // buffer if needed
            if (attackTimer > 0)
            {
                bufferedAttack = BufferedAttack.ONE;
                bufferTimer = inputBufferTime;
                return;
            }
            // pick attack and execute
            doAttackOne();
        }
        else if(input2)
        {
            // buffer if needed
            if (attackTimer > 0)
            {
                bufferedAttack = BufferedAttack.TWO;
                bufferTimer = inputBufferTime;
                return;
            }
            // pick attack and execute
            doAttackTwo();
        }

    }

    private void doAttackTwo()
    {
        dashTimer = Mathf.Min(dashTimer, 0);
        // set attack delay 
        attackTimer = attackTwoScript.timeBetweenAttacks;
        // spawn object
        Invoke("SpawnAttackTwo", attackTwoScript.spawnDelay);
        // animate
        if (enableAirAttackAnimTwo && !grounded)
        {
            anim.Play("attackTwoAir");
        }
        else
        {
            anim.Play("attackTwo");
        }
    }

    private void doAttackOne()
    {
        dashTimer = Mathf.Min(dashTimer, 0);
        // set attack delay 
        attackTimer = attackOneScript.timeBetweenAttacks;
        // spawn object
        Invoke("SpawnAttackOne", attackOneScript.spawnDelay);
        // animate
        if(enableAirAttackAnimOne && !grounded) 
        {
            anim.Play("attackOneAir");
        } else
        {
            anim.Play("attackOne");
        }
    }

    private void SpawnAttackOne()
    {
        SpawnAttack(attackOneScript, attackOnePrefab, attackOneScript.quantity);
    }
    private void SpawnAttackTwo()
    {
        SpawnAttack(attackTwoScript, attackTwoPrefab, attackTwoScript.quantity);
    }

    private void SpawnAttack(Attack attackScript, GameObject attackPrefab, int quantity)
    {
        for (int i = 0; i < quantity; i++)
        {
            // do initial creation/get script
            GameObject attack = Instantiate(attackPrefab, transform);

            // decouple ranged projectiles from parent transform
            if (attackScript.decoupled)
            {
                attack.transform.parent = null;
                attack.GetComponent<Attack>().dir = dir;
            }

            // handle spread
            if (attackScript.spreadEnabled)
            {
                float evalMid = quantity == 1 ? .5f : i / (quantity - 1f);
                float evalRange = Mathf.Clamp01(attackScript.spreadRandomness) / 2f / quantity;
                float evaluationPoint = Random.Range(Mathf.Clamp01(evalMid - evalRange), Mathf.Clamp01(evalMid + evalRange));
                attack.transform.Rotate(new Vector3(0, 0, dir * attackScript.spread.Evaluate(evaluationPoint)));
            }
        }
    }

    // use attack buffer
    void doAttackBuffer()
    {
        // check buffer
        if (bufferedAttack == BufferedAttack.NONE || bufferTimer <= 0 || !canInput) return;
        if (bufferedAttack == BufferedAttack.ONE) doAttackOne();
        if (bufferedAttack == BufferedAttack.TWO) doAttackTwo();
    }

}

/**
 * TODO
 *      fix bouncy projectile
 *      dying check
 *      enemy health script (boss hitbox)
 *      
 *      Testing
 *      Boss hitbox
 *      Player hitbox
 *      Projectile behaviors
 */