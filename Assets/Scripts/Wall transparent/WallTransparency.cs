using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WallTransparency — Attach to your Camera.
///
/// Every frame, casts rays from the camera to each tracked player.
/// Any wall hit along the way becomes translucent (fade alpha).
/// Walls that are no longer blocking are restored to their original material.
///
/// Setup:
///   1. Attach this script to your Main Camera.
///   2. Tag all wall/obstacle objects as "Wall"  (or assign the obstacleLayers mask).
///   3. Tag all player objects as "Player".
///   4. Make sure wall materials use a shader that supports transparency
///      (URP: Universal Render Pipeline/Lit with Surface Type = Transparent,
///       or Standard shader with Rendering Mode = Fade).
///   5. Adjust fadeAlpha and fadeSpeed in the Inspector.
/// </summary>
public class WallTransparency : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Detection")]
    [Tooltip("Layer mask for objects that can block the camera view. " +
             "Set this to only your Wall/Obstacle layer for best performance.")]
    public LayerMask obstacleLayers = ~0; // default: everything

    [Tooltip("Tag used to find player GameObjects automatically")]
    public string playerTag = "Player";

    [Header("Transparency")]
    [Tooltip("Alpha value while the wall is blocking the player (0 = invisible, 1 = solid)")]
    [Range(0f, 1f)]
    public float fadeAlpha = 0.2f;

    [Tooltip("How fast walls fade in/out (higher = snappier)")]
    public float fadeSpeed = 8f;

    [Tooltip("Offset applied to the player position for the ray target " +
             "(raise Y so the ray aims at chest/head, not feet)")]
    public Vector3 playerTargetOffset = new Vector3(0f, 1f, 0f);

    // ── Private ───────────────────────────────────────────────────────────────

    // Tracks every renderer currently faded and its original per-material data
    private class FadeData
    {
        public Renderer  renderer;
        public Material[] originalMaterials;   // cloned originals
        public Material[] fadeMaterials;        // cloned working copies
        public bool       shouldFade;           // set true each frame it blocks
    }

    private Dictionary<Renderer, FadeData> _faded   = new Dictionary<Renderer, FadeData>();
    private List<Renderer>                 _toRestore = new List<Renderer>();
    private Camera                         _cam;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null)
            Debug.LogError("[WallTransparency] This script must be on the Camera GameObject.");
    }

    void LateUpdate()
    {
        // Reset "shouldFade" flags before raycasting this frame
        foreach (var kv in _faded)
            kv.Value.shouldFade = false;

        // Cast rays to every player
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        foreach (GameObject player in players)
            CastRaysToPlayer(player);

        // Fade blockers in, restore non-blockers
        FadeBlockers();
        RestoreNonBlockers();
    }

    void OnDestroy()
    {
        // Restore all materials when script is removed / scene unloads
        foreach (var kv in _faded)
            RestoreRenderer(kv.Value);
        _faded.Clear();
    }

    // ── Core Logic ────────────────────────────────────────────────────────────

    private void CastRaysToPlayer(GameObject player)
    {
        Vector3 origin    = _cam.transform.position;
        Vector3 target    = player.transform.position + playerTargetOffset;
        Vector3 direction = target - origin;
        float   distance  = direction.magnitude;

        // Cast through all obstacles between camera and player
        RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, distance, obstacleLayers);

        foreach (RaycastHit hit in hits)
        {
            // Skip the player itself
            if (hit.collider.gameObject == player) continue;
            if (hit.collider.CompareTag(playerTag))  continue;

            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend == null) continue;

            // Register and mark as needing fade
            if (!_faded.ContainsKey(rend))
                RegisterRenderer(rend);

            _faded[rend].shouldFade = true;
        }
    }

    private void FadeBlockers()
    {
        foreach (var kv in _faded)
        {
            if (!kv.Value.shouldFade) continue;

            foreach (Material mat in kv.Value.fadeMaterials)
                FadeToAlpha(mat, fadeAlpha);
        }
    }

    private void RestoreNonBlockers()
    {
        _toRestore.Clear();

        foreach (var kv in _faded)
        {
            if (kv.Value.shouldFade) continue;

            bool fullyRestored = true;

            foreach (Material mat in kv.Value.fadeMaterials)
            {
                float current = mat.color.a;
                float target  = GetOriginalAlpha(kv.Value, mat);
                float next    = Mathf.MoveTowards(current, target, fadeSpeed * Time.deltaTime);

                SetAlpha(mat, next);

                if (!Mathf.Approximately(next, target))
                    fullyRestored = false;
            }

            if (fullyRestored)
                _toRestore.Add(kv.Key);
        }

        // Remove fully-restored renderers and swap back to original materials
        foreach (Renderer rend in _toRestore)
        {
            RestoreRenderer(_faded[rend]);
            _faded.Remove(rend);
        }
    }

    // ── Material Helpers ──────────────────────────────────────────────────────

    private void RegisterRenderer(Renderer rend)
    {
        FadeData data = new FadeData
        {
            renderer          = rend,
            originalMaterials = rend.materials,        // current shared materials
            fadeMaterials     = CloneMaterials(rend.materials),
            shouldFade        = false
        };

        // Enable transparency on the cloned working materials
        foreach (Material mat in data.fadeMaterials)
            EnableTransparency(mat);

        rend.materials = data.fadeMaterials;
        _faded[rend]   = data;
    }

    private void RestoreRenderer(FadeData data)
    {
        if (data.renderer == null) return;

        data.renderer.materials = data.originalMaterials;

        // Destroy clones to avoid memory leak
        foreach (Material mat in data.fadeMaterials)
            Destroy(mat);
    }

    private Material[] CloneMaterials(Material[] source)
    {
        Material[] clones = new Material[source.Length];
        for (int i = 0; i < source.Length; i++)
            clones[i] = new Material(source[i]);
        return clones;
    }

    /// <summary>Enable transparency on a material — handles URP Lit and Standard shader.</summary>
    private void EnableTransparency(Material mat)
    {
        // ── URP Lit ──────────────────────────────────────────────────────────
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1);        // 0 = Opaque, 1 = Transparent
            mat.SetFloat("_Blend",   0);        // Alpha blend
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",    0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            return;
        }

        // ── Standard shader ──────────────────────────────────────────────────
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 2); // Fade mode
            mat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite",    0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
    }

    private void FadeToAlpha(Material mat, float targetAlpha)
    {
        float current = mat.color.a;
        float next    = Mathf.MoveTowards(current, targetAlpha, fadeSpeed * Time.deltaTime);
        SetAlpha(mat, next);
    }

    private void SetAlpha(Material mat, float alpha)
    {
        Color c = mat.color;
        c.a       = alpha;
        mat.color = c;
    }

    private float GetOriginalAlpha(FadeData data, Material fadeMat)
    {
        // Match the fade material index back to its original
        for (int i = 0; i < data.fadeMaterials.Length; i++)
        {
            if (data.fadeMaterials[i] == fadeMat)
                return data.originalMaterials[i].color.a;
        }
        return 1f;
    }
}
