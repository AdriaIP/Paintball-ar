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
        
        // Set color (ensure full alpha for cutout rendering)
        Color opaqueColor = new Color(color.r, color.g, color.b, 1f);
        splatRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", opaqueColor);
        propertyBlock.SetColor("_Color", opaqueColor);
        
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
    /// Fade out the splat over time (uses scale shrink since we use alpha cutout)
    /// </summary>
    public void FadeOut(float duration)
    {
        StartCoroutine(FadeOutCoroutine(duration));
    }
    
    private System.Collections.IEnumerator FadeOutCoroutine(float duration)
    {
        float startTime = Time.time;
        Vector3 startScale = transform.localScale;
        
        while (Time.time - startTime < duration)
        {
            float t = (Time.time - startTime) / duration;
            // Shrink the splat instead of fading alpha (works with cutout rendering)
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            
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
