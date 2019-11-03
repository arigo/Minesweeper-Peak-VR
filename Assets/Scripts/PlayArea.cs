using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayArea : MonoBehaviour
{
    public Transform digitsPrefabs;
    public Transform unknownPrefab;
    public Clock clock;
    public GameObject selectLevel;
    public ParticleSystem smokeParticleSys, successParticleSys;
    public Transform explosionPrefab;
    public Transform bombPrefab, explodedBombPrefab;
    public Light spotLight;
}
