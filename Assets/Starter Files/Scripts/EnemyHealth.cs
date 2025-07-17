using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{

    [Header("Health Modifications")]
    public float maxHealth;
    public float immunityFrameLength;

    [Header("Hit Flash Effect")]
    public SpriteRenderer[] flashSprites; // Put sprites here that should flash white on hit
    public GameObject flashPrefab;

    // other vars
    [HideInInspector] public float health;
    private float immunityTimer;
    [HideInInspector] public bool dead;

    void Start()
    {
        // put any code here that you need (such as UI stuff)
    }

    void Update()
    {
        // update immunity frames
        if (immunityTimer >= 0) immunityTimer -= Time.deltaTime;
        health = maxHealth;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // get the attack's script and ignore non-attacks
        Attack attackScript;
        collision.TryGetComponent<Attack>(out attackScript);
        if (!attackScript || !attackScript.friendly) return;

        // try damage/immunity
        if (immunityTimer <= 0)
        {
            health -= attackScript.damage;
            if (health < 0)
            {
                die();
            }
            else
            {
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
            GameObject flash = Instantiate(flashPrefab, sprite.transform.parent);
            flash.GetComponent<SpriteRenderer>().sprite = sprite.sprite;
        }
    }

    // Handle game over conditions
    void die()
    {
        // clamp health and let player know to die
        health = 0;
        dead = true;

        // you probably want to put more code here (or somewhere else)
    }
}
