using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExampleBoss : MonoBehaviour
{
    public EnemyHealth healthScript;
    public SpriteRenderer sprite;
    public GameObject attackOnePrefab;
    private Attack attackOneScript;
    private float attackOneTimer = -5;
    private int attackOneBurstCounter;
    public GameObject attackTwoPrefab;
    private Attack attackTwoScript;
    private float attackTwoTimer = -5;
    public GameObject player;
    void Start()
    {
        attackOneScript = attackOnePrefab.GetComponent<Attack>();
        attackTwoScript = attackTwoPrefab.GetComponent<Attack>();
    }
    void Update()
    {
        // indicate health
        sprite.color = new Color(1,1,1,healthScript.health / healthScript.maxHealth + .2f);
        if (healthScript.dead) Destroy(gameObject);

        // attack one
        attackOneTimer += Time.deltaTime;
        // wait until time
        if (attackOneTimer > attackOneScript.timeBetweenAttacks)
        {
            // indicate attack start
            attackOneTimer = 0;
            attackOneBurstCounter++;
            // attack in burst
            if (attackOneBurstCounter <= attackOneScript.quantity)
            {
                // aim at player
                float angle = Vector2.Angle(Vector2.right, player.transform.position - transform.position);
                if (player.transform.position.y < transform.position.y) angle *= -1;
                Instantiate(attackOnePrefab.gameObject, transform.position, Quaternion.Euler(0, 0, angle));
            }
            else if (attackOneBurstCounter == attackOneScript.quantity * 4)
            {
                // reset burst
                attackOneBurstCounter = 0;
            }
        }

        // attack two
        attackTwoTimer += Time.deltaTime;
        // wait until time
        if (attackTwoTimer > attackTwoScript.timeBetweenAttacks)
        {
            // indicate attack start
            attackTwoTimer = 0;
            // shoot out a burst
            float angleStep = 360f / attackTwoScript.quantity;
            float initialAngle = Random.Range(0, angleStep);
            for (int i=0; i< attackTwoScript.quantity; i++)
            {
                Instantiate(attackTwoPrefab.gameObject, transform.position, Quaternion.Euler(0,0, initialAngle + i * angleStep));
            }
        }
    }
}
