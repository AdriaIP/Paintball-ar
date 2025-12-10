using UnityEngine;
using UnityEngine.EventSystems;

using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


[AddComponentMenu("Radial Menu Framework/RMF Core Script")]
public class RMF_RadialMenu : MonoBehaviour {

    [HideInInspector]
    public RectTransform rt;

    [Header("OVR Hand Settings")]
    [Tooltip("Reference to the OVRHand component for hand tracking.")]
    public OVRHand ovrHand;

    [Tooltip("Reference to the hand anchor transform (e.g., RightHandAnchor). Used to calculate pointing direction for pinch mode.")]
    public Transform handAnchor;

    [Tooltip("Reference to the index finger tip transform for poke/touch selection.")]
    public Transform fingerTip;

    [Tooltip("Pinch strength threshold for hand selection (0-1). Default is 0.7.")]
    [Range(0f, 1f)]
    public float pinchThreshold = 0.7f;

    [Tooltip("Distance threshold for finger poke selection (in meters).")]
    public float pokeDistance = 0.05f;

    [Header("Selection Settings")]
    [Tooltip("Enable pinch-based selection (point hand direction + pinch to select).")]
    public bool usePinchSelection = true;

    [Tooltip("Enable poke/touch selection (touch element with finger tip to select).")]
    public bool usePokeSelection = true;

    [Tooltip("With lazy selection, you only have to point in the direction of an element to select it.")]
    public bool useLazySelection = true;

    [Header("Visual Settings")]
    [Tooltip("If set to true, a pointer will aim in the direction of your hand.")]
    public bool useSelectionFollower = true;

    [Tooltip("If using the selection follower, this must point to the rect transform of the selection follower's container.")]
    public RectTransform selectionFollowerContainer;

    [Tooltip("This is the text object that will display the labels of the radial elements when hovered.")]
    public Text textLabel;

    [Header("Menu Elements")]
    [Tooltip("This is the list of radial menu elements. Order-dependent.")]
    public List<RMF_RadialMenuElement> elements = new List<RMF_RadialMenuElement>();

    [Tooltip("Controls the total angle offset for all elements. Good values are 0, 45, 90, or 180.")]
    public float globalOffset = 0f;

    [Header("Initial Selection")]
    [Tooltip("Reference to the RayGun to sync initial selection with currentPrefabIndex.")]
    public RayGun rayGun;

    [HideInInspector]
    public float currentAngle = 0f;

    [HideInInspector]
    public int index = 0;

    private int elementCount;
    private float angleOffset;
    private int previousActiveIndex = 0;
    private PointerEventData pointer;
    private bool wasPinching = false;
    private float pokeDebounceTime = 0.6f;
    private float lastPokeTime = 0f;

    void Awake() {

        pointer = new PointerEventData(EventSystem.current);

        rt = GetComponent<RectTransform>();

        if (rt == null)
            Debug.LogError("Radial Menu: Rect Transform for radial menu " + gameObject.name + " could not be found. Please ensure this is an object parented to a canvas.");

        if (useSelectionFollower && selectionFollowerContainer == null)
            Debug.LogError("Radial Menu: Selection follower container is unassigned on " + gameObject.name + ", which has the selection follower enabled.");

        if (usePinchSelection && ovrHand == null)
            Debug.LogError("Radial Menu: Pinch selection is enabled but ovrHand is not assigned on " + gameObject.name);

        if (usePinchSelection && handAnchor == null)
            Debug.LogError("Radial Menu: Pinch selection is enabled but handAnchor is not assigned on " + gameObject.name);

        if (usePokeSelection && fingerTip == null)
            Debug.LogError("Radial Menu: Poke selection is enabled but fingerTip is not assigned on " + gameObject.name);

        elementCount = elements.Count;

        angleOffset = (360f / (float)elementCount);

        //Loop through and set up the elements.
        for (int i = 0; i < elementCount; i++) {
            if (elements[i] == null) {
                Debug.LogError("Radial Menu: element " + i.ToString() + " in the radial menu " + gameObject.name + " is null!");
                continue;
            }
            elements[i].parentRM = this;

            elements[i].setAllAngles((angleOffset * i) + globalOffset, angleOffset);

            elements[i].assignedIndex = i;

        }

    }


