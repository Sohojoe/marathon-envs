using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MappingOffset
{
    //string _tName;
    Transform _t;
    //string _rbName;
    Rigidbody _rb;

    //the ragdoll in the physics based anim uses articulated bodies, instead of rigidbodies:
    ArticulationBody _ab;

    Quaternion _offsetRB2Bone;



    private bool _isRoot = false;
    private Vector3 _debugDistance = Vector3.zero;
    private bool _debugWithRigidBody;

    //this variables define two completely different modes. In one it updates rigidbodies from transforms, in the other it updates transforms from articulationbodies. 
    //see contstructors and function UpdateAnimation
    private bool _updateRigidBodies = false;

    Transform _tson = null;



    public MappingOffset(Transform t, Rigidbody rb, Quaternion offset)
    {

        _t = t;
        _rb = rb;
        _offsetRB2Bone = offset;

        _ab = null;

        _updateRigidBodies = true;




    }




    //  Vector3 pos = (animEndBone.transform.position - animStartBone.transform.position);
    //  target.transform.position = animStartBone.transform.position + (pos/2);
    //target.transform.localPosition += offset;
    //target.transform.rotation = animStartBone.transform.rotation* rotationOffset;





    public MappingOffset(Transform t, ArticulationBody ab, Quaternion offset)
    {

        _t = t;
        _rb = null;
        _offsetRB2Bone = offset;

        _ab = ab;

        _updateRigidBodies = false;
    }



    public void SetAsRoot(bool b = true, float offset = 0.0f)
    {

        _isRoot = b;
        _debugDistance.z = -offset;

    }


    //this is a special function used inside RagdollControllerArtanim, it is only used to check the mapping between physical and ragdoll characters works well
    public void SetAsRagdollcontrollerDebug(bool debugWithRigidBody)
    {
        _debugWithRigidBody = debugWithRigidBody;


    }


    //public void SetSon(Transform son) {

    //    if (!_updateRigidBodies)
    //        Debug.LogError("using son transform only makes sense when we are in the mode that updates the rigidbodies form the transforms. Please check how you initialize this class");

    //    _tson = son;
    
    
    //}


    public bool UpdateRigidBodies { get => _updateRigidBodies; set => _updateRigidBodies = value; }

    public void UpdateRotation()
    {

        if (_updateRigidBodies)
        {
          
           

            if (_debugWithRigidBody)
            {
                _t.transform.localRotation = _offsetRB2Bone * _rb.transform.localRotation;

                if (_isRoot)
                {
                    _t.transform.rotation = _rb.rotation;
                    _t.transform.position = _rb.position + _debugDistance;


                }



            }
            else
            {
                //THE MAIN OPERATION, used most frequently when called this function:
                if (_isRoot)
                {
                    _rb.transform.rotation = _t.rotation;
                    _rb.transform.position = _t.position + _debugDistance;


                }
                else if (_tson != null)
                {

                    //the center of this thing is in the wrong position
                    //  Vector3 pos = (_tson.position - _t.position);
                    //  _rb.position = _t.transform.position + (pos/2) +_debugDistance;

                    //target.transform.rotation = animStartBone.transform.rotation* rotationOffset;

                }
                else { 
                    // _rb.transform.rotation = _offsetRB2Bone * _t.rotation;

                    //using the local rotation makes sure we do take into account rotation of their parents (for example, the call of this function for the arm, when rotating the spine)
                    _rb.transform.localRotation = _offsetRB2Bone * _t.localRotation;
                }
            }
        }
        else
        {
            //_t.rotation = _offsetRB2Bone * _rb.transform.rotation;
            _t.rotation = _offsetRB2Bone * _ab.transform.rotation;
            if (_isRoot)
            {
                _t.position = _ab.transform.position + _debugDistance;

                //TEST TEST TEST. we override the offset decided before to make it match
                //_t.rotation = _ab.transform.rotation; 

            }
        }
    }

  



}


