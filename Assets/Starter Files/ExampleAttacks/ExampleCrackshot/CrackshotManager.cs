using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrackshotManager : MonoBehaviour
{
    public static CrackshotManager Instance;
    public List<Transform> targets;
    void Start()
    {
        // set up singleton
        if (Instance) Destroy(gameObject);
        Instance = this;
    }

    public float getAngle(Vector2 pos)
    {
        // catch edge case of no targets or deleted targets
        for (int i=targets.Count-1; i>=0; i--)
        {
            if (!targets[i]) targets.RemoveAt(i);
        }
        if (targets.Count == 0) return 0;

        // find closest
        Vector2 closest = targets[0].position;
        float dist = (closest - pos).magnitude;
        for (int i=1; i<targets.Count; i++)
        {
            Vector2 target = targets[i].position;
            float currentDist = (target - pos).magnitude;
            if(currentDist < dist || (currentDist == dist && Random.Range(0f,1f) > .5f))
            {
                closest = target;
                dist = currentDist;
            }
        }

        // calc angle to closest
        float angle = Vector2.Angle(Vector2.right, closest-pos);
        if (closest.y < pos.y) angle *= -1;

        return angle;
    }
}
