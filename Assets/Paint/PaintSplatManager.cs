using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the spawning and pooling of paint splats.
/// Uses quad-based splats that conform to surfaces, which works on AR meshes.
/// </summary>
public class PaintSplatManager : MonoBehaviour
{
    public static PaintSplatManager Instance { get; private set; }
    
    [Header("Splat Prefab")]
    [Tooltip("Prefab for paint splat (quad with PaintSplat component)")]
    public GameObject splatPrefab;

    [Header("Render Settings")]
    [Tooltip("Assign the 'SplatTemplate' material here. Must have Alpha Clipping enabled!")]
    public Material splatMaterialTemplate;

    [Header("Splat Settings")]
    [Tooltip("Base size of splats")]
    public float baseSplatSize = 0.08f;
    
    [Tooltip("Random size variation (±)")]
    public float sizeVariation = 0.03f;
    
    [Tooltip("How far from surface to place splat (prevents z-fighting)")]
    public float surfaceOffset = 0.008f;
    
    [Tooltip("Use raycast to find exact surface position")]
    public bool useRaycastPositioning = true;
    
    [Tooltip("Layer to assign to splats (e.g., DeletableObjects)")]
    public string splatLayer = "DeletableObjects";
    
    [Tooltip("Parent splats to hit objects (keeps splats attached to moving objects)")]
    public bool parentToHitObject = true;
    
    [Tooltip("Maximum number of splats in scene")]
    public int maxSplats = 200;
    
    [Tooltip("Splat lifetime in seconds (0 = permanent)")]
    public float splatLifetime = 0f;
    
    [Header("Surface Conforming")]
    [Tooltip("Enable splat stretching on edges/corners")]
    public bool conformToEdges = true;
    
    [Tooltip("Max raycast distance for edge detection")]
    public float edgeDetectionDistance = 0.15f;
    
    [Header("Texture Variations")]
    [Tooltip("Different splat textures for variety")]
    public Texture2D[] splatTextures;
    
