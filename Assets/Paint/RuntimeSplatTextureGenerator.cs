using UnityEngine;

/// <summary>
/// Generates procedural paint splat textures at runtime.
/// Attach to the PaintSplatManager or any persistent object.
/// </summary>
public class RuntimeSplatTextureGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public int textureSize = 128;
    public int textureCount = 5;
    
    [Header("Auto-Assign to Manager")]
    public PaintSplatManager targetManager;
    
    private void Awake()
    {
        GenerateAndAssign();
    }
    
    public void GenerateAndAssign()
    {
        Texture2D[] textures = new Texture2D[textureCount];
        
        for (int i = 0; i < textureCount; i++)
        {
            // Alternate between Irregular and Splatter shapes
            int type = i % 2;
            switch (type)
            {
                case 0:
                    textures[i] = GenerateIrregularSplat(textureSize, i * 12345);
                    break;
                case 1:
                    textures[i] = GenerateSplatterSplat(textureSize, i * 67890);
                    break;
            }
        }
        
        // Auto-find manager if not assigned
        if (targetManager == null)
        {
            targetManager = GetComponent<PaintSplatManager>();
        }
        if (targetManager == null)
        {
            targetManager = FindFirstObjectByType<PaintSplatManager>();
        }
        
        if (targetManager != null)
        {
            targetManager.splatTextures = textures;
        }
    }
    
    private Texture2D GenerateCircleSplat(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(dist / radius);
                alpha = Mathf.Pow(alpha, 0.5f);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
    
    private Texture2D GenerateIrregularSplat(int size, int seed)
    {
        Random.State oldState = Random.state;
        Random.InitState(seed);
        
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float baseRadius = size / 3f * 0.75f; // Smaller to ensure it stays within circular bounds
        float maxRadius = size / 2f * 0.95f; // Hard circular boundary
        
        int numBumps = Random.Range(6, 12);
        float[] bumpAngles = new float[numBumps];
        float[] bumpAmounts = new float[numBumps];
        
        for (int i = 0; i < numBumps; i++)
        {
            bumpAngles[i] = Random.Range(0f, Mathf.PI * 2f);
            bumpAmounts[i] = Random.Range(-0.2f, 0.25f); // Less extreme bumps
        }
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                Vector2 toCenter = pos - center;
                float dist = toCenter.magnitude;
                float angle = Mathf.Atan2(toCenter.y, toCenter.x);
                
                // Calculate irregular radius at this angle
                float radiusAtAngle = baseRadius;
                for (int i = 0; i < numBumps; i++)
                {
                    float angleDiff = Mathf.DeltaAngle(angle * Mathf.Rad2Deg, bumpAngles[i] * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                    float influence = Mathf.Exp(-angleDiff * angleDiff * 2f);
                    radiusAtAngle += baseRadius * bumpAmounts[i] * influence;
                }
                
                // Clamp to circular boundary
                radiusAtAngle = Mathf.Min(radiusAtAngle, maxRadius);
                
                // Circular falloff - outside maxRadius is always 0
                float circularMask = 1f - Mathf.Clamp01((dist - maxRadius * 0.8f) / (maxRadius * 0.2f));
                
                float alpha = 1f - Mathf.Clamp01(dist / radiusAtAngle);
                float noise = Mathf.PerlinNoise(x * 0.08f + seed, y * 0.08f) * 0.2f;
                alpha = Mathf.Clamp01(alpha + noise - 0.1f);
                alpha = Mathf.Pow(alpha, 0.5f);
                
                // Apply circular mask
                alpha *= circularMask;
                
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        Random.state = oldState;
        return tex;
    }
    
    private Texture2D GenerateSplatterSplat(int size, int seed)
    {
        Random.State oldState = Random.state;
        Random.InitState(seed);
        
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        
        Color[] pixels = new Color[size * size];
        
        // Main blob - smaller to leave room for drops
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float mainRadius = size / 4f; // Smaller central blob (was size/3f)
        float maxRadius = size / 2f * 0.95f; // Hard circular boundary
        
        // Satellite blobs - more of them, spread out more
        int blobCount = Random.Range(5, 8); // More droplets
        Vector2[] blobCenters = new Vector2[blobCount];
        float[] blobRadii = new float[blobCount];
        
        for (int i = 0; i < blobCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(mainRadius * 0.8f, mainRadius * 2.2f); // Spread further out
            blobCenters[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            blobRadii[i] = Random.Range(size * 0.055f, size * 0.075f); // Tighter size range
        }
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = 0f;
                Vector2 pos = new Vector2(x, y);
                float distFromCenter = Vector2.Distance(pos, center);
                
                // Circular mask - fade out near edges
                float circularMask = 1f - Mathf.Clamp01((distFromCenter - maxRadius * 0.75f) / (maxRadius * 0.25f));
                
                // Main blob
                float dist = distFromCenter;
                alpha = Mathf.Max(alpha, 1f - Mathf.Clamp01(dist / mainRadius));
                
                // Satellite blobs
                for (int i = 0; i < blobCount; i++)
                {
                    dist = Vector2.Distance(pos, blobCenters[i]);
                    float blobAlpha = 1f - Mathf.Clamp01(dist / blobRadii[i]);
                    alpha = Mathf.Max(alpha, blobAlpha);
                }
                
                // Noise
                float noise = Mathf.PerlinNoise(x * 0.12f + seed, y * 0.12f) * 0.15f;
                alpha = Mathf.Clamp01(alpha + noise - 0.08f);
                alpha = Mathf.Pow(alpha, 0.5f);
                
                // Apply circular mask
                alpha *= circularMask;
                
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        Random.state = oldState;
        return tex;
    }
}
