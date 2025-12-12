using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Controller for a hand-interactable settings menu.
/// Handles the logic when toggle/slider values change.
/// </summary>
public class HandSettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    public Toggle leftHandedToggle;
    public Slider valueSlider;
    public Text sliderValueText;
    
    [Header("References")]
    [Tooltip("Reference to HandednessManager. If not set, will try to find it.")]
    public HandednessManager handednessManager;
    
    [Header("Events")]
    [Tooltip("Called when slider value changes.")]
    public UnityEvent<float> onSliderChanged;
    
    void Start()
    {
        // Find HandednessManager if not assigned
        if (handednessManager == null)
            handednessManager = FindFirstObjectByType<HandednessManager>();
        
        if (leftHandedToggle != null)
        {
            leftHandedToggle.onValueChanged.AddListener(OnLeftHandedToggleChanged);
            
            // Sync toggle with current handedness state
            if (handednessManager != null)
                leftHandedToggle.isOn = handednessManager.isLeftHandedMode;
        }
        
        if (valueSlider != null)
        {
            valueSlider.onValueChanged.AddListener(OnSliderChanged);
            UpdateSliderText(valueSlider.value);
        }
    }
    
    void OnDestroy()
    {
        if (leftHandedToggle != null)
            leftHandedToggle.onValueChanged.RemoveListener(OnLeftHandedToggleChanged);
        
        if (valueSlider != null)
            valueSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }
    
    private void OnLeftHandedToggleChanged(bool isOn)
    {
        Debug.Log($"Left-handed mode toggle: {isOn}");
        
        if (handednessManager != null)
        {
            handednessManager.SetLeftHandedMode(isOn);
        }
        else
        {
            Debug.LogWarning("HandSettingsMenu: HandednessManager not found!");
        }
    }
    
    private void OnSliderChanged(float value)
    {
        UpdateSliderText(value);
        Debug.Log($"Slider changed: {value}");
        onSliderChanged?.Invoke(value);
    }
    
    private void UpdateSliderText(float value)
    {
        if (sliderValueText != null)
            sliderValueText.text = value.ToString("F1");
    }
}
