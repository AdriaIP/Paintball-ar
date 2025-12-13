using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Tulip menu for the non-raygun hand. Uses long press gestures on different fingers:
/// - Middle finger long press: Opens/closes the config menu
/// - Ring finger long press: Resets sphere color to white
/// - Pinky finger long press: Removes all objects from the DeletableObjects layer
/// 
/// Note: Index finger is reserved for ball grabbing (HandPinch).
/// Automatically swaps hands when HandednessManager changes handedness.
/// Shows visual feedback with text and loading bar while holding.
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
    
    [Tooltip("Reference to the Sphere object to reset color.")]
    public GameObject sphere;
    
    [Tooltip("Default color for the sphere when reset.")]
    public Color defaultSphereColor = Color.white;
    
    [Tooltip("Layer of deletable objects.")]
    public LayerMask deletableLayer;
    
    [Header("Visual Feedback UI")]
    [Tooltip("Canvas for showing the loading feedback (should be world space, child of hand).")]
    public Canvas feedbackCanvas;
    
    [Tooltip("Text to show the action name.")]
    public TextMeshProUGUI actionText;
    
    [Tooltip("Image for the loading bar fill.")]
    public Image loadingBarFill;
    
    [Tooltip("Background image for loading bar.")]
    public Image loadingBarBackground;
    
    [Header("Audio Feedback")]
    [Tooltip("Audio source for feedback sounds.")]
    public AudioSource audioSource;
    
    [Tooltip("Sound to play when action is triggered.")]
    public AudioClip actionSound;
    
    [Header("Debug")]
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
    
    // Action names for display
    private const string ACTION_CONFIG_MENU = "Toggle Menu";
    private const string ACTION_RESET_COLOR = "Reset Color";
    private const string ACTION_DELETE_ALL = "Delete All";
    
    // Track which action is currently being shown
    private string currentActionName = "";
    private float currentProgress = 0f;
    
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
        
        // Hide feedback UI initially
        HideFeedbackUI();
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
        
        // Reset visual feedback tracking
        currentActionName = "";
        currentProgress = 0f;
        
        // Track middle finger (config menu)
        TrackFingerLongPress(
            OVRHand.HandFinger.Middle,
            ref middlePressTime,
            ref middleActionTriggered,
            ToggleConfigMenu,
            ACTION_CONFIG_MENU
        );
        
        // Track ring finger (reset sphere color)
        TrackFingerLongPress(
            OVRHand.HandFinger.Ring,
            ref ringPressTime,
            ref ringActionTriggered,
            ResetSphereColor,
            ACTION_RESET_COLOR
        );
        
        // Track pinky finger (delete all objects)
        TrackFingerLongPress(
            OVRHand.HandFinger.Pinky,
            ref pinkyPressTime,
            ref pinkyActionTriggered,
            DeleteAllObjects,
            ACTION_DELETE_ALL
        );
        
        // Update visual feedback
        if (!string.IsNullOrEmpty(currentActionName) && currentProgress > 0.1f)
        {
            ShowFeedbackUI(currentActionName, currentProgress / longPressDuration);
        }
        else
        {
            HideFeedbackUI();
        }
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
            
            // Track for visual feedback (use the highest progress action)
            if (pressTime > currentProgress)
            {
                currentActionName = actionName;
                currentProgress = pressTime;
            }
            
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
    
    private void ShowFeedbackUI(string actionName, float progress)
    {
        if (feedbackCanvas != null)
        {
            feedbackCanvas.gameObject.SetActive(true);
        }
        
        if (actionText != null)
        {
            actionText.text = actionName;
            actionText.gameObject.SetActive(true);
        }
        
        if (loadingBarFill != null)
        {
            loadingBarFill.fillAmount = Mathf.Clamp01(progress);
            loadingBarFill.gameObject.SetActive(true);
        }
        
        if (loadingBarBackground != null)
        {
            loadingBarBackground.gameObject.SetActive(true);
        }
    }
    
    private void HideFeedbackUI()
    {
        if (feedbackCanvas != null)
        {
            feedbackCanvas.gameObject.SetActive(false);
        }
        
        if (actionText != null)
        {
            actionText.gameObject.SetActive(false);
        }
        
        if (loadingBarFill != null)
        {
            loadingBarFill.gameObject.SetActive(false);
        }
        
        if (loadingBarBackground != null)
        {
            loadingBarBackground.gameObject.SetActive(false);
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
    /// Reset the sphere color to default (white).
    /// </summary>
    public void ResetSphereColor()
    {
        if (sphere != null)
        {
            Renderer renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = defaultSphereColor;
                Debug.Log($"TulipMenu: Sphere color reset to {defaultSphereColor}");
            }
            else
            {
                Debug.LogWarning("TulipMenu: Sphere has no Renderer component!");
            }
        }
        else
        {
            Debug.LogWarning("TulipMenu: Sphere not assigned!");
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
