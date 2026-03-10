using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BoardEndBoom : MonoBehaviour
{
    [Header("Assign the root GameObject of the chess board (parent of all tiles/pieces)")]
    public GameObject boardRoot;

    [Header("Explosion settings")]
    public float explosionForce = 800f;
    public float explosionRadius = 5f;
    public float upwardsModifier = 0.5f;
    public float extraTorque = 20f;

    // When true, the script will also detach pieces from the board root before applying physics
    public bool detachPieces = true;

    [Header("Post-explosion (scene) transition")]
    public float showDuration = 3f; // how many seconds to wait showing the explosion before scene change
    public string sceneToLoad = "MainMenu"; // name of the scene to load after explosion

    // Keep references to disabled objects for possible future restoration (optional)
    System.Collections.Generic.List<MonoBehaviour> disabledBehaviours = new System.Collections.Generic.List<MonoBehaviour>();
    System.Collections.Generic.List<GameObject> disabledUIObjects = new System.Collections.Generic.List<GameObject>();

    [SerializeField] GameObject checkmatePanel_prefab;
    [SerializeField] GameObject stalematePanel_prefab;
    [SerializeField] Transform pannel_parent;

    // Public method intended to be wired to a UI Button OnClick
    public void TriggerBoom(int parm = 0)
    {
        StartCoroutine(DoBoomAndTransition(parm));
    }

    IEnumerator DoBoomAndTransition(int parm = 0)
    {
        // 0) Disable all UI GameObjects so the screen is clean for the explosion
        GameStreamManager.Instance.ClearMarkerAction();
        var allCanvases = FindObjectsOfType<Canvas>(true);
        foreach (var c in allCanvases)
        {
            // keep this object's canvas if the script itself is placed on a UI element and you want to keep it
            if (c.gameObject == this.gameObject) continue;
            if (c.enabled)
            {
                c.enabled = false;
                disabledUIObjects.Add(c.gameObject);
            }
            else
            {
                // also try disabling root gameobject for non-enabled canvas
                if (c.gameObject.activeSelf)
                {
                    c.gameObject.SetActive(false);
                    disabledUIObjects.Add(c.gameObject);
                }
            }
        }

        // 1) Disable most MonoBehaviours to "pause" gameplay (but keep this script running)
        var allBehaviours = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var mb in allBehaviours)
        {
            if (mb == this) continue; // keep this script active
            // don't disable UI canvases here (already handled) or internal systems that might be required by scene loading
            try
            {
                if (mb.enabled)
                {
                    mb.enabled = false;
                    disabledBehaviours.Add(mb);
                }
            }
            catch { }
        }

        // 2) Determine boardRoot
        if (boardRoot == null)
        {
            var found = GameObject.Find("Board");
            if (found != null) boardRoot = found;
            else
            {
                var byTag = GameObject.FindWithTag("Board");
                if (byTag != null) boardRoot = byTag;
            }
        }

        if (boardRoot == null)
        {
            Debug.LogWarning("BoardEndBoom: boardRoot not assigned and no GameObject named 'Board' or tagged 'Board' found.");
            yield break;
        }

        // 3) Prefer using GameStreamManager's board data to find Unit instances
        var gsm = GameStreamManager.Instance;
        System.Collections.Generic.List<GameObject> targets = new System.Collections.Generic.List<GameObject>();

        if (gsm != null && gsm.board_data != null)
        {
            // board is expected to be an 8x8 array
            for (int x = 0; x < gsm.board_data.board.Count; x++)
            {
                for (int y = 0; y < gsm.board_data.board[x].Count; y++)
                {
                    var tile = gsm.board_data.board[x][y];
                    if (tile == null) continue;
                    var u = tile.unit;
                    if (u != null && u.gameObject != null)
                    {
                        if (!targets.Contains(u.gameObject)) targets.Add(u.gameObject);
                    }
                }
            }
        }

        // Fallback: if no units found via GameStreamManager, fall back to scanning children of boardRoot
        if (targets.Count == 0 && boardRoot != null)
        {
            var pieces = boardRoot.GetComponentsInChildren<Transform>(true);
            foreach (var t in pieces)
            {
                var go = t.gameObject;
                if (go == boardRoot) continue;
                if (!targets.Contains(go)) targets.Add(go);
            }
        }

        // Determine explosion center: prefer boardRoot position, otherwise average of targets, otherwise world origin
        Vector3 center = Vector3.zero;
        if (boardRoot != null) center = boardRoot.transform.position;
        else if (targets.Count > 0)
        {
            foreach (var g in targets) center += g.transform.position;
            center /= Mathf.Max(1, targets.Count);
        }

        // Apply physics to each target GameObject (units)
        foreach (var go in targets)
        {
            // detach if requested so physics acts independently
            if (detachPieces)
            {
                go.transform.SetParent(null, true);
            }

            // Disable MonoBehaviours on this unit/gameobject to stop gameplay scripts from interfering
            var mbs = go.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var mb in mbs)
            {
                if (mb == this) continue;
                try { if (mb.enabled) { mb.enabled = false; disabledBehaviours.Add(mb); } } catch { }
            }

            // Add or configure Rigidbody
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody>();
                rb.mass = Random.Range(0.5f, 3f);
            }
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;

            // Apply explosion force and random torque
            try
            {
                rb.AddExplosionForce(explosionForce * Random.Range(0.8f, 1.2f), center, explosionRadius, upwardsModifier, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * extraTorque, ForceMode.Impulse);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("BoardEndBoom: failed to apply force to " + go.name + " -> " + ex.Message);
            }
        }

        // 4) Make the boardRoot itself dynamic so it also reacts (if provided)
        if (boardRoot != null)
        {
            Rigidbody boardRb = boardRoot.GetComponent<Rigidbody>();
            if (boardRb == null) boardRb = boardRoot.AddComponent<Rigidbody>();
            boardRb.isKinematic = false;
            boardRb.useGravity = true;
            boardRb.constraints = RigidbodyConstraints.None;
            boardRb.AddExplosionForce(explosionForce * 1.2f, center, explosionRadius * 1.5f, upwardsModifier * 0.5f, ForceMode.Impulse);
            boardRb.AddTorque(Random.insideUnitSphere * extraTorque * 2f, ForceMode.Impulse);
        }

        Debug.Log("BoardEndBoom: Triggered — disabled scripts and applied explosion forces.");


        if (parm == 1)
        {

            var obj = Instantiate(checkmatePanel_prefab, pannel_parent);
            var canvas = obj.GetComponentInParent<Canvas>();
            if (canvas != null)
                canvas.enabled = true;
        }
        else if (parm == 2)
        {
            var obj = Instantiate(stalematePanel_prefab, pannel_parent);
            var canvas = obj.GetComponentInParent<Canvas>();
            if (canvas != null)
                canvas.enabled = true;
        }

        // 5) Wait to show the explosion
        yield return new WaitForSeconds(showDuration);

        // 6) Load target scene
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("BoardEndBoom: sceneToLoad is empty — not changing scene.");
        }
    }
}