    void Start() {
        // Initialize selection follower
        if (useSelectionFollower && selectionFollowerContainer != null)
            selectionFollowerContainer.rotation = Quaternion.Euler(0, 0, -globalOffset);

        // Highlight the initially selected element based on RayGun's currentPrefabIndex
        if (rayGun != null && rayGun.currentPrefabIndex < elementCount && elementCount > 0) {
            int initialIndex = rayGun.currentPrefabIndex;
            if (elements[initialIndex] != null) {
                elements[initialIndex].highlightThisElement(pointer);
                previousActiveIndex = initialIndex;
            }
        }
    }

    // Update is called once per frame
    void Update() {

        bool isPinching = false;
        int pokedElementIndex = -1;

        // Check for poke/touch selection first
        if (usePokeSelection && fingerTip != null) {
            pokedElementIndex = GetPokedElementIndex();
        }

        // Handle pinch-based selection (hand direction + pinch)
        if (usePinchSelection && ovrHand != null && handAnchor != null) {
            float handAngle = GetOVRHandAngle();
            bool validAngle = handAngle >= 0;
            
            if (validAngle) {
                currentAngle = normalizeAngle(handAngle - globalOffset + (angleOffset / 2f));
            }

            isPinching = ovrHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > pinchThreshold;
        }

        // Handle lazy selection (highlighting based on hand direction)
        if (angleOffset != 0 && useLazySelection && usePinchSelection) {
            index = (int)(currentAngle / angleOffset);
            
            // Clamp index to valid range
            if (index >= elementCount) index = elementCount - 1;
            if (index < 0) index = 0;

            if (elements[index] != null) {
                selectButton(index);

                // Pinch to confirm selection
                if (wasPinching && !isPinching) {
                    ExecuteEvents.Execute(elements[index].button.gameObject, pointer, ExecuteEvents.submitHandler);
                }
                wasPinching = isPinching;
            }
        }

        // Handle poke/touch selection
        if (usePokeSelection && pokedElementIndex >= 0 && pokedElementIndex < elementCount) {
            if (Time.time - lastPokeTime > pokeDebounceTime) {
                // Highlight and select the poked element
                selectButton(pokedElementIndex);
                ExecuteEvents.Execute(elements[pokedElementIndex].button.gameObject, pointer, ExecuteEvents.submitHandler);
                lastPokeTime = Time.time;
            }
        }

        // Update selection follower
        if (useSelectionFollower && selectionFollowerContainer != null && usePinchSelection) {
            float handAngle = GetOVRHandAngle();
            if (handAngle >= 0) {
                selectionFollowerContainer.rotation = Quaternion.Euler(0, 0, -handAngle + 90);
            }
        }
    }

    // Gets the index of the element being poked/touched by the finger tip
    // Returns -1 if no element is being touched
    private int GetPokedElementIndex() {
        if (fingerTip == null) return -1;

        for (int i = 0; i < elementCount; i++) {
            if (elements[i] == null || elements[i].button == null) continue;

            // Get the button's RectTransform
            RectTransform buttonRect = elements[i].button.GetComponent<RectTransform>();
            if (buttonRect == null) continue;

            // Check distance from finger tip to button center
            float distance = Vector3.Distance(fingerTip.position, buttonRect.position);
            
            if (distance < pokeDistance) {
                return i;
            }
        }

        return -1;
    }

    // Gets the angle from the menu center to where the hand is, projected onto the menu plane
    // Returns angle in degrees (0-360), or -1 if invalid
    private float GetOVRHandAngle() {
        if (handAnchor == null) return -1f;

        // Get the vector from menu center to hand position
        Vector3 centerToHand = handAnchor.position - transform.position;
        
        // Project onto the menu plane (perpendicular to the menu's forward direction)
        Vector3 centerToHandProjected = Vector3.ProjectOnPlane(centerToHand, transform.forward);
        
        // If the projected vector is too small, hand is too close to center
        if (centerToHandProjected.magnitude < 0.01f) return -1f;

        // Calculate signed angle from menu's up direction
        float angle = Vector3.SignedAngle(transform.up, centerToHandProjected, -transform.forward);
        
        // Convert to 0-360 range
        if (angle < 0) {
            angle += 360f;
        }
        
        return angle;
    }


    //Selects the button with the specified index.
    private void selectButton(int i) {

          if (elements[i].active == false) {

            elements[i].highlightThisElement(pointer); //Select this one

            if (previousActiveIndex != i) 
                elements[previousActiveIndex].unHighlightThisElement(pointer); //Deselect the last one.
            

        }

        previousActiveIndex = i;

    }

    //Keeps angles between 0 and 360.
    private float normalizeAngle(float angle) {

        angle = angle % 360f;

        if (angle < 0)
            angle += 360;

        return angle;

    }


}
