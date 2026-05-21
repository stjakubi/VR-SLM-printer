using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Auto-upgrades every Canvas in the scene so the XR controller ray can click
/// its buttons. The default <see cref="GraphicRaycaster"/> only handles the
/// editor mouse — for VR you also need
/// <see cref="TrackedDeviceGraphicRaycaster"/> from the XR Interaction Toolkit.
///
/// Runs automatically after scene load via
/// <see cref="RuntimeInitializeOnLoadMethodAttribute"/>, so no manual hookup
/// is required. Existing canvases that already have a
/// TrackedDeviceGraphicRaycaster are left alone.
///
/// To re-run this on canvases created after scene load (e.g., spawned by
/// runtime UI), call <see cref="UpgradeAllCanvases"/> manually.
/// </summary>
public static class VRCanvasInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
        UpgradeAllCanvases();
    }

    /// <summary>
    /// Adds a TrackedDeviceGraphicRaycaster to every Canvas in the scene that
    /// doesn't already have one. Also ensures each canvas has a
    /// GraphicRaycaster (some hand-built canvases skip it).
    /// </summary>
    public static void UpgradeAllCanvases()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        int upgraded = 0;
        foreach (var canvas in canvases)
        {
            // Make sure there's a GraphicRaycaster first — TrackedDevice
            // raycaster expects one to be present in the chain.
            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();

            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                upgraded++;
            }
        }

        if (upgraded > 0)
            Debug.Log($"[VRCanvasInitializer] Added TrackedDeviceGraphicRaycaster to {upgraded} canvas(es).");
    }
}
