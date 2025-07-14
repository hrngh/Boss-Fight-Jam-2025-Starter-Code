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
        // get the attack's script and ignore invalid attacks
        Attack attackScript;
        collision.TryGetComponent<Attack>(out attackScript);
        if (!attackScript || attackScript.friendly || playerScript.isInvulnerable()) return;

        // try damage/immunity
        if(immunityTimer <= 0)
        {
            health -= attackScript.damage;
            if (health < 0) health = 0;
            immunityTimer += immunityFrameLength;
        }

        // destroy projectile (if possible)
        attackScript.tryDestroy();

    }
}
