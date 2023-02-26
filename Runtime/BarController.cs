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


    private Vector3 prevPos; 

    public void InitBar(Transform _target, Transform _origin, Transform _stalk)
    {
        target = _target;
        origin = _origin;
        stalk = _stalk; 

        GetComponent<Rigidbody>().isKinematic = true; 
    }

    public void SetInitialPosition()
    {
        stalk.position = origin.position;
        stalk.rotation = origin.rotation;
        GetComponent<Rigidbody>().MovePosition(origin.position);
        GetComponent<Rigidbody>().MoveRotation(origin.rotation);
    }

    public void MoveTowardsTarget()
    {
        GetComponent<Rigidbody>().MovePosition(Vector3.Lerp(transform.position, target.position, 0.5f));
    }


    public void UpdateBarStalk(float heightGlowStrength)
    {
        var dropDown = (origin.up * transform.localScale.y / 2);
        stalk.transform.rotation = origin.rotation; //This has nothing to do with the physics going through each other
        //if moving below origin, prevent it
        if (Vector3.Dot((origin.position - transform.position), origin.up) > 0)
        {
            stalk.transform.position = origin.position - dropDown;
            stalk.localScale = new Vector3(stalk.localScale.x, transform.localScale.y, stalk.localScale.z);
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

}
