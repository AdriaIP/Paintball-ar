using Meta.WitAi;
using System;
using UnityEngine;




public class HandPinch : MonoBehaviour
{
    public OVRHand rightHand;
    public GameObject rightHandObject;
    bool isPinching;
    bool wasPinching;
    public GameObject ball;
    public float distance = 0.12f;
    Rigidbody b_Rigidbody;
    private Vector3 previousPosition;
    public Vector3 velocity;
    public Vector3 velocity2;
    public Vector3 velocity3;
    public Vector3 velocity4;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        b_Rigidbody = ball.GetComponent<Rigidbody>();
        isPinching = false;
        previousPosition = rightHandObject.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        velocity4 = velocity3;
        velocity3 = velocity2;
        velocity2 = velocity;
        velocity = (rightHandObject.transform.position - previousPosition) / Time.deltaTime;
        previousPosition = rightHandObject.transform.position;

        Debug.Log(velocity);
        if (rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > 0.13)
        {
            isPinching = true;

            //if (!wasPinching)
            //{
            //    b_Rigidbody.useGravity = false;
            //    b_Rigidbody.linearVelocity = Vector3.zero;
            //    b_Rigidbody.MovePosition(rightHandObject.transform.position + rightHandObject.transform.forward * distance);

            //    FixedJoint fixedJoint = ball.AddComponent<FixedJoint>();
            //    fixedJoint.connectedBody = rightHandObject.GetComponent<Rigidbody>();
            //}

            //Debug.Log("pinching");
            //Vector3 obj_velocity = (lastPosition - rightHandObject.transform.position) * Time.deltaTime;
            if (!wasPinching)
            {
                b_Rigidbody.useGravity = false;
                b_Rigidbody.MovePosition(rightHandObject.transform.position + rightHandObject.transform.forward * distance);
                b_Rigidbody.linearVelocity = Vector3.zero;
            }


            ball.transform.position = rightHandObject.transform.position+ rightHandObject.transform.forward* distance;
        }
        else
        {
            isPinching = false;
            if (wasPinching)
            {
                
                b_Rigidbody.linearVelocity = (velocity+ velocity2 + velocity3)/ 2;
                b_Rigidbody.useGravity = true;

            }
        }
        wasPinching = isPinching;
    }
}