    // Object pool
    private Queue<GameObject> splatPool = new Queue<GameObject>();
    private List<GameObject> activeSplats = new List<GameObject>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Pre-populate pool
        for (int i = 0; i < 20; i++)
        {
            CreatePooledSplat();
        }
    }
    
    private GameObject CreatePooledSplat()
    {
        GameObject splat;
        
        if (splatPrefab != null)
        {
            splat = Instantiate(splatPrefab, transform);
        }
        else
        {
            // Create default quad splat
            splat = CreateDefaultSplatObject();
        }
        
        splat.SetActive(false);
        splatPool.Enqueue(splat);
        return splat;
    }
    
    private GameObject CreateDefaultSplatObject()
    {
        GameObject splat = GameObject.CreatePrimitive(PrimitiveType.Quad);
        splat.name = "PaintSplat";
        splat.transform.SetParent(transform);
        
        // Remove collider
        var collider = splat.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        
        // Setup material - use Alpha Cutout instead of Transparent to avoid passthrough bleed-through
        var renderer = splat.GetComponent<Renderer>();

        if (splatMaterialTemplate != null)
        {
            // Instantiate the template material which effectively clones its settings (Shader + Keywords)
            renderer.material = new Material(splatMaterialTemplate);
        }
        else
        {
            // Fallback (Not recommended for Builds)
            Material splatMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        
            // Use AlphaTest (Cutout) mode instead of Transparent
            // This prevents passthrough camera feed from bleeding through
            splatMat.SetFloat("_Surface", 0); // Opaque base
            splatMat.SetFloat("_AlphaClip", 1); // Enable alpha clipping
            splatMat.SetFloat("_Cutoff", 0.1f); // Alpha threshold (pixels below this are discarded)
            splatMat.SetFloat("_Cull", 0); // No culling (render both sides)
            splatMat.EnableKeyword("_ALPHATEST_ON");
            splatMat.renderQueue = 2450; // AlphaTest queue (after opaque, before transparent)
            renderer.material = splatMat;

        }

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        // Add PaintSplat component
        if (splat.GetComponent<PaintSplat>() == null)
            splat.AddComponent<PaintSplat>();
        
        return splat;
    }
    
    /// <summary>
    /// Spawn a paint splat at a collision point.
    /// </summary>
    public void SpawnSplat(Vector3 position, Vector3 normal, Color color, Collider hitCollider = null)
    {
        GameObject splat = GetSplatFromPool();
        if (splat == null) return;
        
        // Ensure normal points outward (away from surface toward where ball came from)
        // Use raycast to find exact surface position and correct normal
        Vector3 finalPosition = position;
        Vector3 finalNormal = normal.normalized;
        
        if (useRaycastPositioning)
        {
            // Cast ray from slightly above the surface to find exact hit point
            Vector3 rayOrigin = position + normal * 0.1f;
            if (Physics.Raycast(rayOrigin, -normal, out RaycastHit hit, 0.2f))
            {
                finalPosition = hit.point;
                finalNormal = hit.normal.normalized;
            }
            else
            {
                // Try opposite direction in case normal was inverted
                rayOrigin = position - normal * 0.1f;
                if (Physics.Raycast(rayOrigin, normal, out hit, 0.2f))
                {
                    finalPosition = hit.point;
                    finalNormal = hit.normal.normalized;
                }
            }
        }
        
        // Position with offset from surface (always offset outward along normal)
        splat.transform.position = finalPosition + finalNormal * surfaceOffset;
        
        // Rotate splat to lie flat on surface (quad's forward = surface normal)
        // Calculate a proper "up" vector that's perpendicular to the normal
        Vector3 upVector = Vector3.up;
        
        // If surface is nearly horizontal (floor/ceiling), use world forward as reference
        if (Mathf.Abs(Vector3.Dot(finalNormal, Vector3.up)) > 0.9f)
        {
            upVector = Vector3.forward;
        }
        
        // Create rotation where the quad faces along the normal direction
        // The quad's local Z axis will point along the normal (away from surface)
        Vector3 tangent = Vector3.Cross(finalNormal, upVector).normalized;
        if (tangent.sqrMagnitude < 0.001f)
        {
            tangent = Vector3.Cross(finalNormal, Vector3.right).normalized;
        }
        Vector3 bitangent = Vector3.Cross(tangent, finalNormal).normalized;
        
        splat.transform.rotation = Quaternion.LookRotation(finalNormal, bitangent);
        
        // Random rotation around normal (rotate around Z which is now the normal direction)
        splat.transform.Rotate(0, 0, Random.Range(0f, 360f), Space.Self);
        
        // Size with variation
        float size = baseSplatSize + Random.Range(-sizeVariation, sizeVariation);
        Vector3 scale = new Vector3(size, size, 1f);
        
        // Conform to edges if enabled
        if (conformToEdges && hitCollider != null)
        {
            scale = ConformSplatToSurface(splat.transform, finalPosition, finalNormal, scale, hitCollider);
        }
        
        splat.transform.localScale = scale;
        
        // Set layer for splats (e.g., DeletableObjects)
        int layerIndex = LayerMask.NameToLayer(splatLayer);
        if (layerIndex >= 0)
        {
            splat.layer = layerIndex;
        }
        
        // Parent to hit object if enabled (keeps splat attached to moving objects)
        if (parentToHitObject && hitCollider != null)
        {
            splat.transform.SetParent(hitCollider.transform, true);
        }
        else
        {
            splat.transform.SetParent(transform, true);
        }
        
        // Set color and texture
        var paintSplat = splat.GetComponent<PaintSplat>();
        if (paintSplat != null)
        {
            Texture2D tex = null;
            if (splatTextures != null && splatTextures.Length > 0)
            {
                tex = splatTextures[Random.Range(0, splatTextures.Length)];
            }
            // Ensure Initialize is called
            paintSplat.Initialize(color, tex, splatLifetime);
        }
        else
        {
            // Fallback if PaintSplat component missing
            var r = splat.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = color;
                if (splatTextures != null && splatTextures.Length > 0)
                    r.material.mainTexture = splatTextures[Random.Range(0, splatTextures.Length)];
            }
        }
        
        splat.SetActive(true);
        activeSplats.Add(splat);
        
        // Manage max splats
        while (activeSplats.Count > maxSplats)
        {
            RecycleSplat(activeSplats[0]);
        }
    }
    
    /// <summary>
    /// Spawn splat using ContactPoint from collision.
    /// </summary>
    public void SpawnSplat(ContactPoint contact, Color color)
    {
        SpawnSplat(contact.point, contact.normal, color, contact.otherCollider);
    }
    
    private Vector3 ConformSplatToSurface(Transform splatTransform, Vector3 position, Vector3 normal, Vector3 baseScale, Collider hitCollider)
    {
        // Cast rays in the splat's local X and Y directions to detect edges
        Vector3 right = splatTransform.right;
        Vector3 up = splatTransform.up;
        
        float rightDist = CheckEdgeDistance(position, right, normal, hitCollider);
        float leftDist = CheckEdgeDistance(position, -right, normal, hitCollider);
        float upDist = CheckEdgeDistance(position, up, normal, hitCollider);
        float downDist = CheckEdgeDistance(position, -up, normal, hitCollider);
        
        // Calculate scale factors based on available surface
        float halfBaseSize = baseSplatSize * 0.5f;
        
        float xScale = Mathf.Min((rightDist + leftDist) / baseSplatSize, 1.5f);
        float yScale = Mathf.Min((upDist + downDist) / baseSplatSize, 1.5f);
        
        // Don't make too small
        xScale = Mathf.Max(xScale, 0.3f);
        yScale = Mathf.Max(yScale, 0.3f);
        
        // Offset position if near edge
        Vector3 offset = Vector3.zero;
        if (rightDist < halfBaseSize && leftDist > halfBaseSize)
            offset -= right * (halfBaseSize - rightDist) * 0.5f;
        else if (leftDist < halfBaseSize && rightDist > halfBaseSize)
            offset += right * (halfBaseSize - leftDist) * 0.5f;
            
        if (upDist < halfBaseSize && downDist > halfBaseSize)
            offset -= up * (halfBaseSize - upDist) * 0.5f;
        else if (downDist < halfBaseSize && upDist > halfBaseSize)
            offset += up * (halfBaseSize - downDist) * 0.5f;
        
        splatTransform.position += offset;
        
        return new Vector3(baseScale.x * xScale, baseScale.y * yScale, 1f);
    }
    
    private float CheckEdgeDistance(Vector3 origin, Vector3 direction, Vector3 surfaceNormal, Collider originalCollider)
    {
        // Offset origin slightly along surface normal to avoid self-intersection
        Vector3 rayOrigin = origin + surfaceNormal * 0.01f;
        
        // Cast ray along surface
        if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, edgeDetectionDistance))
        {
            // Check if we're still on the same surface (similar normal)
            if (Vector3.Dot(hit.normal, surfaceNormal) > 0.7f)
            {
                return edgeDetectionDistance; // Continue on same surface
            }
            else
            {
                return hit.distance; // Found an edge
            }
        }
        
        // Also check if surface ends by casting back toward original surface
        Vector3 checkPoint = rayOrigin + direction * edgeDetectionDistance;
        if (!Physics.Raycast(checkPoint, -surfaceNormal, out _, 0.05f))
        {
            // Surface ended, find where
            for (float d = edgeDetectionDistance; d > 0; d -= 0.01f)
            {
                checkPoint = rayOrigin + direction * d;
                if (Physics.Raycast(checkPoint, -surfaceNormal, out _, 0.05f))
                {
                    return d;
                }
            }
            return 0.02f; // Very close to edge
        }
        
        return edgeDetectionDistance;
    }
    
    private GameObject GetSplatFromPool()
    {
        if (splatPool.Count == 0)
        {
            CreatePooledSplat();
        }
        
        return splatPool.Count > 0 ? splatPool.Dequeue() : null;
    }
    
    public void RecycleSplat(GameObject splat)
    {
        if (splat == null) return;
        
        splat.SetActive(false);
        splat.transform.SetParent(transform, true); // Reset parent to manager
        activeSplats.Remove(splat);
        splatPool.Enqueue(splat);
    }
    
    /// <summary>
    /// Clear all active splats
    /// </summary>
    public void ClearAllSplats()
    {
        foreach (var splat in activeSplats.ToArray())
        {
            RecycleSplat(splat);
        }
    }
}
