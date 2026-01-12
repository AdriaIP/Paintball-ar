using UnityEngine;

/// <summary>
/// Tracks whether a ball is "wet" (has paint) and how many paint traces it can leave.
/// After maxHits, the ball becomes dry (white) until painted again.
/// </summary>
public class WetBall : MonoBehaviour
{
    [Header("Paint Settings")]
    [Tooltip("Maximum number of paint traces before becoming dry")]
    public int maxHits = 5;
    
    [Tooltip("Color when dry (no paint)")]
    public Color dryColor = Color.white;
    
    [Header("State (Read Only)")]
    [SerializeField] private bool isWet = false;
    [SerializeField] private Color currentPaintColor;
    [SerializeField] private int remainingHits = 0;
    
    private Renderer ballRenderer;
    private MaterialPropertyBlock propertyBlock;
    
    public bool IsWet => isWet;
    public Color PaintColor => currentPaintColor;
    public int RemainingHits => remainingHits;
    
    private void Awake()
    {
        ballRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }
    
    /// <summary>
    /// Paint the ball with a color, making it wet and resetting hit count.
    /// </summary>
    public void Paint(Color color)
    {
        currentPaintColor = color;
        isWet = true;
        remainingHits = maxHits;
        
        SetBallColor(color);
    }
    
    /// <summary>
    /// Called when the ball leaves a paint trace.
    /// Returns true if paint was applied, false if ball was dry.
    /// </summary>
    public bool UsePaint(out Color paintColor)
    {
        paintColor = currentPaintColor;
        
        if (!isWet || remainingHits <= 0)
        {
            paintColor = Color.clear;
            return false;
        }
        
        remainingHits--;
        
        // Become dry after all hits used
        if (remainingHits <= 0)
        {
            MakeDry();
        }
        
        return true;
    }
    
    /// <summary>
    /// Make the ball dry (white, no paint)
    /// </summary>
    public void MakeDry()
    {
        isWet = false;
        currentPaintColor = dryColor;
        remainingHits = 0;
        
        SetBallColor(dryColor);
    }
    
    private void SetBallColor(Color color)
    {
        if (ballRenderer == null) return;
        
        // Use MaterialPropertyBlock to avoid creating material instances
        ballRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color); // Fallback for Standard shader
        ballRenderer.SetPropertyBlock(propertyBlock);
    }
}
