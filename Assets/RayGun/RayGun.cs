using UnityEngine;

public class RayGun : MonoBehaviour
{
    // A hand gesture to trigger the ray gun
    public OVRHand leftHand;
    public GameObject leftHandObject;
    public LineRenderer linePrefab;
    public Transform shootingPoint;
    public float maxLineDistance = 5;
    public float lineShowDuration = 0.3f;

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

        LineRenderer line = Instantiate(linePrefab);
        line.positionCount = 2;
        line.SetPosition(0, shootingPoint.position);

        Vector3 endPosition = shootingPoint.position + shootingPoint.forward * maxLineDistance;

        line.SetPosition(1, endPosition);

        Destroy(line.gameObject, lineShowDuration);
    }
}
