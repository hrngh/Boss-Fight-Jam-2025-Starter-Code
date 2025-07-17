using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{

    [Header("Health Modifications")]
    public float maxHealth;
    private float immunityFrameLength;

    // other vars
    private Player playerScript;
    private float health;
    private float immunityTimer;
    [HideInInspector] public bool dead;

    void Start()
    {
        // grab player script
        TryGetComponent<Player>(out playerScript);
        if (!playerScript) Debug.Log("Check that all PlayerHealths' gameobjects have an associated player");

    }

    void Update()
    {
        // update immunity frames
        if(immunityTimer >= 0) immunityTimer -= Time.deltaTime;
        health = maxHealth;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // get the attack's script and ignore non-attacks
        Attack attackScript;
        collision.TryGetComponent<Attack>(out attackScript);
        if (!attackScript || attackScript.friendly || playerScript.isInvulnerable()) return;

        // try damage/immunity
        if(immunityTimer <= 0)
        {
            health -= attackScript.damage;
            if (health < 0)
            {
                die();
            }
            immunityTimer += immunityFrameLength;
        }

        // destroy projectile (if possible)
        attackScript.tryDestroy(Attack.HitType.hitbox);

    }

    // Handle game over conditions
    void die()
    {
        // clamp health and let player know to die
        health = 0;
        dead = true;
        playerScript.canInput = false;
        // play death anim if enabled
        if (playerScript.enableDieAnim) playerScript.anim.Play("die");

        // you probably want to put more code here
    }
}
