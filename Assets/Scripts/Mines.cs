using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using BaroqueUI;


public class Mines : MonoBehaviour
{
    public PlayArea playArea;
    public int nbombs;
    public int nx, ny, nz;
    public bool interactions;
    public int remoteBestScore;

    public Material activeMat, defaultMat, probablyBombMat, bubblePopMat, wrongBombMat;

    public static bool clicked_trigger, clicked_touchpad;


    public class Digit
    {
        public Transform prefab;
        public Vector3 center;
        public Bounds bounds;
    }


    Dictionary<Vector3Int, Transform> cells;
    Digit[] digits = new Digit[19];
    HashSet<Vector3Int> bombs;
    List<GameObject> remove_me;


    private IEnumerator Start()
    {
        cells = new Dictionary<Vector3Int, Transform>();

        Vector2 size;
        while (!Baroque.TryGetPlayAreaSize(out size))
            yield return null;

#if false
        /* for testing various play area sizes */
        Vector3 size3 = playArea.transform.GetChild(0).localScale;
        size.x = size3.x;
        size.y = size3.z;
        for (int z = 0; z < nz; z++)
            for (int y = 0; y < ny; y++)
                for (int x = 0;  x< nx; x++)
                    SetCell(new Vector3Int(x,y,z), 10);
#endif

        Vector2 current_size = new Vector2(transform.localScale.x * (nx + 0.7f), transform.localScale.z * (nz + 1));
        float factor = Mathf.Min(size.x / current_size.x, size.y / current_size.y);
        factor *= 0.8f;
        if (factor < 1)
            transform.localScale *= factor;

        Vector3 center = transform.TransformPoint(new Vector3((nx - 1) * 0.5f, 0, nz * 0.5f));
        center.y = 0;
        transform.position -= center;

        yield return null;
        //Shader.WarmupAllShaders();
    }

    public void Populate(Vector3Int pos, Transform copy_rendering, float target_scale)
    {
        Transform tr = Instantiate(playArea.unknownPrefab);
        tr.localScale = Vector3.one * target_scale;
        tr.SetParent(transform, worldPositionStays: true);
        tr.localPosition = pos;

        tr.GetComponent<MeshFilter>().sharedMesh = copy_rendering.GetComponent<MeshFilter>().sharedMesh;
        tr.GetComponent<MeshRenderer>().sharedMaterials = copy_rendering.GetComponent<MeshRenderer>().sharedMaterials;

        var ub = tr.GetComponent<UnknownBox>();
        ub.mines = this;
        ub.position = pos;
        ub.enabled = false;

        cells.Add(pos, tr);

        if (cells.Count == nx * ny * nz)
            StartGame();
    }

    IEnumerable<UnknownBox> GetUnknownBoxes()
    {
        var keys = new List<Vector3Int>(cells.Keys);
        foreach (var pos in keys)
        {
            var unknownbox = GetCellComponent<UnknownBox>(pos);
            if (unknownbox != null)
                yield return unknownbox;
        }
    }

    public void ChangedLights(Vector3Int around_position)
    {
        foreach (var pos0 in Neighbors(around_position))
        {
            var unknownbox = GetCellComponent<UnknownBox>(pos0);
            if (unknownbox == null)
                continue;

            int bits = 0;
            foreach (var pos1 in Neighbors(unknownbox.position))
            {
                var digitbox = GetCellComponent<DigitBox>(pos1);
                if (digitbox != null)
                    bits |= digitbox.EmitExtraLight();
            }
            unknownbox.gameObject.layer = UnknownBox.LAYER0 + bits;
        }
    }

    void StartGame()
    {
        click_neighbors = null;
        interactions = true;
        foreach (var ub in GetUnknownBoxes())
            ub.enabled = true;

        UpdateControllerHints();
    }

    public void UpdateControllerHints()
    {
        foreach (var ctrl in Baroque.GetControllers())
            if (clicked_trigger && clicked_touchpad)
                ctrl.SetControllerHints( /*nothing*/ );
            else
                ctrl.SetControllerHints(trigger: "open", touchpadPressed: "mark");
    }

