using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LightOn : MonoBehaviour
{
    IEnumerator Start() 
    {
        var light = GetComponent<Light>();
        while (light.intensity < 2.69f)
        {
            yield return new WaitForFixedUpdate();
            light.intensity += 0.1f;
        }
    }
}
