using UnityEngine;
using UnityEngine.EventSystems;

using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


[AddComponentMenu("Radial Menu Framework/RMF Core Script")]
public class RMF_RadialMenu : MonoBehaviour {

    [HideInInspector]
    public RectTransform rt;
    //public RectTransform baseCircleRT;
    //public Image selectionFollowerImage;

    [Tooltip("Adjusts the radial menu for use with a gamepad or joystick. You might need to edit this script if you're not using the default horizontal and vertical input axes.")]
    public bool useGamepad = false;

    [Tooltip("Enables OVR hand tracking for menu selection. Uses hand pointing direction and pinch to select.")]
    public bool useOVRHand = false;

    [Tooltip("Reference to the OVRHand component for hand tracking. Required if useOVRHand is enabled.")]
    public OVRHand ovrHand;

    [Tooltip("Reference to the hand anchor transform (e.g., RightHandAnchor or index finger tip). Used to calculate pointing direction.")]
    public Transform handAnchor;

    [Tooltip("The camera used for the radial menu (usually the VR camera/CenterEyeAnchor). Required for OVR hand input.")]
    public Camera vrCamera;

    [Tooltip("Pinch strength threshold for OVR hand selection (0-1). Default is 0.7.")]
    [Range(0f, 1f)]
    public float pinchThreshold = 0.7f;

    [Tooltip("With lazy selection, you only have to point your mouse (or joystick) in the direction of an element to select it, rather than be moused over the element entirely.")]
    public bool useLazySelection = true;


    [Tooltip("If set to true, a pointer with a graphic of your choosing will aim in the direction of your mouse. You will need to specify the container for the selection follower.")]
    public bool useSelectionFollower = true;

    [Tooltip("If using the selection follower, this must point to the rect transform of the selection follower's container.")]
    public RectTransform selectionFollowerContainer;

    [Tooltip("This is the text object that will display the labels of the radial elements when they are being hovered over. If you don't want a label, leave this blank.")]
    public Text textLabel;

    [Tooltip("This is the list of radial menu elements. This is order-dependent. The first element in the list will be the first element created, and so on.")]
    public List<RMF_RadialMenuElement> elements = new List<RMF_RadialMenuElement>();


    [Tooltip("Controls the total angle offset for all elements. For example, if set to 45, all elements will be shifted +45 degrees. Good values are generally 45, 90, or 180")]
    public float globalOffset = 0f;


    [HideInInspector]
    public float currentAngle = 0f; //Our current angle from the center of the radial menu.


    [HideInInspector]
    public int index = 0; //The current index of the element we're pointing at.

    private int elementCount;

    private float angleOffset; //The base offset. For example, if there are 4 elements, then our offset is 360/4 = 90

    private int previousActiveIndex = 0; //Used to determine which buttons to unhighlight in lazy selection.

    private PointerEventData pointer;

    // OVR Hand tracking state
    private bool wasPinching = false;

    void Awake() {

        pointer = new PointerEventData(EventSystem.current);

        rt = GetComponent<RectTransform>();

        if (rt == null)
            Debug.LogError("Radial Menu: Rect Transform for radial menu " + gameObject.name + " could not be found. Please ensure this is an object parented to a canvas.");

        if (useSelectionFollower && selectionFollowerContainer == null)
            Debug.LogError("Radial Menu: Selection follower container is unassigned on " + gameObject.name + ", which has the selection follower enabled.");

        if (useOVRHand && ovrHand == null)
            Debug.LogError("Radial Menu: OVR Hand is enabled but ovrHand is not assigned on " + gameObject.name);

        if (useOVRHand && handAnchor == null)
            Debug.LogError("Radial Menu: OVR Hand is enabled but handAnchor is not assigned on " + gameObject.name);

        if (useOVRHand && vrCamera == null)
            Debug.LogError("Radial Menu: OVR Hand is enabled but vrCamera is not assigned on " + gameObject.name);

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


        if (useGamepad) {
            EventSystem.current.SetSelectedGameObject(gameObject, null); //We'll make this the active object when we start it. Comment this line to set it manually from another script.
            if (useSelectionFollower && selectionFollowerContainer != null)
                selectionFollowerContainer.rotation = Quaternion.Euler(0, 0, -globalOffset); //Point the selection follower at the first element.
        }

    }