    public void Unpopulate()
    {
        //playArea.floor.sharedMaterial = playArea.defaultFloorMat;
        playArea.clock.ResetTicking();
        playArea.smokeParticleSys.Stop();
        playArea.successParticleSys.Stop();
        bombs = null;
        List<Vector3Int> keys = new List<Vector3Int>(cells.Keys);
        foreach (var pos in keys)
            SetCell(pos, 0);

        if (remove_me != null)
        {
            foreach (var go in remove_me)
            {
                if (go.GetComponent<Bomb>() == null)
                    go.AddComponent<Bomb>();
                go.GetComponent<Bomb>().disappears = true;
            }
            remove_me = null;
        }
    }

    public Vector3 GetTargetPosition(Vector3Int pos)
    {
        return transform.TransformPoint(pos);
    }

    public void ReadyToEmitExtraLight(Vector3Int pos)
    {
        var digitbox = GetCellComponent<DigitBox>(pos);
        if (digitbox != null)
            digitbox.ReadyToEmitExtraLight();
    }

    void SetCell(Vector3Int pos, int number)
    {
        Transform tr;
        if (cells.TryGetValue(pos, out tr))
        {
            cells.Remove(pos);

            if (tr != null)    /* not destroyed already */
            {
                var unknown = tr.GetComponent<UnknownBox>();
                if (unknown != null)
                    unknown.WinkOut();
                else
                    Destroy(tr.gameObject);
            }
        }

        if (number == 0)
            return;
        Debug.Assert(number > 0);

        var digit = GetPrefabDigit(number);
        Vector3 center = digit.center;

        tr = Instantiate(digit.prefab, transform, worldPositionStays: false);
        const float SCALE = 0.4f;
        tr.localPosition = pos - center * SCALE;
        tr.localRotation = Quaternion.identity;
        tr.localScale = Vector3.one * SCALE;
        cells[pos] = tr;

        var coll = tr.gameObject.AddComponent<BoxCollider>();
        coll.isTrigger = true;
        coll.center = center;

        var digitbox = tr.gameObject.AddComponent<DigitBox>();
        digitbox.mines = this;
        digitbox.position = pos;
    }

