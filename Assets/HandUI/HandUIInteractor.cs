using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Allows hand tracking to interact with world-space UI elements (buttons, toggles, sliders).
/// Supports both left and right hand interaction simultaneously.
/// Attach this to your menu canvas.
/// </summary>
public class HandUIInteractor : MonoBehaviour
{
    [Header("Left Hand References")]
    [Tooltip("Reference to the left OVRHand component.")]
    public OVRHand leftHand;
    
    [Tooltip("The left index finger tip transform for poking UI elements.")]
    public Transform leftFingerTip;
    
    [Header("Right Hand References")]
    [Tooltip("Reference to the right OVRHand component.")]
    public OVRHand rightHand;
    
    [Tooltip("The right index finger tip transform for poking UI elements.")]
    public Transform rightFingerTip;
    
    [Header("Interaction Settings")]
    [Tooltip("How close the finger needs to be to interact with UI (in meters).")]
    public float pokeDistance = 0.03f;
    
    [Tooltip("Cooldown between poke interactions to prevent rapid toggling.")]
    public float pokeDebounceTime = 0.5f;
    
    [Header("UI Elements")]
    [Tooltip("List of Toggles that can be poked.")]
    public List<Toggle> toggles = new List<Toggle>();
    
    [Tooltip("List of Buttons that can be poked.")]
    public List<Button> buttons = new List<Button>();
    
    [Tooltip("List of Sliders that can be dragged.")]
    public List<Slider> sliders = new List<Slider>();
    
    [Tooltip("List of Dropdowns that can be poked.")]
    public List<TMP_Dropdown> dropdowns = new List<TMP_Dropdown>();
    
    private float lastPokeTime = 0f;
    private TMP_Dropdown activeDropdown = null;
    private Slider activeSlider = null;
    private Transform activeFingerTip = null; // Which finger is currently interacting
    private PointerEventData pointer;
    
    void Start()
    {
        pointer = new PointerEventData(EventSystem.current);
    }
    
    void Update()
    {
        // PRIORITY 1: Handle active dropdown (blocks all other interactions)
        if (activeDropdown != null)
        {
            HandleActiveDropdown();
            return; // Block all other interactions while dropdown is open
        }
        
        // PRIORITY 2: Handle slider dragging
        if (activeSlider != null)
        {
            UpdateSliderDrag();
            return;
        }
        
        // Try interactions with both hands
        TryInteractWithHand(leftFingerTip);
        TryInteractWithHand(rightFingerTip);
    }
    
    private void TryInteractWithHand(Transform fingerTip)
    {
        if (fingerTip == null) return;
        
        // Check for dropdown open poke FIRST (before other UI elements)
        int pokedDropdownIndex = GetPokedDropdownIndex(fingerTip);
        if (pokedDropdownIndex >= 0 && Time.time - lastPokeTime > pokeDebounceTime)
        {
            TMP_Dropdown dropdown = dropdowns[pokedDropdownIndex];
            dropdown.Show();
            activeDropdown = dropdown;
            activeFingerTip = fingerTip;
            lastPokeTime = Time.time;
            Debug.Log($"HandUIInteractor: Opened dropdown '{dropdown.name}'");
            return;
        }
        
        // Check for toggle pokes
        int pokedToggleIndex = GetPokedToggleIndex(fingerTip);
        if (pokedToggleIndex >= 0 && Time.time - lastPokeTime > pokeDebounceTime)
        {
            Toggle toggle = toggles[pokedToggleIndex];
            toggle.isOn = !toggle.isOn;
            lastPokeTime = Time.time;
            Debug.Log($"HandUIInteractor: Toggle '{toggle.name}' set to {toggle.isOn}");
            return;
        }
        
        // Check for button pokes
        int pokedButtonIndex = GetPokedButtonIndex(fingerTip);
        if (pokedButtonIndex >= 0 && Time.time - lastPokeTime > pokeDebounceTime)
        {
            Button button = buttons[pokedButtonIndex];
            button.onClick.Invoke();
            lastPokeTime = Time.time;
            Debug.Log($"HandUIInteractor: Button '{button.name}' clicked");
            return;
        }
        
        // Check for slider touch (starts dragging)
        int pokedSliderIndex = GetPokedSliderIndex(fingerTip);
        if (pokedSliderIndex >= 0)
        {
            activeSlider = sliders[pokedSliderIndex];
            activeFingerTip = fingerTip;
            Debug.Log($"HandUIInteractor: Started dragging slider '{activeSlider.name}'");
            return;
        }
    }
    
