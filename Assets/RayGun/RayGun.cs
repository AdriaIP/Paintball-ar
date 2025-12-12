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
    
    [Tooltip("Position offset from surface for each prefab (in meters). Use this to avoid z-fighting or adjust placement.")]
    public float[] prefabPositionOffsets;
    
    [Tooltip("Current ray impact prefab index.")]
    public int currentPrefabIndex = 0;

    [Header("Delete Mode")]
    [Tooltip("Prefab to show when in delete mode (e.g., spark effect).")]
    public GameObject deleteModePrefab;
    
    [Tooltip("Rotation offset for delete mode prefab.")]
    public Vector3 deleteModePrefabRotationOffset = Vector3.zero;
    
    [Tooltip("Layer for spawned objects that can be deleted.")]
    public LayerMask deletableLayer;
    
    [Tooltip("Is delete mode currently active?")]
    public bool isDeleteMode = false;

    public Transform shootingPoint;
    public float maxLineDistance = 50;
    public float lineShowDuration = 0.3f;

    public AudioSource audioSource;
    public AudioClip shootAudioClip;

    [Header("Detachable Settings")]
    [Tooltip("If true, the ray gun will only respond to input when enabled. Use with DetachableRayGun.")]
    public bool requireEnabled = true;

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
        // Don't process input if disabled (used by DetachableRayGun)
        if (requireEnabled && !enabled) return;
        
        bool currentlyPinching = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > 0.8f;

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
    
    void OnDisable()
    {
        // Clean up any active line when disabled
        if (currentLine != null)
        {
            Destroy(currentLine.gameObject);
            currentLine = null;
        }
        isPinching = false;
        hadHit = false;
    }

    /// Sets the ray impact prefab by index. For example, it can be called from the radial menu buttons.
    /// This also disables delete mode.
    public void SetRayImpactPrefab(int index)
    {
        if (rayImpactPrefabs == null || rayImpactPrefabs.Length == 0)
        {
            Debug.LogWarning("RayGun: No ray impact prefabs assigned!");
            return;
        }
        
        currentPrefabIndex = Mathf.Clamp(index, 0, rayImpactPrefabs.Length - 1);
        isDeleteMode = false; // Disable delete mode when selecting a prefab
    }

    /// Enables delete mode. Call this from a radial menu button.
    public void EnableDeleteMode()
    {
        isDeleteMode = true;
    }

    private void ShowLine()
    {
        Ray ray = new Ray(shootingPoint.position, shootingPoint.forward);
        
        // Combine hitLayerMask with deletableLayer so we can hit both surfaces and spawned objects
        LayerMask combinedMask = hitLayerMask | deletableLayer;
        hadHit = Physics.Raycast(ray, out lastHit, maxLineDistance, combinedMask);

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
            if (isDeleteMode)
            {
                // Delete mode: spawn spark effect and delete hit object if it's on the deletable layer
                if (deleteModePrefab != null)
                {
                    Quaternion sparkRotation = Quaternion.LookRotation(lastHit.normal);
                    sparkRotation *= Quaternion.Euler(deleteModePrefabRotationOffset);
                    GameObject spark = Instantiate(deleteModePrefab, lastHit.point, sparkRotation);
                    spark.transform.position += lastHit.normal * 0.05f;

                    Destroy(spark, 1f);
                }

                // Check if hit object is on the deletable layer and destroy it
                GameObject hitObject = lastHit.collider.gameObject;
                // Debug.Log("Hit object layer: " + LayerMask.LayerToName(hitObject.layer));
                if (((1 << hitObject.layer) & deletableLayer) != 0)
                {
                    // Debug.Log("RayGun: Deleting object " + hitObject.name);
                    Destroy(hitObject);
                }
            }
            else if (CurrentRayImpactPrefab != null)
            {
                // Normal mode: spawn the selected prefab
                Quaternion rayImpactRotation = Quaternion.LookRotation(lastHit.normal);
                
                // Apply per-prefab rotation offset if configured
                if (prefabRotationOffsets != null && currentPrefabIndex < prefabRotationOffsets.Length)
                {
                    rayImpactRotation *= Quaternion.Euler(prefabRotationOffsets[currentPrefabIndex]);
                }
                
                GameObject rayImpact = Instantiate(CurrentRayImpactPrefab, lastHit.point, rayImpactRotation);

                // Set the object to the deletable layer so it can be deleted later
                int layerIndex = GetLayerFromMask(deletableLayer);
                if (layerIndex >= 0)
                {
                    rayImpact.layer = layerIndex;
                }

                // Move the impact object slightly away from the surface to avoid z-fighting
                float posOffset = 0.0f; // Default offset
                if (prefabPositionOffsets != null && currentPrefabIndex < prefabPositionOffsets.Length)
                {
                    posOffset = prefabPositionOffsets[currentPrefabIndex];
                }
                rayImpact.transform.position += lastHit.normal * posOffset;
            }
        }

        // Destroy the line after a short duration (we keep it visible for a bit for a better effect)
        if (currentLine != null)
        {
            Destroy(currentLine.gameObject, lineShowDuration);
            currentLine = null;
        }

        hadHit = false;
    }

    // Helper to get the first layer index from a LayerMask
    private int GetLayerFromMask(LayerMask mask)
    {
        int layerMaskValue = mask.value;
        for (int i = 0; i < 32; i++)
        {
            if ((layerMaskValue & (1 << i)) != 0)
                return i;
        }
        return -1;
    }
}
