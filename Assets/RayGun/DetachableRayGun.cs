using UnityEngine;

/// <summary>
/// Makes the RayGun detachable from the hand. When held, it follows the hand and the menu is visible.
/// When released (hand opens wide), it falls with physics. Can be grabbed again with a pinch gesture.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class DetachableRayGun : MonoBehaviour
{
    [Header("Hand References")]
    [Tooltip("Reference to the OVRHand component for the left hand.")]
    public OVRHand hand;
    
    [Tooltip("The hand anchor transform to attach to when held.")]
    public Transform handAnchor;
    
    [Header("Grab Settings")]
    [Tooltip("Index and middle finger pinch strength threshold to grab the ray gun.")]
    [Range(0f, 1f)]
    public float grabThreshold = 0.25f;
    
    [Tooltip("How close the hand needs to be to grab the ray gun (in meters). Set to 0.3-0.5 for easy grabbing.")]
    public float grabDistance = 0.4f;
    
    [Tooltip("Index and middle fingers must be below this threshold to trigger release (open hand gesture).")]
    [Range(0f, 1f)]
    public float openHandThreshold = 0.1f;
    
    [Header("Attached Position")]
    [Tooltip("Local position offset when attached to hand.")]
    public Vector3 attachedPositionOffset = Vector3.zero;
    
    [Tooltip("Local rotation offset when attached to hand (Euler angles).")]
    public Vector3 attachedRotationOffset = Vector3.zero;
    
    [Header("Safety Settings")]
    [Tooltip("Minimum time after attaching before release is allowed (prevents accidental release).")]
    public float releaseDelay = 0.5f;
    
    [Tooltip("If the gun falls below this Y position, it will respawn at hand. Set to a very low value to disable.")]
    public float minYPosition = -10f;
    
    [Tooltip("Layer mask for surfaces the gun can collide with when dropped.")]
    public LayerMask collisionLayers;
    
    [Header("Menu Reference")]
    [Tooltip("The radial menu GameObject to show/hide based on attachment state.")]
    public GameObject radialMenuObject;
    
    [Header("RayGun Reference")]
    [Tooltip("Reference to the RayGun component. If not set, will try to find it on this GameObject.")]
    public RayGun rayGun;
    
    [Header("State")]
    [Tooltip("Is the ray gun currently attached to the hand?")]
    public bool isAttached = true;
    
    [Tooltip("Set to true when using right hand (for mirrored rotation).")]
    public bool isRightHand = false;
    
    private Rigidbody rb;
    private bool wasGrabbing = false;
    private float attachTime = 0f;
    private Vector3 previousHandPosition;
    private Vector3 handVelocity;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rayGun == null)
            rayGun = GetComponent<RayGun>();
    }
    
    void Start()
    {
        // Start attached to hand
        if (isAttached)
        {
            AttachToHand();
        }
        else
        {
            DetachFromHand();
        }
        
        if (handAnchor != null)
            previousHandPosition = handAnchor.position;
    }
    
    void Update()
    {
        if (hand == null || handAnchor == null) return;
        
        // Track hand velocity for throwing
        handVelocity = (handAnchor.position - previousHandPosition) / Time.deltaTime;
        previousHandPosition = handAnchor.position;
        
        if (isAttached)
        {
            // Follow the hand with mirrored rotation for right hand
            Vector3 posOffset = attachedPositionOffset;
            Vector3 rotOffset = attachedRotationOffset;
            
            if (isRightHand)
            {
                // Mirror position on X axis and flip Y rotation for right hand
                //posOffset.x = -posOffset.x;
                //rotOffset.y = -rotOffset.y;
                //rotOffset.z = -rotOffset.z;
            }
            
            transform.position = handAnchor.position + handAnchor.TransformDirection(posOffset);
            transform.rotation = handAnchor.rotation * Quaternion.Euler(rotOffset);
            
            // Check if hand opens wide to release (with delay to prevent accidental release)
            if (Time.time - attachTime > releaseDelay && IsHandWideOpen())
            {
                DetachFromHand();
            }
        }
        else
        {
            // Check if gun fell too far and needs to respawn
            if (transform.position.y < minYPosition)
            {
                Debug.Log("DetachableRayGun: Gun fell too far, respawning at hand");
                AttachToHand();
                return;
            }
            
            // Check if we can grab it (all fingers closed)
            float distanceToHand = Vector3.Distance(transform.position, handAnchor.position);
            
            // Debug.Log($"Distance: {distanceToHand:F2}m, GrabDistance: {grabDistance:F2}m, HandClosed: {IsHandClosed()}, WasGrabbing: {wasGrabbing}");
            
            bool isGrabbing = IsHandClosed() && distanceToHand < grabDistance;
            
            // Grab on fist close (when wasn't grabbing before)
            if (isGrabbing && !wasGrabbing)
            {
                // Debug.Log("DetachableRayGun: Grabbing!");
                AttachToHand();
            }
            
            wasGrabbing = isGrabbing;
        }
    }
    
    private bool IsHandClosed()
    {
        if (hand == null) return false;
        
        // Check that index and middle finger pinch strengths are above the threshold
        float indexPinch = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        float middlePinch = hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle);
        
        // Debug log to see actual values
        // Debug.Log($"Middle pinch: {middlePinch:F2}, Index pinch: {indexPinch:F2}, Threshold: {grabThreshold:F2}");

        // Both main fingers must be closed (pinching)
        bool fingersClosed = indexPinch > grabThreshold && middlePinch > grabThreshold;
        
        return fingersClosed;
    }
    
    private bool IsHandWideOpen()
    {
        if (hand == null) return false;
        
        // Check that index and middle finger pinch strengths are below the threshold
        // (these are the ones that register when closing hand)
        float indexPinch = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        float middlePinch = hand.GetFingerPinchStrength(OVRHand.HandFinger.Middle);
        
        // Both main fingers must be open (not pinching)
        bool fingersOpen = indexPinch < openHandThreshold && middlePinch < openHandThreshold;
        
        return fingersOpen;
    }
    
    public void AttachToHand()
    {
        isAttached = true;
        attachTime = Time.time;
        wasGrabbing = true; // Reset grab state to prevent immediate re-grab detection
        
        // Disable physics
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // Position at hand with mirrored rotation for right hand
        if (handAnchor != null)
        {
            Vector3 posOffset = attachedPositionOffset;
            Vector3 rotOffset = attachedRotationOffset;
            
            if (isRightHand)
            {
                // Mirror position on X axis and flip Y rotation for right hand
                posOffset.x = -posOffset.x;
                rotOffset.y = -rotOffset.y;
                rotOffset.z = -rotOffset.z;
            }
            
            transform.position = handAnchor.position + handAnchor.TransformDirection(posOffset);
            transform.rotation = handAnchor.rotation * Quaternion.Euler(rotOffset);
            previousHandPosition = handAnchor.position; // Reset velocity tracking
        }
        
        // Enable the RayGun functionality
        if (rayGun != null)
            rayGun.enabled = true;
        
        // Show the radial menu
        if (radialMenuObject != null)
            radialMenuObject.SetActive(true);
        
        // Debug.Log("DetachableRayGun: Attached to hand");
    }
    
    public void DetachFromHand()
    {
        isAttached = false;
        
        // Enable physics
        rb.isKinematic = false;
        rb.useGravity = true;
        
        // Give it a slight velocity based on hand movement for realism
        rb.linearVelocity = handVelocity;
        
        // Disable the RayGun functionality while detached
        if (rayGun != null)
            rayGun.enabled = false;
        
        // Hide the radial menu
        if (radialMenuObject != null)
            radialMenuObject.SetActive(false);
        
        // Reset grab state
        wasGrabbing = false;
        
        // Debug.Log("DetachableRayGun: Detached from hand");
    }
    
    /// <summary>
    /// Resets the grab detection state. Call this when swapping hands to allow
    /// the new hand to grab the gun immediately.
    /// </summary>
    public void ResetGrabState()
    {
        wasGrabbing = false;
        if (handAnchor != null)
        {
            previousHandPosition = handAnchor.position;
        }
    }
}