    private void HandleActiveDropdown()
    {
        // Use whichever finger is closer to interact with the dropdown
        Transform fingerTip = GetClosestFingerTip(activeDropdown.GetComponent<RectTransform>().position);
        if (fingerTip == null) fingerTip = activeFingerTip;
        if (fingerTip == null) return;
        
        // Dropdown is open, ONLY check for option selection or closing
        int selectedOption = GetPokedDropdownOption(fingerTip);
        if (selectedOption >= 0 && Time.time - lastPokeTime > pokeDebounceTime)
        {
            activeDropdown.value = selectedOption;
            activeDropdown.Hide();
            lastPokeTime = Time.time;
            Debug.Log($"HandUIInteractor: Selected dropdown option {selectedOption}");
            activeDropdown = null;
            activeFingerTip = null;
            return;
        }
        
        // Check if both fingers are far away to close dropdown
        RectTransform dropdownRect = activeDropdown.GetComponent<RectTransform>();
        if (dropdownRect != null)
        {
            float minDist = GetMinDistanceToDropdown(dropdownRect);
            
            // Only close if both hands are far from the dropdown
            if (minDist > pokeDistance * 15f)
            {
                activeDropdown.Hide();
                activeDropdown = null;
                activeFingerTip = null;
                Debug.Log("HandUIInteractor: Closed dropdown (fingers moved away)");
            }
        }
    }
    
    private float GetMinDistanceToDropdown(RectTransform dropdownRect)
    {
        float minDist = float.MaxValue;
        
        // Find the dropdown list to check distance from it
        Transform dropdownList = activeDropdown.transform.Find("Dropdown List");
        
        // Check left finger
        if (leftFingerTip != null)
        {
            float distToDropdown = Vector3.Distance(leftFingerTip.position, dropdownRect.position);
            if (dropdownList != null)
            {
                float distToList = Vector3.Distance(leftFingerTip.position, dropdownList.position);
                minDist = Mathf.Min(minDist, distToDropdown, distToList);
            }
            else
            {
                minDist = Mathf.Min(minDist, distToDropdown);
            }
        }
        
        // Check right finger
        if (rightFingerTip != null)
        {
            float distToDropdown = Vector3.Distance(rightFingerTip.position, dropdownRect.position);
            if (dropdownList != null)
            {
                float distToList = Vector3.Distance(rightFingerTip.position, dropdownList.position);
                minDist = Mathf.Min(minDist, distToDropdown, distToList);
            }
            else
            {
                minDist = Mathf.Min(minDist, distToDropdown);
            }
        }
        
        return minDist;
    }
    
    private Transform GetClosestFingerTip(Vector3 position)
    {
        float leftDist = leftFingerTip != null ? Vector3.Distance(leftFingerTip.position, position) : float.MaxValue;
        float rightDist = rightFingerTip != null ? Vector3.Distance(rightFingerTip.position, position) : float.MaxValue;
        
        if (leftDist < rightDist && leftFingerTip != null)
            return leftFingerTip;
        if (rightFingerTip != null)
            return rightFingerTip;
        return leftFingerTip;
    }
    
    private int GetPokedToggleIndex(Transform fingerTip)
    {
        if (fingerTip == null) return -1;
        
        for (int i = 0; i < toggles.Count; i++)
        {
            if (toggles[i] == null) continue;
            
            // For toggles, check the background/checkmark area which is usually a child
            RectTransform rt = toggles[i].GetComponent<RectTransform>();
            Transform background = toggles[i].transform.Find("Background");
            if (background != null)
            {
                rt = background.GetComponent<RectTransform>();
            }
            if (rt == null) continue;
            
            float distance = Vector3.Distance(fingerTip.position, rt.position);
            // Debug.Log($"Toggle '{toggles[i].name}' distance: {distance:F3}, threshold: {pokeDistance}");
            if (distance < pokeDistance)
            {
                return i;
            }
        }
        
        return -1;
    }
    
