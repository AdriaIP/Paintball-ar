using UnityEngine;
public class GoalMover : MonoBehaviour
{
    public GameObject predictionPoint;
    public GameObject hand;
    public LineRenderer lineRenderer;

    private Vector3 targetPoint;
    private Vector3 swingPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        int layerMask = LayerMask.GetMask("Mesh"); // ignore player layer
        Vector3 start = hand.transform.position + hand.transform.forward * 0.2f;

        bool hasHit = Physics.Raycast(start, hand.transform.forward, out hit, 100, layerMask);
        if (hasHit)
        {
            // Debug.Log("Hit: " + hit.collider.name);
            swingPoint = hit.point;

            // store the hit as a world-space target so it won't change when the controller/rig moves
            targetPoint = swingPoint;

            predictionPoint.SetActive(true);
            predictionPoint.transform.position = swingPoint;
            //predictionPoint.GetComponent<Renderer>().material.color = Color.yellow;
        }
        else
        {
            predictionPoint.SetActive(true);
            // store a fallback world-space point
            targetPoint = hand.transform.position + hand.transform.forward * 100;
            predictionPoint.transform.position = targetPoint;
            //predictionPoint.GetComponent<Renderer>().material.color = Color.red;
        }
    }
}
