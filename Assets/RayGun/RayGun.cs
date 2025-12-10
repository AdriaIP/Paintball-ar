using UnityEngine;

public class RayGun : MonoBehaviour
{
    public LayerMask hitLayerMask;
    // A hand gesture to trigger the ray gun
    public OVRHand leftHand;
    public GameObject leftHandObject;
    public LineRenderer linePrefab;

    [Header("Ray Impact Prefabs")]
    [Tooltip("List of available ray impact prefabs. Use SetRayImpactPrefab() to switch between them.")]
    public GameObject[] rayImpactPrefabs;
    
    [Tooltip("Rotation offset for each prefab (in degrees). Use this to correct orientation issues. For objects facing +Z use (0,0,0), for objects facing +Y use (-90,0,0).")]
    public Vector3[] prefabRotationOffsets;
    
    [Tooltip("Current ray impact prefab index.")]
    public int currentPrefabIndex = 0;

    public Transform shootingPoint;
    public float maxLineDistance = 5;
    public float lineShowDuration = 0.3f;

    public AudioSource audioSource;
    public AudioClip shootAudioClip;

    private bool isPinching = false;
    private LineRenderer currentLine;
    private RaycastHit lastHit;
    private bool hadHit = false;

    // Property to get the current prefab
    public GameObject CurrentRayImpactPrefab {
        get {
            if (rayImpactPrefabs == null || rayImpactPrefabs.Length == 0) return null;
            return rayImpactPrefabs[Mathf.Clamp(currentPrefabIndex, 0, rayImpactPrefabs.Length - 1)];
        }
    }

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

    /// Sets the ray impact prefab by index. For example, it can be called from the radial menu buttons.
    public void SetRayImpactPrefab(int index)
    {
        if (rayImpactPrefabs == null || rayImpactPrefabs.Length == 0)
        {
            Debug.LogWarning("RayGun: No ray impact prefabs assigned!");
            return;
        }
        
        currentPrefabIndex = Mathf.Clamp(index, 0, rayImpactPrefabs.Length - 1);
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
        if (hadHit && CurrentRayImpactPrefab != null)
        {
            // Base rotation: align object's forward (Z) with surface normal
            Quaternion rayImpactRotation = Quaternion.LookRotation(lastHit.normal);
            
            // Apply per-prefab rotation offset if configured
            if (prefabRotationOffsets != null && currentPrefabIndex < prefabRotationOffsets.Length)
            {
                rayImpactRotation *= Quaternion.Euler(prefabRotationOffsets[currentPrefabIndex]);
            }
            
            GameObject rayImpact = Instantiate(CurrentRayImpactPrefab, lastHit.point, rayImpactRotation);

            // Move the impact object slightly away from the surface to avoid z-fighting
            rayImpact.transform.position += lastHit.normal * 0.05f;
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
