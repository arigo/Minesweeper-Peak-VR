using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class LevelBox : MonoBehaviour
{
    public const int LAYER = 9;

    public Color laserColor;
    public Mines correspondingMines;
    public ParticleSystem particleSys;
    public bool autoClickAtStartup;

    public void SetHighlight(int highlight)
    {
        var panel = GetComponentInChildren<UnityEngine.UI.Image>();
        var color = panel.color;
        color.a = 0.75f + 0.25f * highlight;
        panel.color = color;

        Color col = highlight == 2 ? Color.white : Color.black;
        GetComponentInChildren<MeshRenderer>().material.SetColor("_EmissionColor", col);
    }
}
