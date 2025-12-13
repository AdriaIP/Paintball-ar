using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Controller for a hand-interactable settings menu.
/// Handles the logic when toggle/slider/dropdown values change.
/// </summary>
public class HandSettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    public Toggle leftHandedToggle;
    public Slider valueSlider;
    public Text sliderValueText;
    public TMP_Dropdown dropdown;
    
    [Header("Dropdown Options")]
    [Tooltip("Options to populate the dropdown. Leave empty to populate manually or via code.")]
    public List<string> dropdownOptions = new List<string>();
    
    [Header("References")]
    [Tooltip("Reference to HandednessManager. If not set, will try to find it.")]
    public HandednessManager handednessManager;
    
    [Tooltip("Reference to GenreMusicPlayer for genre selection. Optional.")]
    public GenreMusicPlayer musicPlayer;
    
    [Header("Events")]
    [Tooltip("Called when slider value changes.")]
    public UnityEvent<float> onSliderChanged;
    
    [Tooltip("Called when dropdown selection changes. Passes the selected index.")]
    public UnityEvent<int> onDropdownChanged;
    
    [Tooltip("Called when dropdown selection changes. Passes the selected option text.")]
    public UnityEvent<string> onDropdownTextChanged;
    
    void Start()
    {
        // Find HandednessManager if not assigned
        if (handednessManager == null)
            handednessManager = FindFirstObjectByType<HandednessManager>();
        
        // Find GenreMusicPlayer if not assigned
        if (musicPlayer == null)
            musicPlayer = FindFirstObjectByType<GenreMusicPlayer>();
        
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
        
        if (dropdown != null)
        {
            // Populate dropdown with options
            PopulateDropdown();
            dropdown.onValueChanged.AddListener(OnDropdownChanged);
        }
    }
    
    void OnDestroy()
    {
        if (leftHandedToggle != null)
            leftHandedToggle.onValueChanged.RemoveListener(OnLeftHandedToggleChanged);
        
        if (valueSlider != null)
            valueSlider.onValueChanged.RemoveListener(OnSliderChanged);
        
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }
    
    /// <summary>
    /// Populates the dropdown with options from the list or from the music player's genres.
    /// </summary>
    private void PopulateDropdown()
    {
        if (dropdown == null) return;
        
        dropdown.ClearOptions();
        
        // If we have a music player, populate with genre names
        if (musicPlayer != null && dropdownOptions.Count == 0)
        {
            string[] genreNames = musicPlayer.GetGenreNames();
            dropdown.AddOptions(new List<string>(genreNames));
            dropdown.value = musicPlayer.currentGenreIndex;
        }
        // Otherwise use the manual options list
        else if (dropdownOptions.Count > 0)
        {
            dropdown.AddOptions(dropdownOptions);
        }
    }
    
    /// <summary>
    /// Sets the dropdown options programmatically.
    /// </summary>
    public void SetDropdownOptions(List<string> options)
    {
        dropdownOptions = options;
        PopulateDropdown();
    }
    
    /// <summary>
    /// Sets the dropdown options from a string array.
    /// </summary>
    public void SetDropdownOptions(string[] options)
    {
        dropdownOptions = new List<string>(options);
        PopulateDropdown();
    }
    
    /// <summary>
    /// Sets the selected dropdown index.
    /// </summary>
    public void SetDropdownValue(int index)
    {
        if (dropdown != null && index >= 0 && index < dropdown.options.Count)
        {
            dropdown.value = index;
        }
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
    
    private void OnDropdownChanged(int index)
    {
        string selectedText = dropdown.options[index].text;
        Debug.Log($"Dropdown changed: {index} ({selectedText})");
        
        // If music player is connected, change the genre
        if (musicPlayer != null)
        {
            musicPlayer.SetGenre(index);
        }
        
        onDropdownChanged?.Invoke(index);
        onDropdownTextChanged?.Invoke(selectedText);
    }
    
    private void UpdateSliderText(float value)
    {
        if (sliderValueText != null)
            sliderValueText.text = value.ToString("F1");
    }
}
