using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SphereGoingToTarget : MonoBehaviour
{
    public Mines mines;
    public Vector3Int targetInt;

    public Vector3 target;
    public float targetSize;
    public Vector3 velocity;


    private void Start()
    {
        target = mines.GetTargetPosition(targetInt);
        targetSize = mines.transform.lossyScale.y * 0.5f;
        velocity = Random.insideUnitSphere * 3f;
    }

    private void FixedUpdate()
    {
        Vector3 direction = target - transform.position;

        float current_size = transform.localScale.y;
        float deflating = current_size - targetSize;
        if (deflating > 0)
        {
            current_size -= Time.fixedDeltaTime * 0.2f;
            transform.localScale = Vector3.one * current_size;
        }
        else if (direction.sqrMagnitude < 0.02f * 0.02f)
        {
            mines.Populate(targetInt, transform, targetSize);
            Destroy(gameObject);
            return;
        }

        float target_velocity = direction.magnitude * 2.5f + 0.3f;
        if (velocity.magnitude > target_velocity)
            velocity *= target_velocity / velocity.magnitude;

        velocity += direction.normalized * 0.1f;
        Vector3 tv = velocity.normalized;
        velocity = (velocity - tv) * Mathf.Exp(-Time.fixedDeltaTime * 3) + tv;

        transform.position += velocity * Time.fixedDeltaTime;
    }
}
