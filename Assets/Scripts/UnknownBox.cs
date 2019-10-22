using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class UnknownBox : MonoBehaviour
{
    public const int LAYER0 = 12;

    public bool probablyBomb;
    public Vector3Int position;
    public Mines mines;

    bool _hover, _removing;
    float base_size;


    void Start()
    {
        var ht = Controller.HoverTracker(this);
        ht.onEnter += Ht_onEnter;
        ht.onLeave += Ht_onLeave;
        ht.onTriggerDown += Ht_onTriggerDown;
        ht.onTouchPressDown += Ht_onTouchPressDown;
	}

    bool Interactive()
    {
        return !_removing && mines.interactions;
    }

    private void Ht_onTouchPressDown(Controller controller)
    {
        if (!Interactive())
            return;
        probablyBomb = !probablyBomb;
        UpdateMaterial();
        if (base_size == 0)
            base_size = transform.localScale.y;
        StartCoroutine(_Boing());

        Mines.clicked_touchpad = true;
        mines.UpdateControllerHints();
    }

    private void Ht_onTriggerDown(Controller controller)
    {
        if (probablyBomb || !Interactive())
            return;
        mines.Click(transform);

        Mines.clicked_trigger = true;
        mines.UpdateControllerHints();
    }

    private void Ht_onEnter(Controller controller)
    {
        _hover = true;
        UpdateMaterial();
        controller.HapticPulse(200);
    }

    private void Ht_onLeave(Controller controller)
    {
        _hover = false;
        UpdateMaterial();
    }

    public void UpdateMaterial()
    {
        if (!Interactive())
            return;

        Material mat;

        if (probablyBomb)
            mat = mines.probablyBombMat;
        else if (_hover)
            mat = mines.activeMat;
        else
            mat = mines.defaultMat;
        GetComponent<Renderer>().sharedMaterial = mat;
    }

    IEnumerator _Boing()
    {
        for (int i = 1; i <= 25; i++)
        {
            float x = i / 25f;    /* 0 - 1 */
            x = x * (1 - x);      /* 0 - 1/4 - 0 */
            x *= 0.4f;            /* 0 - 1/10 - 0 */
            transform.localScale = Vector3.one * (1 - x) * base_size;
            yield return new WaitForFixedUpdate();
        }
    }

    public void WinkOut(bool show_empty = false)
    {
        _removing = true;
        StopAllCoroutines();
        StartCoroutine(_WinkOut(show_empty));
    }

    IEnumerator _WinkOut(bool show_empty)
    {
        Material material = show_empty ? mines.wrongBombMat : mines.bubblePopMat;
        material = new Material(material);
        GetComponent<MeshRenderer>().sharedMaterials = new Material[] { material };

        Color base_color = material.color;
        float base_size = transform.localScale.y;
        float alpha = 1f;
        while (true)
        {
            if (!show_empty)
            {
                alpha -= 0.032f;
                if (alpha <= 0f)
                    break;
            }
            else
                alpha = 0.55f + Mathf.Sin(Time.time * 6f) * 0.4f;

            transform.localScale = Vector3.one * (1 + (1 - alpha) * 0.2f) * base_size;
            base_color.a = alpha;
            material.color = base_color;

            yield return new WaitForFixedUpdate();
        }

        mines.ReadyToEmitExtraLight(position);
        Destroy(gameObject);
        Destroy(material);
    }
}
