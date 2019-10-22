using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RemovePreload : MonoBehaviour
{
    public ParticleSystem[] prewarmParticleSys;

    IEnumerator Start()
    {
        foreach (var psys in prewarmParticleSys)
        {
            psys.transform.position -= Vector3.up;
            psys.Play();
        }
        yield return new WaitForSeconds(0.3f);

        foreach (var psys in prewarmParticleSys)
        {
            psys.Stop();
            psys.Clear();
            psys.transform.position += Vector3.up;
        }
        Destroy(gameObject);
    }
}
