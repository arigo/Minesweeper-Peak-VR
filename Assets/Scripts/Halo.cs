using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class Halo : MonoBehaviour
{
    private void Update()
    {
        Vector3 direction = Baroque.GetHeadTransform().position - transform.position;
        transform.rotation = Quaternion.LookRotation(direction);
    }
}
