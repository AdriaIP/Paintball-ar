using UnityEngine;

/// <summary>
/// Handles ball collisions - painting the ball when hitting paint sources,
/// and leaving paint traces when a wet ball hits surfaces.
/// </summary>
public class SphereCollision : MonoBehaviour
{
    [Header("Paint Colors")]
    [Tooltip("Make sure alpha is set to 1!")]
    public Color blue = new Color(0.2f, 0.4f, 1f, 1f); // Bright blue with full alpha
    public Color red = new Color(1f, 0.2f, 0.2f, 1f);  // Bright red with full alpha
    
    [Header("Paint Trace Settings")]
    [Tooltip("Tags that should NOT receive paint traces")]
    public string[] ignoredTags = { "BluePaint", "RedPaint", "Player", "Ball" };
    
    [Tooltip("Minimum impact velocity to leave a paint trace")]
    public float minImpactVelocity = 0.5f;
    
    [Tooltip("Spawn multiple splats on hard impacts")]
    public bool multiSplatOnHardImpact = true;
    
    [Tooltip("Velocity threshold for extra splats")]
    public float hardImpactVelocity = 3f;
    
    private WetBall wetBall;
    
    private void Start()
    {
        // Get or add WetBall component
        wetBall = GetComponent<WetBall>();
        if (wetBall == null)
        {
            wetBall = gameObject.AddComponent<WetBall>();
        }
    }

    private void OnCollisionEnter(UnityEngine.Collision collision)
    {
        string tag = collision.gameObject.tag;
        
        // Check if hit a paint source
        if (tag == "BluePaint")
        {
            PaintBall(blue);
            return;
        }
        else if (tag == "RedPaint")
        {
            PaintBall(red);
            return;
        }
        
        // Check if this tag should be ignored for paint traces
        if (IsTagIgnored(tag))
        {
            return;
        }
        
        // Try to leave paint trace if ball is wet
        TryLeavePaintTrace(collision);
    }
    
    private void PaintBall(Color color)
    {
        if (wetBall != null)
        {
            wetBall.Paint(color);
        }
        else
        {
            // Fallback if no WetBall component
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }
    }
    
    private bool IsTagIgnored(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;
        
        foreach (string ignoredTag in ignoredTags)
        {
            if (tag == ignoredTag) return true;
        }
        return false;
    }
    
    private void TryLeavePaintTrace(UnityEngine.Collision collision)
    {
        if (wetBall == null || !wetBall.IsWet) return;
        
        // Check impact velocity
        float impactVelocity = collision.relativeVelocity.magnitude;
        if (impactVelocity < minImpactVelocity) return;
        
        // Use paint from ball
        if (!wetBall.UsePaint(out Color paintColor)) return;
        
        // Spawn paint splat(s)
        SpawnPaintSplats(collision, paintColor, impactVelocity);
    }
    
    private void SpawnPaintSplats(UnityEngine.Collision collision, Color color, float impactVelocity)
    {
        if (PaintSplatManager.Instance == null)
        {
            Debug.LogWarning("PaintSplatManager not found in scene. Paint traces disabled.");
            return;
        }
        
        // Get contact points
        ContactPoint[] contacts = new ContactPoint[collision.contactCount];
        collision.GetContacts(contacts);
        
        if (contacts.Length == 0) return;
        
        // Main splat at first contact point
        PaintSplatManager.Instance.SpawnSplat(contacts[0], color);
        
        // Extra splats on hard impacts
        if (multiSplatOnHardImpact && impactVelocity >= hardImpactVelocity)
        {
            int extraSplats = Mathf.Min(Mathf.FloorToInt(impactVelocity / hardImpactVelocity), 3);
            
            for (int i = 0; i < extraSplats; i++)
            {
                // Offset position slightly for variety
                Vector3 offset = Random.insideUnitSphere * 0.05f;
                Vector3 splatPos = contacts[0].point + offset;
                
                // Slightly vary the color
                Color variedColor = color;
                float variation = Random.Range(-0.1f, 0.1f);
                variedColor.r = Mathf.Clamp01(variedColor.r + variation);
                variedColor.g = Mathf.Clamp01(variedColor.g + variation);
                variedColor.b = Mathf.Clamp01(variedColor.b + variation);
                
                PaintSplatManager.Instance.SpawnSplat(splatPos, contacts[0].normal, variedColor, collision.collider);
            }
        }
        
        // Additional splats at other contact points (for multi-surface impacts)
        for (int i = 1; i < Mathf.Min(contacts.Length, 3); i++)
        {
            // Slightly different shade for each contact
            Color variedColor = color * Random.Range(0.85f, 1f);
            variedColor.a = color.a;
            
            PaintSplatManager.Instance.SpawnSplat(contacts[i], variedColor);
        }
    }
}
