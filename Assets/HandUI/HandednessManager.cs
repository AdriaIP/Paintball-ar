using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages left-handed mode. When toggled, swaps the hand references for ball grabbing and ray gun.
/// This is a singleton that other scripts can reference.
/// </summary>
public class HandednessManager : MonoBehaviour
{
    public static HandednessManager Instance { get; private set; }
    
    [Header("Hand References")]
    [Tooltip("The left OVRHand component.")]
    public OVRHand leftHand;
    
    [Tooltip("The right OVRHand component.")]
    public OVRHand rightHand;
    
    [Tooltip("The left hand anchor transform.")]
    public Transform leftHandAnchor;
    
    [Tooltip("The right hand anchor transform.")]
    public Transform rightHandAnchor;
    
    [Tooltip("The left hand object (for velocity tracking).")]
    public GameObject leftHandObject;
    
    [Tooltip("The right hand object (for velocity tracking).")]
    public GameObject rightHandObject;
    
    [Header("Scripts to Update")]
    [Tooltip("Reference to HandPinch script (ball grabbing).")]
    public HandPinch handPinch;
    
    [Tooltip("Reference to DetachableRayGun script.")]
    public DetachableRayGun detachableRayGun;
    
    [Tooltip("Reference to RayGun script.")]
    public RayGun rayGun;
    
    [Header("State")]
    [Tooltip("If true, left hand is used for ray gun and right hand for ball. If false, it's reversed.")]
    public bool isLeftHandedMode = false;
    
    [Header("Events")]
    public UnityEvent<bool> onHandednessChanged;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    void Start()
    {
        // Apply initial handedness
        ApplyHandedness();
    }
    
    /// Toggle between left-handed and right-handed mode.
    public void ToggleHandedness()
    {
        SetLeftHandedMode(!isLeftHandedMode);
    }
    
    public void SetLeftHandedMode(bool leftHanded)
    {
        isLeftHandedMode = leftHanded;
        ApplyHandedness();
        onHandednessChanged?.Invoke(isLeftHandedMode);
        
        Debug.Log($"HandednessManager: Left-handed mode is now {(isLeftHandedMode ? "ON" : "OFF")}");
    }
    
    /// Apply the current handedness to all relevant scripts.
    /// Default (right-handed): Left hand = RayGun, Right hand = Ball
    /// Left-handed mode: Right hand = RayGun, Left hand = Ball
    private void ApplyHandedness()
    {
        if (isLeftHandedMode)
        {
            // Left-handed mode: RayGun on RIGHT hand, Ball on LEFT hand
            ApplyRayGunHand(rightHand, rightHandAnchor, rightHandObject, true);
            ApplyBallHand(leftHand, leftHandObject);
        }
        else
        {
            // Right-handed mode (default): RayGun on LEFT hand, Ball on RIGHT hand
            ApplyRayGunHand(leftHand, leftHandAnchor, leftHandObject, false);
            ApplyBallHand(rightHand, rightHandObject);
        }
    }
    
    private void ApplyRayGunHand(OVRHand hand, Transform anchor, GameObject handObj, bool isRightHand)
    {
        // Update DetachableRayGun
        if (detachableRayGun != null)
        {
            bool wasAttached = detachableRayGun.isAttached;
            
            detachableRayGun.hand = hand;
            detachableRayGun.handAnchor = anchor;
            detachableRayGun.isRightHand = isRightHand;
            
            // If the gun was attached, re-attach to the new hand
            if (wasAttached)
            {
                detachableRayGun.AttachToHand();
            }
            else
            {
                // Reset grab state so the new hand can grab it
                detachableRayGun.ResetGrabState();
            }
        }
        
        // Update RayGun
        if (rayGun != null)
        {
            rayGun.leftHand = hand;
            rayGun.leftHandObject = handObj;
        }
    }
    
    private void ApplyBallHand(OVRHand hand, GameObject handObj)
    {
        // Update HandPinch (ball grabbing)
        if (handPinch != null)
        {
            handPinch.rightHand = hand;
            handPinch.rightHandObject = handObj;
        }
    }
}
