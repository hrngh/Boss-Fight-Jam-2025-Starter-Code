using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashSprite : MonoBehaviour
{
    // Copies a sprite but with a fading white
    // Destroys itself when it turns transparent
    public AnimationCurve alpha;
    public SpriteRenderer sprite;
    public SpriteRenderer matchSprite;
    private float time;
    void Start()
    {
        sprite.color = Color.white;
    }

    void Update()
    {
        time += Time.deltaTime;
        // do color
        sprite.color = new Color(1, 1, 1, alpha.Evaluate(time));
        if (sprite.color.a <= 0) Destroy(gameObject);
        // do sprite
        if (matchSprite)
        {
            sprite.sprite = matchSprite.sprite;
            sprite.flipX = matchSprite.flipX;
        } else
        {
            Destroy(gameObject);
        }
    }
}
