using UnityEngine;

public class Player : MonoBehaviour
{
    /*****************
     * MODIFICATIONS *
     ****************/
    [Header("Movement Modifications")]
    public float inputBufferTime; // how long inputs are held for
    public float topHorizontalSpeed;
    public bool enableFlipAnim;
    public float jumpSpeed;
    public float jumpMaxExtensionTime; // how long the jump button can be held to extend a jump
    public float dashSpeed;
    public float dashTime;
    public float dashDowntime; // time between dashes
    public float dashGravityModifier; // the strength of gravity when dashing
    public bool dashInvulnerable; // whether the dash is invulnerable

    [Header("Stat Modifications")]
    public float maxHealth;
    public float attackOneTBA; // time between attacks
    public float attackTwoTBA; // time between attacks

    [Header("Toggle Modifications")]
    public bool attackWithMouse; // toggles whether attacks are handled by mouse clicks
    public bool enableDash;
    public bool enableDoubleJump;
    public bool enableSpawnAnim; // toggles whether the player starts out with a spawn animation
    public bool enableDieAnim; // toggles whether the player has an animation when dying
    public bool enableAttackOneAutofire; // toggles whether an input can be held down to perform attack one
    public bool enableAttackTwoAutofire; // toggles whether an input can be held down to perform attack two
    public bool enableAltAttackAnimOne;
    public bool enableAltAttackAnimTwo;
    public bool enableAirAttackAnimOne;
    public bool enableAirAttackAnimTwo;
    public float altAttackWindow; // window in which the alt attack can be done

    [Header("Input Modifications")] // NOTE: to change L/R, go to Editor -> Project Settings -> Input Manager and change the axis there
    public KeyCode attackOneKey; // NOTE: only works when attack with mouse is false
    public KeyCode attackTwoKey; // NOTE: only works when attack with mouse is false
    public KeyCode jumpKey;
    public KeyCode downKey; // used for platforms
    public KeyCode dashKey;

    [Header("Attack Prefabs")] // NOTE: make sure these prefabs contain the "Attack.cs" script, and have the player as the transform parent (projectiles are detached in script)
    public GameObject attackOnePrefab;
    public GameObject attackTwoPrefab;

    /**
     * Include an alternate Animation Controller here to allow for assymetry
     * Leave null to just mirror the contents of the sprite
     */
    [Header("Assymetric Player Art")] 
    public RuntimeAnimatorController controllerAlt;
    private RuntimeAnimatorController controllerMain;

    /*
     * This is a built-in place to add whatever mechanic you would like to the fight.
     * It can be whatever you want, or nothing at all. This is entirely up to you, the jammer.
     */
    public KeyCode specialKey;
    void doSpecialKey()
    {
        // Put code here!
    }

    /***********
     * GENERAL *
     **********/
    [Header("Misc")]
    public bool canInput; // toggles the player's ability to input (good for cutscenes and such)

    // player states
    private float bufferedJumpTimer;
    private BufferedAttack bufferedAttack;
    private BufferedAttack activeAttack;
    private float bufferTimer;
    private float jumpHoldTimer;
    private float attackTimer;
    private float dashTimer;
    private bool grounded;
    private bool platformsDisabled;
    private int groundLayers;
    private int platformLayer;
    private bool canDoubleJump;
    public enum BufferedAttack
    {
        ONE,
        TWO,
        ONEALT,
        TWOALT,
        NONE
    }

    // player components
    public Animator anim;
    public SpriteRenderer sprite;
    public Rigidbody2D rb;
    public Collider2D hitbox;
    public Collider2D physicsCollider;
    public Collider2D groundCheckCollider;

    // handle other
    private float dir;

