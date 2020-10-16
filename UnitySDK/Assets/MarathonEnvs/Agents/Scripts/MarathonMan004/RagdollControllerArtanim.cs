using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollControllerArtanim : MonoBehaviour

    //this class does exactly the symetrical of MocapControllerArtanim: it maps animations from a ragdoll to a rigged character
{

    [SerializeField]

	ArticulationBody _articulationBodyRoot;

	private List<ArticulationBody> _articulationbodies = null;

	private List<Transform> _targetPoseTransforms = null;

    private List<MappingOffset> _offsetsRB2targetPoseTransforms = null;


	private List<MappingOffset> _offsetsSource2RB = null;


	[Space(20)]


	[SerializeField]
	float _debugDistance= 0.0f;


	[SerializeField]
	bool _debugWithRigidBody = false;

	[SerializeField]
	Rigidbody _rigidbodyRoot;


	private List<Rigidbody> _rigidbodies = null;



	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }



	MappingOffset SetOffsetRB2targetPose(string rbname, string tname)
	{
		//here we set up:
		// a. the transform of the rigged character output
		// b. the rigidbody of the physical character
		// c. the offset calculated between the rigged character INPUT, and the rigidbody


		if (_targetPoseTransforms == null)
		{
			_targetPoseTransforms = GetComponentsInChildren<Transform>().ToList();
			Debug.Log("the number of transforms  intarget pose is: " + _targetPoseTransforms.Count);

		}


		if (_offsetsRB2targetPoseTransforms == null)
		{
			_offsetsRB2targetPoseTransforms = new List<MappingOffset>();

		}

		if (_articulationbodies == null)
		{
            if (_debugWithRigidBody) { 
			_rigidbodies = _rigidbodyRoot.GetComponentsInChildren<Rigidbody>().ToList();
            }
            else { 
				_articulationbodies = _articulationBodyRoot.GetComponentsInChildren<ArticulationBody>().ToList();
			}
		}


		Transform rb;
		if (_debugWithRigidBody)
		{
			rb = _rigidbodies.First(x => x.name == rbname).transform;
		}else
		{ 
			rb = _articulationbodies.First(x => x.name == rbname).transform;
		
			if (rb == null)
			{
				Debug.LogError("no rigidbody with name " + rbname);

			}
		}

		Transform t = null;
		try
		{

			t = _targetPoseTransforms.First(x => x.name == tname);

		}
		catch (Exception e)
		{
			Debug.LogError("no bone transform with name in target pose" + tname);

		}

		Transform tref = null;
		try
		{

			tref = _targetPoseTransforms.First(x => x.name == tname);

		}
		catch (Exception e)
		{
			Debug.LogError("no bone transform with name in input pose " + tname);

		}

		//from refPose to Physical body:
		//q_{physical_body} = q_{offset} * q_{refPose}
		//q_{offset} = q_{physical_body} * Quaternion.Inverse(q_{refPose})

		//Quaternion qoffset = rb.transform.localRotation * Quaternion.Inverse(tref.localRotation);

		Quaternion qoffset = rb.transform.rotation * Quaternion.Inverse(tref.rotation);


		//from physical body to targetPose:
		//q_{target_pose} = q_{offset2} * q_{physical_body}
		//q_{offset2} = Quaternion.Inverse(q_{offset})

		MappingOffset r;
		if (_debugWithRigidBody)
		{
			Rigidbody myrb = rb.GetComponent<Rigidbody>();
			r = new MappingOffset(t, myrb, Quaternion.Inverse(qoffset));
			r.SetAsRagdollcontrollerDebug(_debugWithRigidBody);
		}
		else 
		{
			ArticulationBody myrb = rb.GetComponent<ArticulationBody>();
			r = new MappingOffset(t, myrb, Quaternion.Inverse(qoffset));
		}




		_offsetsRB2targetPoseTransforms.Add(r);
		return r;
	}


	void MimicPhysicalChar()
	{

		try
		{
			foreach (MappingOffset o in _offsetsRB2targetPoseTransforms)
			{
				o.UpdateRotation();
			}
		}
		catch (Exception e)
		{
			Debug.Log("not calibrated yet...");

		}


	}

    private void FixedUpdate()
    {
		MimicAnimationArtanim();
    }
    void MimicAnimationArtanim()
	{


		if (_offsetsRB2targetPoseTransforms == null)
		{
			MappingOffset o = SetOffsetRB2targetPose("butt", "mixamorig:Hips");
			o.SetAsRoot(true, _debugDistance);
			SetOffsetRB2targetPose("lower_waist", "mixamorig:Spine");
			SetOffsetRB2targetPose("upper_waist", "mixamorig:Spine1");
			SetOffsetRB2targetPose("torso", "mixamorig:Spine2");
			SetOffsetRB2targetPose("head", "mixamorig:Head");


			SetOffsetRB2targetPose("left_shoulder_joint", "mixamorig:LeftShoulder");

			SetOffsetRB2targetPose("left_upper_arm_joint", "mixamorig:LeftArm");
			SetOffsetRB2targetPose("left_larm_joint", "mixamorig:LeftForeArm");

			//	SetOffsetRB2targetPose("left_hand", "mixamorig:LeftHand");
			// hands do not have rigidbodies


			SetOffsetRB2targetPose("right_shoulder_joint", "mixamorig:RightShoulder");

			SetOffsetRB2targetPose("right_upper_arm_joint", "mixamorig:RightArm");
			SetOffsetRB2targetPose("right_larm_joint", "mixamorig:RightForeArm");
		//	SetOffsetRB2targetPose("right_hand", "mixamorig:RightHand");

//			SetOffsetRB2targetPose("left_thigh", "mixamorig:LeftUpLeg");

			SetOffsetRB2targetPose("left_thigh_joint", "mixamorig:LeftUpLeg");

//			SetOffsetRB2targetPose("left_shin", "mixamorig:LeftLeg");
			SetOffsetRB2targetPose("left_shin_joint", "mixamorig:LeftLeg");
			SetOffsetRB2targetPose("left_left_foot", "mixamorig:LeftToeBase");
		//	SetOffsetRB2targetPose("right_left_foot", "mixamorig:LeftToeBase");


//			SetOffsetRB2targetPose("right_thigh", "mixamorig:RightUpLeg");
			SetOffsetRB2targetPose("right_thigh_joint", "mixamorig:RightUpLeg");
			//SetOffsetRB2targetPose("right_shin", "mixamorig:RightLeg");
			SetOffsetRB2targetPose("right_shin_joint", "mixamorig:RightLeg");


			SetOffsetRB2targetPose("left_left_foot", "mixamorig:RightToeBase");
		//	SetOffsetRB2targetPose("left_right_foot", "mixamorig:RightToeBase");



		}
		else
		{
			MimicPhysicalChar();


		}



	}


}
