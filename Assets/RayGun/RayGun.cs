using UnityEngine;

public class RayGun : MonoBehaviour
{
    // A hand gesture to trigger the ray gun
    public OVRHand leftHand;
    public GameObject leftHandObject;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > 0.3) {
            Shoot();
        }
    }

    public void Shoot() {
        Debug.Log("Pew Pew!");
    }
}
