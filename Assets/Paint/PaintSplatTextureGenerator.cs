using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to generate procedural paint splat textures.
/// </summary>
public class PaintSplatTextureGenerator : MonoBehaviour
{
    [Header("Texture Settings")]
    public int textureSize = 256;
    public int splatCount = 5;
    
    [Header("Generated Textures (Read Only)")]
    public Texture2D[] generatedTextures;
    
    /// <summary>
    /// Generate a circular splat texture with soft edges
    /// </summary>
    public static Texture2D GenerateCircleSplat(int size)
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
                
                // Soft falloff
                alpha = Mathf.Pow(alpha, 0.5f);
                
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
    
    /// <summary>
    /// Generate an irregular splat texture
    /// </summary>
    public static Texture2D GenerateIrregularSplat(int size, int seed = 0)
    {
        Random.State oldState = Random.state;
        if (seed != 0) Random.InitState(seed);
        
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float baseRadius = size / 2f * 0.8f;
        
        // Generate noise points for irregular shape
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
                
                // Calculate irregular radius at this angle
                float radiusAtAngle = baseRadius;
                for (int i = 0; i < numBumps; i++)
                {
                    float angleDiff = Mathf.DeltaAngle(angle * Mathf.Rad2Deg, bumpAngles[i] * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                    float influence = Mathf.Exp(-angleDiff * angleDiff * 2f);
                    radiusAtAngle += baseRadius * bumpAmounts[i] * influence;
                }
                
                float alpha = 1f - Mathf.Clamp01(dist / radiusAtAngle);
                
                // Add some noise
                float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.3f;
                alpha = Mathf.Clamp01(alpha + noise - 0.15f);
                
                // Soft falloff
                alpha = Mathf.Pow(alpha, 0.7f);
                
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        Random.state = oldState;
        return tex;
    }
    
    /// <summary>
    /// Generate a drip/streak splat texture
    /// </summary>
    public static Texture2D GenerateDripSplat(int size, int seed = 0)
    {
        Random.State oldState = Random.state;
        if (seed != 0) Random.InitState(seed);
        
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        
        Color[] pixels = new Color[size * size];
        
        // Start with a base circle
        Vector2 center = new Vector2(size / 2f, size / 3f);
        float radius = size / 4f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = 0f;
                
                // Main blob
                float dist = Vector2.Distance(new Vector2(x, y), center);
                alpha = Mathf.Max(alpha, 1f - Mathf.Clamp01(dist / radius));
                
                // Drip downward
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
                
                // Soft falloff
                alpha = Mathf.Pow(alpha, 0.6f);
                
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        Random.state = oldState;
        return tex;
    }
    
    [ContextMenu("Generate Textures")]
    public void GenerateTextures()
    {
        generatedTextures = new Texture2D[splatCount];
        
        for (int i = 0; i < splatCount; i++)
        {
            int type = i % 3;
            switch (type)
            {
                case 0:
                    generatedTextures[i] = GenerateCircleSplat(textureSize);
                    generatedTextures[i].name = $"SplatCircle_{i}";
                    break;
                case 1:
                    generatedTextures[i] = GenerateIrregularSplat(textureSize, i * 12345);
                    generatedTextures[i].name = $"SplatIrregular_{i}";
                    break;
                case 2:
                    generatedTextures[i] = GenerateDripSplat(textureSize, i * 67890);
                    generatedTextures[i].name = $"SplatDrip_{i}";
                    break;
            }
        }
        
        Debug.Log($"Generated {splatCount} splat textures");
    }
}
