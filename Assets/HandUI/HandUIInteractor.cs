using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// Allows hand tracking to interact with world-space UI elements (buttons, toggles, sliders).
/// Uses the same poke detection approach as RMF_RadialMenu.
/// Attach this to your menu canvas.
/// </summary>
public class HandUIInteractor : MonoBehaviour
{
    [Header("Hand References")]
    [Tooltip("Reference to the OVRHand component.")]
    public OVRHand hand;
    
    [Tooltip("The index finger tip transform for poking UI elements.")]
    public Transform fingerTip;
    
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
    
    private float lastPokeTime = 0f;
    private Slider activeSlider = null;
    private PointerEventData pointer;
    
    void Start()
    {
        pointer = new PointerEventData(EventSystem.current);
    }
    
    void Update()
    {
        if (fingerTip == null) return;
        
        // Handle slider dragging first
        if (activeSlider != null)
        {
            UpdateSliderDrag();
            return;
        }
        
        // Check for toggle pokes
        int pokedToggleIndex = GetPokedToggleIndex();
        if (pokedToggleIndex >= 0 && Time.time - lastPokeTime > pokeDebounceTime)
        {
            Toggle toggle = toggles[pokedToggleIndex];
            toggle.isOn = !toggle.isOn;
            lastPokeTime = Time.time;
            Debug.Log($"HandUIInteractor: Toggle '{toggle.name}' set to {toggle.isOn}");
            return;
        }
        
        // Check for button pokes
        int pokedButtonIndex = GetPokedButtonIndex();
        if (pokedButtonIndex >= 0 && Time.time - lastPokeTime > pokeDebounceTime)
        {
            Button button = buttons[pokedButtonIndex];
            button.onClick.Invoke();
            lastPokeTime = Time.time;
            Debug.Log($"HandUIInteractor: Button '{button.name}' clicked");
            return;
        }
        
        // Check for slider touch (starts dragging)
        int pokedSliderIndex = GetPokedSliderIndex();
        if (pokedSliderIndex >= 0)
        {
            activeSlider = sliders[pokedSliderIndex];
            Debug.Log($"HandUIInteractor: Started dragging slider '{activeSlider.name}'");
        }
    }
    
    private int GetPokedToggleIndex()
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
    
    private int GetPokedButtonIndex()
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
    
    private int GetPokedSliderIndex()
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
        if (activeSlider == null || fingerTip == null) return;
        
        RectTransform sliderRect = activeSlider.GetComponent<RectTransform>();
        if (sliderRect == null) return;
        
        // Check if finger is still near the slider
        float distanceToSlider = Vector3.Distance(fingerTip.position, sliderRect.position);
        if (distanceToSlider > pokeDistance * 4f)
        {
            // Finger moved away, stop dragging
            Debug.Log($"HandUIInteractor: Stopped dragging slider '{activeSlider.name}'");
            activeSlider = null;
            return;
        }
        
        // Convert finger position to local slider space
        Vector3 localPoint = sliderRect.InverseTransformPoint(fingerTip.position);
        
        // Calculate normalized position along the slider (assuming horizontal slider)
        float sliderWidth = sliderRect.rect.width;
        float normalizedX = (localPoint.x + sliderWidth / 2f) / sliderWidth;
        
        // Clamp and apply value
        normalizedX = Mathf.Clamp01(normalizedX);
        activeSlider.value = Mathf.Lerp(activeSlider.minValue, activeSlider.maxValue, normalizedX);
    }
}
