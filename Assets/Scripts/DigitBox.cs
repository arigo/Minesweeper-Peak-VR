using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class DigitBox : MonoBehaviour
{
    public Mines mines;
    public Vector3Int position;

    bool ready_to_emit_extra_light;
    int extra_light_layer;    /* 0, 1, 2 or 3: bit mask corresponding to both controllers */


    private void Start()
    {
        var ht = Controller.HoverTracker(this);
        ht.onEnter += Ht_onEnter;
        ht.onLeave += Ht_onLeave;
    }

    private void Ht_onEnter(Controller controller)
    {
        Ht_onLeave(controller);
        if (!mines.interactions)
            return;

        int bit = (1 << controller.index);  /* 1 or 2 */

        var light = Instantiate(mines.playArea.spotLight, transform);
        light.transform.localPosition = Vector3.zero;
        light.color = mines.activeMat.color;
        /* light.renderMode is set to "Important" in the prefab ("LightRenderMode.ForcePixel" in
         * code).  This is essential: otherwise, the default value of Auto will be interpreted
         * in editor mode as ForcePixel, but in a build it will turn into ForceVertex.
         * The latter means specular reflections are turned off, and that's almost all of
         * the effect of these lights, so it looks like the lights don't work at all in a build.
         */
        light.cullingMask = (1 << (UnknownBox.LAYER0 + bit)) |     /* bit 13 or 14 */
                            (1 << (UnknownBox.LAYER0 + 3));        /* bit 15 */
        extra_light_layer |= bit;
        mines.ChangedLights(position);
    }

    private void Ht_onLeave(Controller controller)
    {
        int bit = (1 << controller.index);  /* 1 or 2 */
        if ((extra_light_layer & bit) != 0)
        {
            extra_light_layer &= ~bit;
            Destroy(GetComponentInChildren<Light>().gameObject);
            mines.ChangedLights(position);
        }
    }

    public void ReadyToEmitExtraLight()
    {
        ready_to_emit_extra_light = true;
        if (extra_light_layer != 0)
            mines.ChangedLights(position);
    }

    public int EmitExtraLight()
    {
        if (ready_to_emit_extra_light)
            return extra_light_layer;
        return 0;
    }
}
