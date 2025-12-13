using UnityEngine;

/// <summary>
/// Tulip menu for the non-raygun hand. Uses long press gestures on different fingers:
/// - Middle finger long press: Opens/closes the config menu
/// - Ring finger long press: Resets ball color to first prefab
/// - Pinky finger long press: Removes all objects from the DeletableObjects layer
/// 
/// Note: Index finger is reserved for ball grabbing (HandPinch).
/// Automatically swaps hands when HandednessManager changes handedness.
/// </summary>
public class TulipMenuActions : MonoBehaviour
{
    [Header("Hand References")]
    [Tooltip("Reference to the left OVRHand component.")]
    public OVRHand leftHand;
    
    [Tooltip("Reference to the right OVRHand component.")]
    public OVRHand rightHand;
    
    [Header("Long Press Settings")]
    [Tooltip("How long a finger must be held to trigger the action (in seconds).")]
    public float longPressDuration = 3f;
    
    [Tooltip("Pinch strength threshold to consider a finger as pressed.")]
    [Range(0f, 1f)]
    public float pinchThreshold = 0.7f;
    
    [Header("References")]
    [Tooltip("The config menu GameObject to show/hide.")]
    public GameObject configMenu;
    
    [Tooltip("Reference to RayGun to reset ball color.")]
    public RayGun rayGun;
    
    [Tooltip("Layer of deletable objects.")]
    public LayerMask deletableLayer;
    
    [Header("Feedback")]
    [Tooltip("Audio source for feedback sounds.")]
    public AudioSource audioSource;
    
    [Tooltip("Sound to play when action is triggered.")]
    public AudioClip actionSound;
    
    [Header("Visual Feedback")]
    [Tooltip("Show debug logs for long press progress.")]
    public bool showDebugLogs = false;
    
    // Current hand being used for tulip menu (the non-raygun hand)
    private OVRHand currentHand;
    
    // Long press tracking for each finger
    private float middlePressTime = 0f;
    private float ringPressTime = 0f;
    private float pinkyPressTime = 0f;
    
    private bool middleActionTriggered = false;
    private bool ringActionTriggered = false;
    private bool pinkyActionTriggered = false;
    
    void Start()
    {
        // Subscribe to handedness changes
        if (HandednessManager.Instance != null)
        {
            HandednessManager.Instance.onHandednessChanged.AddListener(OnHandednessChanged);
            UpdateCurrentHand();
        }
        else
        {
            // Default: tulip menu on right hand (raygun on left)
            currentHand = rightHand;
        }
    }
    
    void OnDestroy()
    {
        if (HandednessManager.Instance != null)
        {
            HandednessManager.Instance.onHandednessChanged.RemoveListener(OnHandednessChanged);
        }
    }
    
    void Update()
    {
        if (currentHand == null) return;
        
        // Track middle finger (config menu)
        TrackFingerLongPress(
            OVRHand.HandFinger.Middle,
            ref middlePressTime,
            ref middleActionTriggered,
            ToggleConfigMenu,
            "Config Menu"
        );
        
        // Track ring finger (reset ball color)
        TrackFingerLongPress(
            OVRHand.HandFinger.Ring,
            ref ringPressTime,
            ref ringActionTriggered,
            ResetBallColor,
            "Reset Ball Color"
        );
        
        // Track pinky finger (delete all objects)
        TrackFingerLongPress(
            OVRHand.HandFinger.Pinky,
            ref pinkyPressTime,
            ref pinkyActionTriggered,
            DeleteAllObjects,
            "Delete All Objects"
        );
    }
    
    private void TrackFingerLongPress(
        OVRHand.HandFinger finger,
        ref float pressTime,
        ref bool actionTriggered,
        System.Action action,
        string actionName)
    {
        float pinchStrength = currentHand.GetFingerPinchStrength(finger);
        
        if (pinchStrength > pinchThreshold)
        {
            pressTime += Time.deltaTime;
            
            if (showDebugLogs && pressTime > 0.5f)
            {
                Debug.Log($"TulipMenu: {actionName} progress: {pressTime:F1}/{longPressDuration}s");
            }
            
            // Trigger action when long press duration is reached
            if (pressTime >= longPressDuration && !actionTriggered)
            {
                actionTriggered = true;
                action?.Invoke();
                PlayFeedbackSound();
                Debug.Log($"TulipMenu: {actionName} triggered!");
            }
        }
        else
        {
            // Reset when finger is released
            pressTime = 0f;
            actionTriggered = false;
        }
    }
    
    private void OnHandednessChanged(bool isLeftHanded)
    {
        UpdateCurrentHand();
    }
    
    private void UpdateCurrentHand()
    {
        if (HandednessManager.Instance != null)
        {
            // Tulip menu is on the hand NOT holding the raygun
            // Default (right-handed): Raygun on LEFT, so tulip on RIGHT
            // Left-handed mode: Raygun on RIGHT, so tulip on LEFT
            currentHand = HandednessManager.Instance.isLeftHandedMode ? leftHand : rightHand;
        }
        else
        {
            currentHand = rightHand;
        }
        
        Debug.Log($"TulipMenu: Now using {(currentHand == leftHand ? "LEFT" : "RIGHT")} hand");
    }
    
    /// <summary>
    /// Toggle the config menu visibility.
    /// </summary>
    public void ToggleConfigMenu()
    {
        if (configMenu != null)
        {
            bool newState = !configMenu.activeSelf;
            configMenu.SetActive(newState);
            Debug.Log($"TulipMenu: Config menu {(newState ? "opened" : "closed")}");
        }
        else
        {
            Debug.LogWarning("TulipMenu: Config menu not assigned!");
        }
    }
    
    /// <summary>
    /// Reset the ball color to the first prefab (index 0).
    /// </summary>
    public void ResetBallColor()
    {
        if (rayGun != null)
        {
            rayGun.SetRayImpactPrefab(0);
            Debug.Log("TulipMenu: Ball color reset to default (prefab 0)");
        }
        else
        {
            Debug.LogWarning("TulipMenu: RayGun not assigned!");
        }
    }
    
    /// <summary>
    /// Delete all objects on the deletable layer.
    /// </summary>
    public void DeleteAllObjects()
    {
        // Find all objects on the deletable layer
        int layerIndex = GetLayerFromMask(deletableLayer);
        if (layerIndex < 0)
        {
            Debug.LogWarning("TulipMenu: Deletable layer not set!");
            return;
        }
        
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int deletedCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == layerIndex)
            {
                Destroy(obj);
                deletedCount++;
            }
        }
        
        Debug.Log($"TulipMenu: Deleted {deletedCount} objects from layer {LayerMask.LayerToName(layerIndex)}");
    }
    
    private int GetLayerFromMask(LayerMask mask)
    {
        int layerMaskValue = mask.value;
        for (int i = 0; i < 32; i++)
        {
            if ((layerMaskValue & (1 << i)) != 0)
            {
                return i;
            }
        }
        return -1;
    }
    
    private void PlayFeedbackSound()
    {
        if (audioSource != null && actionSound != null)
        {
            audioSource.PlayOneShot(actionSound);
        }
    }
}
