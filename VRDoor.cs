using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Physical VR door — grab it and physically push/pull it open like a real door.
/// The door rotates around a hinge, is constrained between closed and open angles,
/// and snaps to fully open or fully closed when released.
///
/// Attach to the DOOR PANEL (not the hinge).
/// The hinge (parent) must be assigned in hingeTransform.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class VRDoor : MonoBehaviour
{
    [Header("Hinge")]
    [Tooltip("The parent pivot object that rotates. Assigned automatically by PrinterStructureBuilder.")]
    public Transform hingeTransform;

    [Header("Angles (hinge local Y rotation)")]
    public float closedAngle =   0f;
    public float openAngle   = -90f;

    [Header("Snap & Feel")]
    [Tooltip("Angle past which the door snaps fully open when released")]
    public float snapThreshold = 40f;

    [Tooltip("How fast the door snaps to open/closed after release")]
    public float snapSpeed = 300f;

    // -----------------------------------------------------------------------
    private XRSimpleInteractable interactable;
    private IXRSelectInteractor  currentInteractor;
    private bool  isGrabbed  = false;
    private bool  isSnapping = false;
    private float snapTarget;

    // Captured at grab time — used to drive the door from the angular delta of
    // the controller around the hinge (not its absolute position), so ray-grabs
    // from a distance behave like a real door.
    private float grabInitialControllerAngle;
    private float grabInitialDoorAngle;
    private bool  grabReferenceValid;

    public bool IsOpen   { get; private set; } = false;
    public bool IsClosed { get; private set; } = true;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        // Kinematic rigidbody — needed for XR interactable but we drive rotation manually
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;
    }

    void Start()
    {
        interactable.selectEntered.AddListener(OnGrabbed);
        interactable.selectExited.AddListener(OnReleased);
    }

    // -----------------------------------------------------------------------
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        currentInteractor = args.interactorObject;
        isGrabbed  = true;
        isSnapping = false;

        // Capture the controller's bearing around the hinge AT THE MOMENT OF GRAB,
        // along with the door's current angle. From here on we drive the door from
        // the *delta* between the live bearing and this captured one, added to the
        // captured door angle. This is what makes ray-grabs from any distance and
        // any standing angle behave intuitively — the door rotates by exactly as
        // many degrees as the player swings the controller around the hinge.
        grabReferenceValid = TryComputeControllerAngleAroundHinge(out grabInitialControllerAngle);
        grabInitialDoorAngle = NormaliseAngle(hingeTransform.localEulerAngles.y);

        Debug.Log("[VRDoor] Grabbed.");
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed          = false;
        currentInteractor  = null;
        grabReferenceValid = false;

        // Decide whether to snap open or closed based on current angle
        float currentAngle = NormaliseAngle(hingeTransform.localEulerAngles.y);
        float openFraction = Mathf.Abs(currentAngle - closedAngle) /
                             Mathf.Abs(openAngle - closedAngle);

        snapTarget = (openFraction > 0.4f) ? openAngle : closedAngle;
        isSnapping = true;

        Debug.Log($"[VRDoor] Released at {currentAngle:F1}° — snapping to {snapTarget}°");
    }

    // -----------------------------------------------------------------------
    void Update()
    {
        if (hingeTransform == null) return;

        if (isGrabbed && currentInteractor != null)
        {
            DriveFromController();
        }
        else if (isSnapping)
        {
            SnapToTarget();
        }
    }

    // -----------------------------------------------------------------------
    // Drives hinge angle based on the angular DELTA of the controller around
    // the hinge since the moment of grab. This makes the door behave the same
    // whether you're holding the handle directly or grabbing from across the
    // room with a ray — every degree you swing the controller around the
    // hinge is one degree the door rotates.
    // -----------------------------------------------------------------------
    private void DriveFromController()
    {
        if (!grabReferenceValid)
        {
            // We failed to capture a reference at grab time (controller was
            // exactly on the hinge). Try again now.
            grabReferenceValid = TryComputeControllerAngleAroundHinge(out grabInitialControllerAngle);
            grabInitialDoorAngle = NormaliseAngle(hingeTransform.localEulerAngles.y);
            if (!grabReferenceValid) return;
        }

        if (!TryComputeControllerAngleAroundHinge(out float currentControllerAngle))
            return;

        // Mathf.DeltaAngle handles the 180/-180 wrap-around correctly.
        float delta = Mathf.DeltaAngle(grabInitialControllerAngle, currentControllerAngle);
        float targetDoorAngle = grabInitialDoorAngle + delta;

        float minAngle = Mathf.Min(closedAngle, openAngle);
        float maxAngle = Mathf.Max(closedAngle, openAngle);
        targetDoorAngle = Mathf.Clamp(targetDoorAngle, minAngle, maxAngle);

        hingeTransform.localEulerAngles = new Vector3(0f, targetDoorAngle, 0f);
    }

    // Returns the bearing of the controller around the hinge axis, measured
    // in the hinge parent's local XZ plane. False if the controller is sitting
    // right on top of the hinge (degenerate case, no defined angle).
    private bool TryComputeControllerAngleAroundHinge(out float angle)
    {
        angle = 0f;
        if (currentInteractor == null || hingeTransform == null) return false;

        Vector3 controllerWorldPos = currentInteractor.GetAttachTransform(interactable).position;
        Transform hingeParent = hingeTransform.parent;
        Vector3 localCtrl = hingeParent != null
            ? hingeParent.InverseTransformPoint(controllerWorldPos)
            : controllerWorldPos;

        Vector3 toController = localCtrl - hingeTransform.localPosition;
        toController.y = 0f;

        if (toController.sqrMagnitude < 0.001f) return false;

        angle = Mathf.Atan2(toController.x, toController.z) * Mathf.Rad2Deg;
        return true;
    }

    // -----------------------------------------------------------------------
    // Smooth snap to open or closed after release
    // -----------------------------------------------------------------------
    private void SnapToTarget()
    {
        float current = NormaliseAngle(hingeTransform.localEulerAngles.y);
        float next    = Mathf.MoveTowards(current, snapTarget, snapSpeed * Time.deltaTime);
        hingeTransform.localEulerAngles = new Vector3(0f, next, 0f);

        if (Mathf.Abs(next - snapTarget) < 0.5f)
        {
            hingeTransform.localEulerAngles = new Vector3(0f, snapTarget, 0f);
            isSnapping = false;

            bool justOpened = Mathf.Approximately(snapTarget, openAngle);
            IsOpen   =  justOpened;
            IsClosed = !justOpened;

            if (justOpened)   OnDoorFullyOpened();
            else              OnDoorFullyClosed();
        }
    }

    // -----------------------------------------------------------------------
    private void OnDoorFullyClosed()
    {
        if (WorkflowManager.Instance != null)
            WorkflowManager.Instance.OnDoorClosed();

        if (VirtualComputer.Instance != null)
            VirtualComputer.Instance.UpdateDoorStatus(isOpen: false);

        if (PrinterErrorSimulator.Instance != null)
            PrinterErrorSimulator.Instance.TriggerContextualError(
                PrinterErrorSimulator.ErrorContext.Door);

        Debug.Log("[VRDoor] Door closed.");
    }

    private void OnDoorFullyOpened()
    {
        if (WorkflowManager.Instance != null)
            WorkflowManager.Instance.OnDoorOpened();

        if (VirtualComputer.Instance != null)
            VirtualComputer.Instance.UpdateDoorStatus(isOpen: true);

        if (ControllerLabelDisplay.Instance != null)
            ControllerLabelDisplay.Instance.ShowPartLabel(
                "Door Open",
                "Wait for the chamber to cool before reaching inside. " +
                "Wear gloves and a dust mask — loose metal powder is a " +
                "respiratory and fire hazard.");

        Debug.Log("[VRDoor] Door opened.");
    }

    // -----------------------------------------------------------------------
    // Normalise Unity's 0–360 euler angle to –180..180
    // -----------------------------------------------------------------------
    private float NormaliseAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnGrabbed);
            interactable.selectExited.RemoveListener(OnReleased);
        }
    }
}