    // Update is called once per frame
    void Update() {

        float rawAngle = 0f;
        bool isPinching = false;

        if (useOVRHand && ovrHand != null && handAnchor != null) {
            // OVR Hand tracking mode - skip legacy input entirely
            float handAngle = GetOVRHandAngle();
            bool validAngle = handAngle >= 0;
            
            if (validAngle) {
                currentAngle = normalizeAngle(handAngle - globalOffset + (angleOffset / 2f));
            }

            // Check for pinch gesture
            isPinching = ovrHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > pinchThreshold;
        }
        else if (!useOVRHand) {
            // Legacy input mode - only use when OVR hand is disabled
            bool joystickMoved = false;
            
            try {
                joystickMoved = Input.GetAxis("Horizontal") != 0.0 || Input.GetAxis("Vertical") != 0.0;
            } catch {
                // Input System is active, skip gamepad input
            }

            if (!useGamepad) {
                try {
                    rawAngle = Mathf.Atan2(Input.mousePosition.y - rt.position.y, Input.mousePosition.x - rt.position.x) * Mathf.Rad2Deg;
                    currentAngle = normalizeAngle(-rawAngle + 90 - globalOffset + (angleOffset / 2f));
                } catch {
                    // Input System is active, skip mouse input
                }
            }
            else if (joystickMoved) {
                try {
                    rawAngle = Mathf.Atan2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal")) * Mathf.Rad2Deg;
                    currentAngle = normalizeAngle(-rawAngle + 90 - globalOffset + (angleOffset / 2f));
                } catch {
                    // Input System is active, skip gamepad input
                }
            }
        }

        //Handles lazy selection. Checks the current angle, matches it to the index of an element, and then highlights that element.
        if (angleOffset != 0 && useLazySelection) {

            //Current element index we're pointing at.
            index = (int)(currentAngle / angleOffset);

            if (elements[index] != null) {

                //Select it.
                selectButton(index);

                //If we click or press a "submit" button (Button on joystick, enter, or spacebar), then we'll execute the OnClick() function for the button.
                if (useOVRHand) {
                    // For OVR hand: trigger on pinch release (like the RayGun behavior)
                    if (wasPinching && !isPinching) {
                        ExecuteEvents.Execute(elements[index].button.gameObject, pointer, ExecuteEvents.submitHandler);
                    }
                    wasPinching = isPinching;
                }
                else if (!useOVRHand) {
                    try {
                        if (Input.GetMouseButtonDown(0) || Input.GetButtonDown("Submit")) {
                            ExecuteEvents.Execute(elements[index].button.gameObject, pointer, ExecuteEvents.submitHandler);
                        }
                    } catch {
                        // Input System is active, skip legacy input
                    }
                }
            }

        }

        //Updates the selection follower if we're using one.
        if (useSelectionFollower && selectionFollowerContainer != null) {
            if (useOVRHand) {
                float handAngle = GetOVRHandAngle();
                if (handAngle >= 0) {
                    selectionFollowerContainer.rotation = Quaternion.Euler(0, 0, -handAngle + 90);
                }
            }
            else if (!useOVRHand) {
                try {
                    bool joystickMoved = useGamepad && (Input.GetAxis("Horizontal") != 0.0 || Input.GetAxis("Vertical") != 0.0);
                    if (!useGamepad || joystickMoved) {
                        float rawAngleForFollower;
                        if (!useGamepad)
                            rawAngleForFollower = Mathf.Atan2(Input.mousePosition.y - rt.position.y, Input.mousePosition.x - rt.position.x) * Mathf.Rad2Deg;
                        else
                            rawAngleForFollower = Mathf.Atan2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal")) * Mathf.Rad2Deg;
                        selectionFollowerContainer.rotation = Quaternion.Euler(0, 0, rawAngleForFollower + 270);
                    }
                } catch {
                    // Input System is active, skip legacy input
                }
            }

        } 

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
