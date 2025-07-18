using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{

    [Header("Health Modifications")]
    public float maxHealth;
    public float immunityFrameLength;

    [Header("Hit Flash Effect")]
    public SpriteRenderer[] flashSprites; // Put sprites here that should flash white on hit
    public GameObject flashPrefab;

    [Header("Sounds")] // leave null for no sound
    public AudioClip hurtSound;
    public float hurtSoundVol = 1;
    public AudioClip dieSound;
    public float dieSoundVol = 1;

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
        health = maxHealth;

    }

    void Update()
    {
        // update immunity frames
        if(immunityTimer >= 0) immunityTimer -= Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // get the attack's script and ignore non-attacks
        Attack attackScript;
        collision.TryGetComponent<Attack>(out attackScript);
        if (!attackScript || attackScript.friendly || playerScript.isInvulnerable() || dead) return;

        // try damage/immunity
        if(immunityTimer <= 0)
        {
            health -= attackScript.damage;
            if (health <= 0)
            {
                die();
            } else
            {
                AudioManager.Instance.PlaySFX(hurtSound, hurtSoundVol);
                doSpriteFlash();
            }
            immunityTimer += immunityFrameLength;
        }

        // destroy projectile (if possible)
        attackScript.tryDestroy(Attack.HitType.hitbox);

    }

    void doSpriteFlash()
    {
        foreach (SpriteRenderer sprite in flashSprites)
        {
            GameObject flash = Instantiate(flashPrefab, sprite.transform);
            flash.GetComponent<FlashSprite>().matchSprite = sprite;
        }
    }

    // Handle game over conditions
    void die()
    {
        // clamp health and let player know to die
        health = 0;
        dead = true;
        playerScript.canInput = false;
        // play death anim if enabled
        AudioManager.Instance.PlaySFX(dieSound, dieSoundVol);
        if (playerScript.enableDieAnim) playerScript.anim.Play("die");

        // you probably want to put more code here
    }
}