    private int GetPokedButtonIndex(Transform fingerTip)
    {
        if (fingerTip == null) return -1;
        
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null) continue;
            
            RectTransform rt = buttons[i].GetComponent<RectTransform>();
            if (rt == null) continue;
            
            float distance = Vector3.Distance(fingerTip.position, rt.position);
            if (distance < pokeDistance)
            {
                return i;
            }
        }
        
        return -1;
    }
    
    private int GetPokedSliderIndex(Transform fingerTip)
    {
        if (fingerTip == null) return -1;
        
        for (int i = 0; i < sliders.Count; i++)
        {
            if (sliders[i] == null) continue;
            
            // Check distance to the slider handle or the slider itself
            RectTransform handleRect = sliders[i].handleRect;
            RectTransform sliderRect = sliders[i].GetComponent<RectTransform>();
            
            if (handleRect != null)
            {
                float distanceToHandle = Vector3.Distance(fingerTip.position, handleRect.position);
                if (distanceToHandle < pokeDistance)
                {
                    return i;
                }
            }
            
            if (sliderRect != null)
            {
                float distanceToSlider = Vector3.Distance(fingerTip.position, sliderRect.position);
                if (distanceToSlider < pokeDistance * 2f)
                {
                    return i;
                }
            }
        }
        
        return -1;
    }
    
    private void UpdateSliderDrag()
    {
        if (activeSlider == null || activeFingerTip == null) return;
        
        RectTransform sliderRect = activeSlider.GetComponent<RectTransform>();
        if (sliderRect == null) return;
        
        // Check if finger is still near the slider
        float distanceToSlider = Vector3.Distance(activeFingerTip.position, sliderRect.position);
        if (distanceToSlider > pokeDistance * 4f)
        {
            // Finger moved away, stop dragging
            Debug.Log($"HandUIInteractor: Stopped dragging slider '{activeSlider.name}'");
            activeSlider = null;
            activeFingerTip = null;
            return;
        }
        
        // Convert finger position to local slider space
        Vector3 localPoint = sliderRect.InverseTransformPoint(activeFingerTip.position);
        
        // Calculate normalized position along the slider (assuming horizontal slider)
        float sliderWidth = sliderRect.rect.width;
        float normalizedX = (localPoint.x + sliderWidth / 2f) / sliderWidth;
        
        // Clamp and apply value
        normalizedX = Mathf.Clamp01(normalizedX);
        activeSlider.value = Mathf.Lerp(activeSlider.minValue, activeSlider.maxValue, normalizedX);
    }
    
    private int GetPokedDropdownIndex(Transform fingerTip)
    {
        if (fingerTip == null) return -1;
        
        for (int i = 0; i < dropdowns.Count; i++)
        {
            if (dropdowns[i] == null) continue;
            
            RectTransform rt = dropdowns[i].GetComponent<RectTransform>();
            if (rt == null) continue;
            
            float distance = Vector3.Distance(fingerTip.position, rt.position);
            if (distance < pokeDistance)
            {
                return i;
            }
        }
        
        return -1;
    }
    
    private int GetPokedDropdownOption(Transform fingerTip)
    {
        if (fingerTip == null || activeDropdown == null) return -1;
        
        // Find the dropdown list (it's created as a child when dropdown is shown)
        Transform dropdownList = activeDropdown.transform.Find("Dropdown List");
        if (dropdownList == null) return -1;
        
        // Find the content with the items
        Transform content = dropdownList.Find("Viewport/Content");
        if (content == null) return -1;
        
        // Check each item (they're named "Item 0", "Item 1", etc.)
        for (int i = 0; i < activeDropdown.options.Count; i++)
        {
            Transform item = content.Find($"Item {i}: {activeDropdown.options[i].text}");
            if (item == null)
            {
                // Try alternate naming convention
                item = content.Find($"Item {i}");
            }
            if (item == null) continue;
            
            RectTransform rt = item.GetComponent<RectTransform>();
            if (rt == null) continue;
            
            float distance = Vector3.Distance(fingerTip.position, rt.position);
            if (distance < pokeDistance * 1.5f)
            {
                return i;
            }
        }
        
        return -1;
    }
}