    void Start()
    {
        // set initial values
        dir = 1; 
        groundLayers = LayerMask.GetMask(new string[] { "Ground", "Platform" });
        platformLayer = LayerMask.NameToLayer("Platform");
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
            if (attackTimer <= -altAttackWindow) activeAttack = BufferedAttack.NONE;
        }
        if (dashTimer > 0)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                if (dashInvulnerable) hitbox.enabled = true;
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
        if (Input.GetKeyDown(jumpKey) && Input.GetKey(downKey)) disablePlatforms(true);
        if (platformsDisabled && !physicsCollider.IsTouchingLayers(platformLayer)) disablePlatforms(false);
    }

    void doHorizontalMovement()
    {
        float horizInput = Input.GetAxis("Horizontal") * topHorizontalSpeed;
        // handle direction
        if ((horizInput > 0.01f && dir == -1) || (horizInput < -0.01f && dir == 1))
        {
            dir = Mathf.Sign(horizInput);
            if(controllerAlt) sprite.flipX = dir == 1 ? true : false;
            transform.localScale = new Vector3(dir * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            //transform.rotation = Quaternion.Euler(0, dir == -1 ? -180 : 0, 0);
            if (grounded && enableFlipAnim) anim.Play("flip");
        }
        // do movement
        rb.velocity = new Vector2(horizInput, rb.velocity.y); //TODO maybe change
        anim.SetBool("moving", Mathf.Abs(horizInput) > 0.01f);
    }

    void doJumping()
    {
        // check jump input (and validate)
        if (Input.GetKeyDown(jumpKey) && !Input.GetKey(downKey))
        {
            if (grounded || canDoubleJump)
            {
                if (!grounded) canDoubleJump = false;
                jumpHoldTimer = jumpMaxExtensionTime;
                anim.Play("jump");
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
            anim.Play("dash");
            anim.SetBool("dashing", true);
            if (dashInvulnerable) hitbox.enabled = false;
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
        if (groundCheckCollider.IsTouchingLayers(groundLayers))
        {
            // reset grounded var
            grounded = true;
            // do jump if buffered
            if(bufferedJumpTimer > 0)
            {
                if (Input.GetKey(downKey))
                {
                    disablePlatforms(true);
                }
                else
                {
                    jumpHoldTimer = jumpMaxExtensionTime;
                }
            }
            // handle double jump
            if (enableDoubleJump) canDoubleJump = true;
            // handle animations
            anim.SetBool("airborne", false);
            if(!wasGrounded) anim.Play("land");
        } else
        {
            grounded = false;
            anim.SetBool("airborne", true);
        }
    }

    void disablePlatforms(bool ignore)
    {
        platformsDisabled = ignore;
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Default"), LayerMask.NameToLayer("Platform"), ignore);
    }

    // handle attacking
    void doAttacks()
    {
        // check inputs
        bool input1;
        bool input2;
        if (attackWithMouse)
        {
            input1 = (enableAttackOneAutofire && Input.GetMouseButton(0)) || (!enableAttackOneAutofire && Input.GetMouseButtonDown(0));
            input2 = (enableAttackTwoAutofire && Input.GetMouseButton(1)) || (!enableAttackTwoAutofire && Input.GetMouseButtonDown(1));
        } else
        {
            input1 = (enableAttackOneAutofire && Input.GetKey(attackOneKey)) || (!enableAttackOneAutofire && Input.GetKeyDown(attackOneKey));
            input2 = (enableAttackTwoAutofire && Input.GetKey(attackTwoKey)) || (!enableAttackTwoAutofire && Input.GetKeyDown(attackTwoKey));
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
        attackTimer = attackTwoTBA;
        // spawn object
        SpawnAttack(attackTwoPrefab, false);
        // animate
        if (!enableAltAttackAnimTwo || activeAttack != BufferedAttack.TWO || (enableAirAttackAnimTwo && !grounded))
        {
            if (enableAirAttackAnimTwo && !grounded)
            {
                anim.Play("attackTwoAir");
            }
            else
            {
                anim.Play("attackTwo");
            }
            activeAttack = BufferedAttack.TWO;
        }
        else
        {
            anim.Play("attackTwoAlt");
            activeAttack = BufferedAttack.TWOALT;
        }
    }

    private void doAttackOne()
    {
        dashTimer = Mathf.Min(dashTimer, 0);
        // set attack delay 
        attackTimer = attackOneTBA;
        // spawn object
        SpawnAttack(attackOnePrefab, true);
        // animate
        if (!enableAltAttackAnimOne || activeAttack != BufferedAttack.ONE || (enableAirAttackAnimOne && !grounded))
        {
            if(enableAirAttackAnimOne && !grounded) 
            {
                anim.Play("attackOneAir");
            } else
            {
                anim.Play("attackOne");
            }
            activeAttack = BufferedAttack.ONE;
        }
        else
        {
            anim.Play("attackOneAlt");
            activeAttack = BufferedAttack.ONEALT;
        }
    }

    private void SpawnAttack(GameObject prefab, bool isAttackOne)
    {
        Attack attackScript;
        prefab.TryGetComponent<Attack>(out attackScript);
        if (!attackScript) Debug.Log("Check your attack" + (isAttackOne ? "One" : "Two") + "Prefab for the Attack script");
        int quantity = attackScript.quantity;
        for(int i=0; i<quantity; i++)
        {
            // do initial creation/get script
            GameObject attack = Instantiate(prefab, transform);

            // decouple ranged projectiles from parent transform
            if (attackScript.decoupled)
            {
                attack.transform.parent = null;
                attack.transform.Rotate(new Vector3(0, 0, dir == -1 ? 180 : 0));
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
 *      Assymetric toggle
 *      maybe get some height on attack (as a toggle)
 *      maybe acceleration for horiz movement (as a toggle)
 *      health script (player hitbox)
 *          immunity frames/dash collider interaction
 *      enemy health script (boss hitbox)
 *      
 *      Testing
 *      Boss hitbox
 *      Player hitbox
 *      Projectile behaviors
 */