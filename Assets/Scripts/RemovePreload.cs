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
        Capture();
        yield return new WaitForSeconds(0.25f);

        foreach (var psys in prewarmParticleSys)
        {
            psys.Stop();
            psys.Clear();
            psys.transform.position += Vector3.up;
        }
        Capture();
        Destroy(gameObject);
    }

    void Capture()
    {
        var tt = RenderTexture.GetTemporary(64, 64);
        GetComponent<Camera>().targetTexture = tt;
        GetComponent<Camera>().Render();
        RenderTexture.ReleaseTemporary(tt);
    }
}
