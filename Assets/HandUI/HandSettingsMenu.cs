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
    public Toggle featureToggle;
    public Slider valueSlider;
    public Text sliderValueText;
    
    [Header("Events")]
    [Tooltip("Called when toggle value changes.")]
    public UnityEvent<bool> onToggleChanged;
    
    [Tooltip("Called when slider value changes.")]
    public UnityEvent<float> onSliderChanged;
    
    void Start()
    {
        if (featureToggle != null)
            featureToggle.onValueChanged.AddListener(OnToggleChanged);
        
        if (valueSlider != null)
        {
            valueSlider.onValueChanged.AddListener(OnSliderChanged);
            UpdateSliderText(valueSlider.value);
        }
    }
    
    void OnDestroy()
    {
        if (featureToggle != null)
            featureToggle.onValueChanged.RemoveListener(OnToggleChanged);
        
        if (valueSlider != null)
            valueSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }
    
    private void OnToggleChanged(bool isOn)
    {
        Debug.Log($"Toggle changed: {isOn}");
        onToggleChanged?.Invoke(isOn);
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
