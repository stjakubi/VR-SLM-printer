using UnityEngine;

/// <summary>
/// At scene load, walks every Renderer under "SLM_280_Printer" and adds a
/// MeshCollider to any GameObject that doesn't already have a Collider.
/// These colliders block the XR controller ray at wall surfaces so the
/// player can't read part info through walls / behind geometry.
///
/// Runs automatically — no manual hookup needed. Skips parts that already
/// have any collider (interactables, grabbables, sockets, the removal
/// table, etc. are untouched).
/// </summary>
public static class RayBlockingColliderInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
        GameObject root = GameObject.Find("SLM_280_Printer");
        if (root == null) return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        int added = 0;
        foreach (Renderer r in renderers)
        {
            GameObject go = r.gameObject;

            // Already has a collider — leave it alone (avoids stomping on
            // grabbables, interactables, and ones we explicitly configured).
            if (go.GetComponent<Collider>() != null) continue;

            // Need a mesh to build a MeshCollider against.
            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.convex = false;
            added++;
        }

        if (added > 0)
            Debug.Log($"[RayBlockingColliderInitializer] Added MeshCollider to {added} previously uncollided mesh(es) under SLM_280_Printer.");
    }
}
