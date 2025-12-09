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

    private bool isPinching = false;
    private LineRenderer currentLine;
    private RaycastHit lastHit;
    private bool hadHit = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        bool currentlyPinching = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > 0.3f;

        if (currentlyPinching)
        {
            // Show the line while pinching
            ShowLine();
            isPinching = true;
        }
        else if (isPinching && !currentlyPinching)
        {
            // Fire the shot
            Shoot();
            isPinching = false;
        }
    }

    private void ShowLine()
    {
        Ray ray = new Ray(shootingPoint.position, shootingPoint.forward);
        hadHit = Physics.Raycast(ray, out lastHit, maxLineDistance, hitLayerMask);

        Vector3 endPoint;
        if (hadHit)
        {
            endPoint = lastHit.point;
        }
        else
        {
            endPoint = shootingPoint.position + shootingPoint.forward * maxLineDistance;
        }

        // Create line if it doesn't exist
        if (currentLine == null)
        {
            currentLine = Instantiate(linePrefab);
            currentLine.positionCount = 2;
        }

        // Update line positions
        currentLine.SetPosition(0, shootingPoint.position);
        currentLine.SetPosition(1, endPoint);
    }

    public void Shoot()
    {
        audioSource.PlayOneShot(shootAudioClip);

        // Create object (target right now) if we hit something
        if (hadHit)
        {
            Quaternion rayImpactRotation = Quaternion.LookRotation(lastHit.normal);
            GameObject rayImpact = Instantiate(rayImpactPrefab, lastHit.point, rayImpactRotation);
        }

        // Destroy the line after a short duration (we keep it visible for a bit for a better effect)
        if (currentLine != null)
        {
            Destroy(currentLine.gameObject, lineShowDuration);
            currentLine = null;
        }

        hadHit = false;
    }
}
