using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Bomb : MonoBehaviour
{
    internal bool disappears;
    Quaternion q_original;
    Vector3 axis;

    private void Start()
    {
        if (disappears)
        {
            axis = Vector3.up;
            q_original = transform.rotation * Quaternion.AngleAxis(-Angle(), axis);
        }
        else
        {
            axis = Random.onUnitSphere;
            q_original = Random.rotationUniform;
            transform.rotation = q_original;
        }
    }

    float Angle()
    {
        return (100 * Time.time) % 360f;
    }

    private void FixedUpdate()
    {
        transform.rotation = q_original * Quaternion.AngleAxis(Angle(), axis);

        float current_scale = transform.localScale.y;
        float new_scale;
        if (disappears)
        {
            new_scale = current_scale - Time.fixedDeltaTime * 2f;
            if (new_scale <= 0f)
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            new_scale = Mathf.Lerp(1f, current_scale, Mathf.Exp(-0.5f * Time.fixedDeltaTime));
        }
        transform.localScale = Vector3.one * new_scale;
    }
}
