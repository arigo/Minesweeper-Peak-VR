using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using BaroqueUI;


public class SelectLevel : MonoBehaviour
{
    public Transform laserPrefab;
    public float particlesPerSecond;
    public SphereGoingToTarget sgtPrefab;
    public ParticleSystem successParticleSys;


    class Laser
    {
        internal Transform tr;
        internal Quaternion q;
        internal LevelBox level_box;
        internal bool trigger_pressed;
    }
    Laser[] lasers;


    private void Start()
    {
        var gt = Controller.GlobalTracker(this);
        gt.onControllersUpdate += Gt_onControllersUpdate;

#if UNITY_EDITOR
        StartCoroutine(_AutoClick());
#endif
    }

#if UNITY_EDITOR
    IEnumerator _AutoClick()
    {
        yield return new WaitForSeconds(0.75f);
        foreach (var level_box in GetComponentsInChildren<LevelBox>())
            if (level_box.autoClickAtStartup)
                SelectedLevel(level_box, Vector3.zero);
    }
#endif

    private void Gt_onControllersUpdate(Controller[] controllers)
    {
        foreach (var level_box in GetComponentsInChildren<LevelBox>())
            level_box.SetHighlight(0);

        foreach (var ctrl in Baroque.GetControllers())    /* includes inactive ones */
        {
            var laser = ctrl.GetAdditionalData(ref lasers);

            if (!ctrl.isActiveAndEnabled)
            {
                if (laser.tr != null)
                {
                    Destroy(laser.tr.gameObject);
                    laser.tr = null;
                }
            }
            else
            {
                if (laser.tr == null)
                {
                    laser.tr = Instantiate(laserPrefab);
                    laser.q = ctrl.rotation;
                    successParticleSys.trigger.SetCollider(ctrl.index, laser.tr.GetComponentInChildren<Collider>());
                }
                laser.q = Quaternion.Lerp(ctrl.rotation, laser.q, Mathf.Exp(-Time.deltaTime * 50));

                laser.tr.position = ctrl.position;
                laser.tr.rotation = laser.q;

                RaycastHit hitinfo;
                LevelBox level_box = null;
                float distance = 5f;
                if (Physics.SphereCast(laser.tr.position, 0.01f, laser.tr.forward, out hitinfo, 10f,
                                       1 << LevelBox.LAYER, QueryTriggerInteraction.Ignore))
                {
                    level_box = hitinfo.collider.GetComponentInParent<LevelBox>();
                    distance = Vector3.Distance(ctrl.position, hitinfo.point);
                }
                var tr0 = laser.tr.GetChild(0);
                Vector3 p = tr0.localScale;
                p.y = distance * 0.5f;
                tr0.localScale = p;
                p = tr0.localPosition;
                p.z = distance * 0.5f + 0.07f;
                tr0.localPosition = p;

                Color c = level_box == null ? new Color(0.3f, 0.3f, 0.3f) :
                    ctrl.triggerPressed ? new Color(1f, 1f, 1f) : level_box.laserColor;
                laser.tr.GetComponentInChildren<Renderer>().material.color = c;

                if (level_box != null)
                {
                    level_box.SetHighlight((ctrl.triggerPressed | laser.trigger_pressed) ? 2 : 1);

                    if (laser.trigger_pressed && !ctrl.triggerPressed)
                    {
                        SelectedLevel(level_box, hitinfo.point);
                        ctrl.HapticPulse();
                        break;
                    }

                    float cur_time = Time.time;
                    float prev_time = cur_time - Time.deltaTime;

                    int n1 = (int)(prev_time * particlesPerSecond);
                    int n2 = (int)(cur_time * particlesPerSecond);

                    if (n2 > n1)
                    {
                        ParticleSystem.EmitParams emit_params = new ParticleSystem.EmitParams
                        {
                            position = hitinfo.point,
                            applyShapeToPosition = true,
                            startColor = level_box.laserColor,
                        };
                        level_box.particleSys.Emit(emit_params, n2 - n1);
                    }
                    if (laser.level_box != level_box)
                        ctrl.HapticPulse();
                }
                laser.level_box = level_box;
                laser.trigger_pressed = ctrl.triggerPressed;

                ctrl.SetControllerHints(trigger: "choose level");
            }
        }
    }

