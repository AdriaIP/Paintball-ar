using UnityEngine;

public class RayGun : MonoBehaviour
{
    public LayerMask hitLayerMask;
    // A hand gesture to trigger the ray gun
    public OVRHand leftHand;
    public GameObject leftHandObject;
    public LineRenderer linePrefab;

    public GameObject rayImpactPrefab;

    public Transform shootingPoint;
    public float maxLineDistance = 5;
    public float lineShowDuration = 0.3f;

    public AudioSource audioSource;
    public AudioClip shootAudioClip;

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
        audioSource.PlayOneShot(shootAudioClip);

        Ray ray = new Ray(shootingPoint.position, shootingPoint.forward);
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, maxLineDistance, hitLayerMask);

        Vector3 endPoint = Vector3.zero;

        if (hasHit) {
            // stop the ray at the hit point
            endPoint = hit.point;

            Quaternion rayImpactRotation = Quaternion.LookRotation(hit.normal);

            GameObject rayImpact = Instantiate(rayImpactPrefab, hit.point, rayImpactRotation);
        } else {
            endPoint = shootingPoint.position + shootingPoint.forward * maxLineDistance;
        }

        LineRenderer line = Instantiate(linePrefab);
        line.positionCount = 2;
        line.SetPosition(0, shootingPoint.position);

        line.SetPosition(1, endPoint);

        Destroy(line.gameObject, lineShowDuration);
    }
}
