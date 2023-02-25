using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(ConfigurableJoint))]
public class BarController : MonoBehaviour //proportional–integral–derivative controller
{
    public Transform target;
    public Transform origin;
    public Transform stalk; 

    public float pGain;
    public float dGain;

    private float lastError = 0f;


    public void InitBar(Transform _target, Transform _origin, Transform _stalk, float _pGain, float _dGain)
    {
        target = _target;
        origin = _origin;
        stalk = _stalk; 
        pGain = _pGain;
        dGain = _dGain;

        GetComponent<ConfigurableJoint>().anchor = Vector3.zero;
        GetComponent<ConfigurableJoint>().autoConfigureConnectedAnchor = false;
        GetComponent<ConfigurableJoint>().xMotion = ConfigurableJointMotion.Limited;
        GetComponent<ConfigurableJoint>().yMotion = ConfigurableJointMotion.Locked;
        GetComponent<ConfigurableJoint>().zMotion = ConfigurableJointMotion.Locked;
        GetComponent<ConfigurableJoint>().angularXMotion = ConfigurableJointMotion.Locked;
        GetComponent<ConfigurableJoint>().angularYMotion = ConfigurableJointMotion.Locked;
        GetComponent<ConfigurableJoint>().angularZMotion = ConfigurableJointMotion.Locked;
    }

    public void SetInitialPosition()
    {
        stalk.position = origin.position;
        stalk.rotation = origin.rotation;
        GetComponent<Rigidbody>().MovePosition(origin.position);
        GetComponent<Rigidbody>().MoveRotation(origin.rotation);
    }

    public void UpdatePIDForce(float secondsPerUpdate)
    {
        if (target == null)
            return;

        // Calculate the error between the target position and the current position
        float error = Vector3.Distance(target.position, transform.position);

        // Calculate the derivative of the error
        float errorDerivative = (error - lastError) / secondsPerUpdate;

        // Calculate the force needed to move towards the target position
        float force = error * pGain + errorDerivative * dGain;

        // Apply the force to the Rigidbody
        GetComponent<Rigidbody>().AddForce(force * (target.position - transform.position).normalized, ForceMode.Force); //PID Controller

        // Remember the last error for the next FixedUpdate
        lastError = error;


        // If the distance between the origin and the transform went negative, move it
        float distFromOrigin = Vector3.Dot((GetComponent<Rigidbody>().position - origin.position), origin.position);
        if(distFromOrigin < 0)
        {
            GetComponent<Rigidbody>().MovePosition(origin.position);
        }
    }

    public void UpdateConfigJoint(float sliderHeightLimit)
    {
        GetComponent<ConfigurableJoint>().connectedAnchor = origin.transform.position;

        GetComponent<ConfigurableJoint>().axis = Vector3.up; // (currBarOrigin.position - currBarTarget.position); //normal;
        SoftJointLimit sjl = new SoftJointLimit();
        sjl.limit = sliderHeightLimit;
        GetComponent<ConfigurableJoint>().linearLimit = sjl;

        GetComponent<ConfigurableJoint>().angularXMotion = ConfigurableJointMotion.Free;
        GetComponent<ConfigurableJoint>().angularYMotion = ConfigurableJointMotion.Free;
        GetComponent<ConfigurableJoint>().angularZMotion = ConfigurableJointMotion.Free;
        transform.rotation = origin.rotation;
        GetComponent<ConfigurableJoint>().angularXMotion = ConfigurableJointMotion.Locked;
        GetComponent<ConfigurableJoint>().angularYMotion = ConfigurableJointMotion.Locked;
        GetComponent<ConfigurableJoint>().angularZMotion = ConfigurableJointMotion.Locked;
    }

    public void UpdateBarStalk()
    {
        //Debug.Log("updating bar stalk: " + transform.name + ": " + transform.position + " rbPos: " + GetComponent<Rigidbody>().position);
        var dropDown = (origin.up * transform.localScale.y / 2);
        stalk.transform.rotation = origin.rotation;

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
       
    }

}