    public Digit GetPrefabDigit(int number)
    {
        int index = number;
        if (digits[index] == null)
        {
            var digit = playArea.digitsPrefabs.GetChild(index == 0 ? 18 : index - 1);
            Bounds bounds = digit.GetComponentInChildren<Renderer>().bounds;
            foreach (var rend in digit.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(rend.bounds);
            Vector3 center = digit.InverseTransformPoint(bounds.center);
            digits[index] = new Digit { prefab = digit, bounds = bounds, center = center };
        }
        return digits[index];
    }

    Vector3Int GetPosition(Transform tr)
    {
        foreach (var pair in cells)
        {
            if (pair.Value == tr)
                return pair.Key;
        }
        throw new KeyNotFoundException();
    }

    IEnumerator BombExplodeOrGameSucceeded(Vector3Int? bomb_pos = null)
    {
        interactions = false;
        click_neighbors = null;
        foreach (var ctrl in Baroque.GetControllers())
        {
            if (ctrl.isActiveAndEnabled)
                ctrl.HapticPulse();
            ctrl.SetControllerHints( /*nothing*/ );
        }

        playArea.clock.StopTicking();
        if (bomb_pos != null)
            SetCell(bomb_pos.Value, 0);

        remove_me = new List<GameObject>();

        if (bomb_pos != null)
        {
            Vector3 p0 = transform.TransformPoint(bomb_pos.Value);
            var explosion = Instantiate(playArea.explosionPrefab);
            explosion.rotation = Quaternion.LookRotation(Baroque.GetHeadTransform().position - p0);
            explosion.position = p0 + explosion.forward * 0.05f;

            yield return new WaitForSeconds(0.1f);
            Destroy(explosion.gameObject);

            var bomb = Instantiate(playArea.explodedBombPrefab, transform);
            bomb.position = p0;
            bomb.localScale = Vector3.one;
            remove_me.Add(bomb.gameObject);

            yield return new WaitForSeconds(0.3f);
            playArea.smokeParticleSys.transform.position = p0;
            playArea.smokeParticleSys.Play();

            yield return new WaitForSeconds(0.6f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            var main = playArea.successParticleSys.main;
            main.startColor = activeMat.color;
            /* ^^^ XXX waaaat "main" is a struct, but it's still how you change parameters.  You
             * assign a value to the struct.  There's a magic setter property that will actually
             * have an effect on the original particle system.  Because if it was a normal struct,
             * such a field assignment would be lost after we forget about the 'main' local variable.
             */
            playArea.successParticleSys.Play();
        }
        playArea.selectLevel.SetActive(true);
        if (bomb_pos == null && playArea.clock.seconds > 0)
            playArea.selectLevel.GetComponent<SelectLevel>().WriteNewScore(this, playArea.clock.seconds);

        foreach (var ub in GetUnknownBoxes())
        {
            if (bombs.Contains(ub.position))
            {
                var bomb = Instantiate(playArea.bombPrefab, transform);
                bomb.localPosition = ub.position;
                bomb.localScale = Vector3.one;
                remove_me.Add(bomb.gameObject);

                if (bomb_pos == null)
                {
                    bomb.localScale *= 0.45f;
                    ub.WinkOut();
                }
            }
            else
                if (bomb_pos != null && ub.probablyBomb)
                    ub.WinkOut(show_empty: true);
        }
    }

#if false
    IEnumerator _BombExplode(Vector3Int pos, Transform explosion)
    {
        Vector3 p0 = transform.TransformPoint(pos);

        playArea.smokeParticleSys.transform.position = p0;
        playArea.smokeParticleSys.Play();

        const int NUM = 1000;

        var draw_matrices = new Matrix4x4[NUM];
        var rotate_matrices = new Matrix4x4[NUM];

        float start_time = Time.time;
        for (int i = 0; i < NUM; i++)
        {
            Quaternion q = Random.rotationUniform;
            float s1 = Random.Range(0.25f, 1f);
            Vector3 s = Vector3.one * s1 * s1 * 0.3f;
            draw_matrices[i] = Matrix4x4.TRS(p0, q, s);

            q = Quaternion.LookRotation(Vector3.forward + Random.insideUnitSphere * 0.03f);
            rotate_matrices[i] = Matrix4x4.Rotate(q);

            float t = Mathf.Lerp(start_time, start_time + 0.5f, (i + 1) / (float)NUM);
            while (Time.time < t)
                yield return null;
        }

        start_time = Time.time;
        Baroque.FadeToColor(Color.clear, TIME_ANIMATION);

        var mesh = new Mesh();
        mesh.vertices = new Vector3[] { new Vector3(-1, -1, -1), new Vector3(1, 1, 1) };
        mesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, submesh: 0, calculateBounds: true);
        var material = new Material(playArea.instanciatedColorMaterial);
        material.SetColor("_ColorMin", Color.Lerp(activeMat.color, Color.white, 0.5f));

        var t_next = Time.time;
        var a_prev = 1f;

        while (true)
        {
            float a = (Time.time - start_time) / TIME_ANIMATION;
            if (a >= 1f)
                break;
            a = 1 - a * a;

            Graphics.DrawMeshInstanced(mesh, 0, material, draw_matrices, NUM,
                null, UnityEngine.Rendering.ShadowCastingMode.Off, false);

            while (Time.time >= t_next)
            {
                float a_ratio = a / a_prev;
                a_prev = a;
                Matrix4x4 scale = Matrix4x4.Scale(Vector3.one * a_ratio);
                for (int i = 0; i < NUM; i++)
                    draw_matrices[i] *= rotate_matrices[i] * scale;
                t_next += 1f / 45f;
            }
            yield return null;
        }
#endif

    public void Click(Transform box)
    {
        Vector3Int pos = GetPosition(box);
        if (bombs == null)
        {
            MakeBombs(pos);
            /*foreach (var p1 in Neighbors(pos))
                if (Distance(p1, pos) == 1)
                    Click(p1);*/
            playArea.clock.StartTicking();
        }
        if (bombs.Contains(pos))
        {
            StopAllCoroutines();
            StartCoroutine(BombExplodeOrGameSucceeded(pos));
            return;
        }
        Click(pos);
    }

    bool TryMakeBombs(Vector3Int pos)
    {
        bombs = new HashSet<Vector3Int>();
        for (int i = 0; i < nbombs; i++)
        {
            while (true)
            {
                Vector3Int p1 = new Vector3Int(
                    Random.Range(0, nx),
                    Random.Range(0, ny),
                    Random.Range(0, nz));
                if (Distance(p1, pos) < 3 || bombs.Contains(p1))
                    continue;
                bombs.Add(p1);
                break;
            }
        }

        /* refuse situations close to win-at-first-click.  This is important for the easy mode */
        HashSet<Vector3Int> removes = new HashSet<Vector3Int>();
        List<Vector3Int> pending = new List<Vector3Int>() { pos };
        while (pending.Count > 0)
        {
            Vector3Int p1 = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (p1.x < 0 || p1.y < 0 || p1.z < 0 || p1.x >= nx || p1.y >= ny || p1.z >= nz)
                continue;
            if (!removes.Add(p1))
                continue;
            int number = 0;
            foreach (var n in Neighbors(p1))
                if (bombs.Contains(n))
                    number++;
            if (number == 0)
                pending.AddRange(Neighbors(p1));
        }
        return (removes.Count < cells.Count - bombs.Count * 2);
    }

    void MakeBombs(Vector3Int pos)
    {
        while (!TryMakeBombs(pos))
            /*retry*/ ;
        playArea.selectLevel.SetActive(false);
    }

    static int Distance(Vector3Int p1, Vector3Int p2)
    {
        int dx = p1.x - p2.x; if (dx < 0) dx = -dx;
        int dy = p1.y - p2.y; if (dy < 0) dy = -dy;
        int dz = p1.z - p2.z; if (dz < 0) dz = -dz;
        return dx + dy + dz;
    }

    public IEnumerable<Vector3Int> Neighbors(Vector3Int pos)
    {
        for (int z = pos.z - 1; z <= pos.z + 1; z++)
            for (int y = pos.y - 1; y <= pos.y + 1; y++)
                for (int x = pos.x - 1; x <= pos.x + 1; x++)
                {
                    int distance = Distance(pos, new Vector3Int(x, y, z));
                    if (distance > 0 && distance < 3)
                        yield return new Vector3Int(x, y, z);
                }
    }

    public T GetCellComponent<T>(Vector3Int pos) where T : MonoBehaviour
    {
        Transform tr;
        if (!cells.TryGetValue(pos, out tr) || tr == null)
            return null;
        return tr.GetComponent<T>();   /* may be null */
    }

    HashSet<Vector3Int> click_neighbors;

    void Click(Vector3Int pos)
    {
        if (GetCellComponent<UnknownBox>(pos) != null)
        {
            int number = 0;
            foreach (var n in Neighbors(pos))
                if (bombs.Contains(n))
                    number++;
            SetCell(pos, number);
            Debug.Assert(GetCellComponent<UnknownBox>(pos) == null);

            if (click_neighbors == null)
            {
                click_neighbors = new HashSet<Vector3Int>();
                StartCoroutine(ClickNeighbors());
            }

            if (number == 0)
                foreach (var n in Neighbors(pos))
                    click_neighbors.Add(n);
        }
    }

    IEnumerator ClickNeighbors()
    {
        yield return new WaitForFixedUpdate();

        HashSet<Vector3Int> seen = new HashSet<Vector3Int>();
        List<Vector3Int> next = new List<Vector3Int>();

        while (click_neighbors.Count > seen.Count)
        {
            next = click_neighbors.ToList();
            
            for (int i = next.Count; i >= 1; --i)
            {
                int index = Random.Range(0, i);
                if (seen.Add(next[index]))
                {
                    Click(next[index]);
                    yield return new WaitForFixedUpdate();
                }
                next[index] = next[i - 1];
            }
        }
        click_neighbors = null;

        foreach (var ub in GetUnknownBoxes())
            if (!bombs.Contains(ub.position))
                yield break;   /* game not finished */

        StopAllCoroutines();
        StartCoroutine(BombExplodeOrGameSucceeded());
    }
}
