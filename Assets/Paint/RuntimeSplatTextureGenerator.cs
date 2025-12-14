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
            int type = i % 4;
            switch (type)
            {
                case 0:
                    textures[i] = GenerateCircleSplat(textureSize);
                    break;
                case 1:
                    textures[i] = GenerateIrregularSplat(textureSize, i * 12345);
                    break;
                case 2:
                    textures[i] = GenerateDripSplat(textureSize, i * 67890);
                    break;
                case 3:
                    textures[i] = GenerateSplatterSplat(textureSize, i * 11111);
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
                // Premultiplied alpha for proper transparency
                pixels[y * size + x] = new Color(alpha, alpha, alpha, alpha);
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
        float baseRadius = size / 2f * 0.8f;
        
        int numBumps = Random.Range(5, 10);
        float[] bumpAngles = new float[numBumps];
        float[] bumpAmounts = new float[numBumps];
        
        for (int i = 0; i < numBumps; i++)
        {
            bumpAngles[i] = Random.Range(0f, Mathf.PI * 2f);
            bumpAmounts[i] = Random.Range(-0.3f, 0.4f);
        }
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                Vector2 toCenter = pos - center;
                float dist = toCenter.magnitude;
                float angle = Mathf.Atan2(toCenter.y, toCenter.x);
                
                float radiusAtAngle = baseRadius;
                for (int i = 0; i < numBumps; i++)
                {
                    float angleDiff = Mathf.DeltaAngle(angle * Mathf.Rad2Deg, bumpAngles[i] * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                    float influence = Mathf.Exp(-angleDiff * angleDiff * 2f);
                    radiusAtAngle += baseRadius * bumpAmounts[i] * influence;
                }
                
                float alpha = 1f - Mathf.Clamp01(dist / radiusAtAngle);
                float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.3f;
                alpha = Mathf.Clamp01(alpha + noise - 0.15f);
                alpha = Mathf.Pow(alpha, 0.7f);
                
                // Premultiplied alpha
                pixels[y * size + x] = new Color(alpha, alpha, alpha, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        Random.state = oldState;
        return tex;
    }
    
    private Texture2D GenerateDripSplat(int size, int seed)
    {
        Random.State oldState = Random.state;
        Random.InitState(seed);
        
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 3f);
        float radius = size / 4f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = 0f;
                
                float dist = Vector2.Distance(new Vector2(x, y), center);
                alpha = Mathf.Max(alpha, 1f - Mathf.Clamp01(dist / radius));
                
                if (y > center.y)
                {
                    float dripWidth = radius * 0.4f * (1f - (y - center.y) / (size - center.y));
                    float xDist = Mathf.Abs(x - center.x);
                    if (xDist < dripWidth)
                    {
                        float dripAlpha = (1f - xDist / dripWidth) * (1f - (y - center.y) / (size - center.y));
                        alpha = Mathf.Max(alpha, dripAlpha * 0.8f);
                    }
                }
                
                alpha = Mathf.Pow(alpha, 0.6f);
                // Premultiplied alpha
                pixels[y * size + x] = new Color(alpha, alpha, alpha, alpha);
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
        
        // Main blob
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float mainRadius = size / 3f;
        
        // Smaller satellite blobs
        int blobCount = Random.Range(3, 6);
        Vector2[] blobCenters = new Vector2[blobCount];
        float[] blobRadii = new float[blobCount];
        
        for (int i = 0; i < blobCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(mainRadius * 0.5f, mainRadius * 1.2f);
            blobCenters[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            blobRadii[i] = Random.Range(size * 0.05f, size * 0.15f);
        }
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = 0f;
                Vector2 pos = new Vector2(x, y);
                
                // Main blob
                float dist = Vector2.Distance(pos, center);
                alpha = Mathf.Max(alpha, 1f - Mathf.Clamp01(dist / mainRadius));
                
                // Satellite blobs
                for (int i = 0; i < blobCount; i++)
                {
                    dist = Vector2.Distance(pos, blobCenters[i]);
                    float blobAlpha = 1f - Mathf.Clamp01(dist / blobRadii[i]);
                    alpha = Mathf.Max(alpha, blobAlpha);
                }
                
                // Noise
                float noise = Mathf.PerlinNoise(x * 0.15f + seed, y * 0.15f) * 0.2f;
                alpha = Mathf.Clamp01(alpha + noise - 0.1f);
                alpha = Mathf.Pow(alpha, 0.6f);
                
                // Premultiplied alpha
                pixels[y * size + x] = new Color(alpha, alpha, alpha, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        Random.state = oldState;
        return tex;
    }
}