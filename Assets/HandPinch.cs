using System;
using UnityEngine;




public class HandPinch : MonoBehaviour
{
    public OVRHand rightHand;
    bool isPinching = false;
    public GameObject ball;
    Rigidbody b_Rigidbody;
    Vector3 lastPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        b_Rigidbody = ball.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        isPinching = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        Debug.Log(rightHand.transform.position);
        if (isPinching )
        {
            Debug.Log("pinching");
            Vector3 obj_velocity = (lastPosition - rightHand.transform.position) * Time.deltaTime;
            b_Rigidbody.MovePosition(transform.position);
            lastPosition = transform.position;

            b_Rigidbody.linearVelocity = obj_velocity;
        }
    }
}
