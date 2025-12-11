using UnityEngine;

public class Collision : MonoBehaviour
{
    public Color blue;
    public Color red;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(UnityEngine.Collision collision)
    {

        if (collision.gameObject.CompareTag("BluePaint"))
        {
            Renderer renderer = GetComponent<Renderer>();
            renderer.material.color = blue;
        }
        else if (collision.gameObject.CompareTag("RedPaint"))
        {
            Renderer renderer = GetComponent<Renderer>();
            renderer.material.color = red;
        }
        else
        {
            // Pintar taca
        }
    }
}
