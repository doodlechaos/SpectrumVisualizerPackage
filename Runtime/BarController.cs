using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

//[RequireComponent(typeof(Rigidbody), typeof(ConfigurableJoint))]
[RequireComponent(typeof(Rigidbody))]
public class BarController : MonoBehaviour //proportional–integral–derivative controller
{
    public Transform target;
    public Transform origin;
    public Transform stalk; 

    public float pGain;
    public float dGain;

    private float lastError = 0f;

    private Vector3 prevPos; 

    public void InitBar(Transform _target, Transform _origin, Transform _stalk, float _pGain, float _dGain)
    {
        target = _target;
        origin = _origin;
        stalk = _stalk; 
        pGain = _pGain;
        dGain = _dGain;

        GetComponent<Collider>().isTrigger = false;
        GetComponent<Rigidbody>().isKinematic = true; 
    }

    public void SetInitialPosition()
    {
        stalk.position = origin.position;
        stalk.rotation = origin.rotation;
        GetComponent<Rigidbody>().MovePosition(origin.position);
        GetComponent<Rigidbody>().MoveRotation(origin.rotation);
    }

    public void UpdatePIDForce(float secondsPerUpdate, float overallForceScaler)
    {
        if (target == null)
            return;


        GetComponent<Rigidbody>().MovePosition(Vector3.Lerp(transform.position, target.position, 0.5f));

        prevPos = transform.position;

        /*
        // Calculate the error between the target position and the current position
        float error = Vector3.Distance(target.position, transform.position);

        // Calculate the derivative of the error
        float errorDerivative = (error - lastError) / secondsPerUpdate;

        // Calculate the force needed to move towards the target position
        float force = error * pGain + errorDerivative * dGain * overallForceScaler;

        // Apply the force to the Rigidbody
        GetComponent<Rigidbody>().AddForce(force * (target.position - transform.position).normalized , ForceMode.Force); //PID Controller

        // Remember the last error for the next FixedUpdate
        lastError = error;


        // If the distance between the origin and the transform went negative, move it
        float distFromOrigin = Vector3.Dot((GetComponent<Rigidbody>().position - origin.position), origin.position);
        if(distFromOrigin < 0)
        {
            GetComponent<Rigidbody>().MovePosition(origin.position);
        }
        */
    }


    public void UpdateBarStalk(float heightGlowStrength)
    {
        //Debug.Log("updating bar stalk: " + transform.name + ": " + transform.position + " rbPos: " + GetComponent<Rigidbody>().position);
        var dropDown = (origin.up * transform.localScale.y / 2);
        stalk.transform.rotation = origin.rotation; //This has nothing to do with the physics going through each other
        //if moving below origin, prevent it
        if (Vector3.Dot((origin.position - transform.position), origin.up) > 0)
        {
            //Debug.LogError(transform.name + " detected below line: " + (origin.position - transform.position));
            stalk.transform.position = origin.position - dropDown;
            stalk.localScale = new Vector3(stalk.localScale.x, transform.localScale.y, stalk.localScale.z);

            if ((origin.position - transform.position).y > 0.02f)
            {
                //EditorApplication.isPaused = true;
            }
            return; 
        }

        //Else if above, change scale and position accordingly
        float length = Vector3.Distance(origin.position, transform.position);
        stalk.localScale = new Vector3(stalk.localScale.x, length + transform.localScale.y, stalk.localScale.z);
        stalk.transform.position = Vector3.Lerp(origin.position, transform.position, 0.5f) - dropDown; //Should be half way between the origin and cap

        //Update the color emission based on how long the length is
        Material currMat = GetComponent<Renderer>().sharedMaterial;
        currMat.SetVector("_EmissionColor", currMat.color * length / 5);

        Material currMatStalk = stalk.GetComponent<Renderer>().sharedMaterial;
        currMatStalk.SetVector("_EmissionColor", currMat.color * length * heightGlowStrength);
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Detected collision: " + other.gameObject.name); 
    }

    private void OnTriggerStay(Collider other)
    {
        
    }

}
