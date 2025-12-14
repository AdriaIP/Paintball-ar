using UnityEngine;

/// <summary>
/// Individual paint splat behavior.
/// Handles color, texture, and lifetime.
/// </summary>
public class PaintSplat : MonoBehaviour
{
    private Renderer splatRenderer;
    private MaterialPropertyBlock propertyBlock;
    private float lifetime;
    private float spawnTime;
    private bool hasLifetime;
    
    private void Awake()
    {
        splatRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }
    
    public void Initialize(Color color, Texture2D texture = null, float lifetime = 0f)
    {
        if (splatRenderer == null)
        {
            splatRenderer = GetComponent<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
        }
        
        // Set color
        splatRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color);
        
        // Set texture if provided
        if (texture != null)
        {
            propertyBlock.SetTexture("_BaseMap", texture);
            propertyBlock.SetTexture("_MainTex", texture);
        }
        
        splatRenderer.SetPropertyBlock(propertyBlock);
        
        // Setup lifetime
        this.lifetime = lifetime;
        this.hasLifetime = lifetime > 0;
        this.spawnTime = Time.time;
    }
    
    private void Update()
    {
        if (hasLifetime && Time.time - spawnTime > lifetime)
        {
            if (PaintSplatManager.Instance != null)
            {
                PaintSplatManager.Instance.RecycleSplat(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
    
    /// <summary>
    /// Fade out the splat over time
    /// </summary>
    public void FadeOut(float duration)
    {
        StartCoroutine(FadeOutCoroutine(duration));
    }
    
    private System.Collections.IEnumerator FadeOutCoroutine(float duration)
    {
        float startTime = Time.time;
        Color startColor = splatRenderer.material.color;
        
        while (Time.time - startTime < duration)
        {
            float t = (Time.time - startTime) / duration;
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(startColor.a, 0f, t);
            
            splatRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", newColor);
            splatRenderer.SetPropertyBlock(propertyBlock);
            
            yield return null;
        }
        
        if (PaintSplatManager.Instance != null)
        {
            PaintSplatManager.Instance.RecycleSplat(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