    void SelectedLevel(LevelBox level_box, Vector3 hitpoint)
    {
        gameObject.SetActive(false);
        foreach (var ctrl in Baroque.GetControllers())
            ctrl.SetControllerHints( /*nothing*/ );

        foreach (var mines1 in FindObjectsOfType<Mines>())
            mines1.Unpopulate();

        var mines = level_box.correspondingMines;

        for (int z = 0; z < mines.nz; z++)
            for (int y = 0; y < mines.ny; y++)
                for (int x = 0; x < mines.nx; x++)
                {
                    var sgt = Instantiate(sgtPrefab);
                    sgt.mines = mines;
                    sgt.targetInt = new Vector3Int(x, y, z);

                    sgt.GetComponent<MeshRenderer>().sharedMaterial = level_box.correspondingMines.defaultMat;

                    sgt.transform.localScale = level_box.particleSys.main.startSize.constant * Vector3.one;
                    sgt.transform.localPosition = hitpoint + Random.insideUnitSphere * 0.5f;
                }

        foreach (var laser in lasers)
            if (laser != null && laser.tr != null)
                Destroy(laser.tr.gameObject);
        lasers = null;
    }

    LevelBox FindLevelBox(Mines mines)
    {
        foreach (var level_box in GetComponentsInChildren<LevelBox>())
            if (level_box.correspondingMines == mines)
                return level_box;
        throw new System.Exception("FindLevelBox failed");
    }

    string GetLocalFileName()
    {
        return System.IO.Path.Combine(Application.persistentDataPath, "Local.txt");
    }

    int LoadLocalBest(int nbombs)
    {
        string[] lines;
        try
        {
            lines = System.IO.File.ReadAllLines(GetLocalFileName());
        }
        catch
        {
            return 0;
        }
        return ParseBest(lines, nbombs);
    }

    int ParseBest(string[] lines, int nbombs)
    {
        string header = nbombs + ": ";
        for (int i = lines.Length - 1; i >= 0; --i)
        {
            string line = lines[i];
            int result;
            if (line.StartsWith(header) && int.TryParse(line.Substring(header.Length), out result))
                return result;
        }
        return 0;
    }

    void SaveLocalBest(int nbombs, int score)
    {
        string[] lines;
        try
        {
            lines = System.IO.File.ReadAllLines(GetLocalFileName());
        }
        catch
        {
            lines = new string[0];
        }
        string new_line = nbombs + ": " + score;
        lines = lines.Concat(new string[] { new_line }).ToArray();
        try
        {
            System.IO.File.WriteAllLines(GetLocalFileName(), lines);
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void WriteNewScore(Mines mines, int score)
    {
        StartCoroutine(_WriteNewScore(mines, score));
    }

    string CalculateMD5Hash(string input)
    {
        // step 1, calculate MD5 hash from input
        System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
        byte[] inputBytes = Encoding.ASCII.GetBytes(input);
        byte[] hash = md5.ComputeHash(inputBytes);

        // step 2, convert byte array to hex string
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
            sb.Append(hash[i].ToString("x2"));
        return sb.ToString();
    }

    IEnumerator _WriteNewScore(Mines mines, int score)
    {
        bool contact_server = (mines.remoteBestScore == 0 || score < mines.remoteBestScore);

        int local_best = LoadLocalBest(mines.nbombs);
        if (local_best == 0 || score < local_best)
        {
            local_best = score;
            SaveLocalBest(mines.nbombs, score);
        }

        var level_box = FindLevelBox(mines);
        var text = level_box.transform.Find("Canvas/Text (1)").GetComponent<UnityEngine.UI.Text>();
        text.fontSize = 50;
        text.text = string.Format("local best: {0} s", local_best);

        if (contact_server)
        {
            string hexdigest = CalculateMD5Hash(mines.nbombs + "SeCrEt" + local_best);
            string url = string.Format("https://vrsketch.eu/minesweeper/score?h={0}", hexdigest);
            WWW www = new WWW(url);
            yield return www;

            if (string.IsNullOrEmpty(www.error))
            {
                string[] lines = www.text.Split('\n');
                foreach (var mines1 in FindObjectsOfType<Mines>())
                    mines1.remoteBestScore = ParseBest(lines, mines1.nbombs);
            }
        }
        if (mines.remoteBestScore > 0 && !text.text.Contains("\n"))
            text.text = string.Format("{0}\nglobal best: {1} s", text.text, mines.remoteBestScore);
    }
}
