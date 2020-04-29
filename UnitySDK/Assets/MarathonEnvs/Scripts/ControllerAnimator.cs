using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;


public class ControllerAnimator : MonoBehaviour, IOnSensorCollision 
{
	public List<float> SensorIsInTouch;
	List<GameObject> _sensors;

	internal Animator anim;

	[Range(0f,1f)]
	public float NormalizedTime;    
	public float Lenght;
	public bool IsLoopingAnimation;
	private List<Rigidbody> _rigidbodies;
	private List<Transform> _transforms;

	public bool CameraFollowMe;
	public Transform CameraTarget;


	void Awake()
    {
        SetupSensors();
        anim = GetComponent<Animator>();
        anim.Play("Record",0, NormalizedTime);
        anim.Update(0f);

		var masters = FindObjectsOfType<StyleTransfer002Master>().ToList();
		if (masters.Count(x=>x.CameraFollowMe) < 1)
			CameraFollowMe = true;
        if (CameraFollowMe){
            var camera = FindObjectOfType<Camera>();
            var follow = camera.GetComponent<SmoothFollow>();
            follow.target = CameraTarget;
        }

    }
	void SetupSensors()
	{
		_sensors = GetComponentsInChildren<SensorBehavior>()
			.Select(x=>x.gameObject)
			.ToList();
		SensorIsInTouch = Enumerable.Range(0,_sensors.Count).Select(x=>0f).ToList();
	}

    void FixedUpdate()
    {
		AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
		AnimatorClipInfo[] clipInfo = anim.GetCurrentAnimatorClipInfo(0);
		Lenght = stateInfo.length;
		NormalizedTime = stateInfo.normalizedTime;
		IsLoopingAnimation = stateInfo.loop;
		var timeStep = stateInfo.length * stateInfo.normalizedTime;
		var endTime = 1f;
		if (IsLoopingAnimation)
			endTime = 3f;
		// if (NormalizedTime <= endTime) {
		// }        
        MimicAnimation();
    }

	public void MimicAnimation(bool skipIfLearning = false)
	{
		if (!anim.enabled)
			return;

        // MimicBone("butt", "mixamorig:Hips", new Vector3(.01f, -.057f, .004f), Quaternion.Euler(90, 88.2f, 88.8f));
        // MimicBone("butt", 			"mixamorig:Hips", 			new Vector3(.0f, .0f, .0f), 			Quaternion.Euler(90, 88.2f, 88.8f));
        MimicBone("butt", 			"mixamorig:Hips", 			new Vector3(.0f, -.055f, .0f), 			Quaternion.Euler(90, 0f, 0f));
        MimicBone("lower_waist",    "mixamorig:Spine",          new Vector3(.0f, .0153f, .0f), 			Quaternion.Euler(90, 0f, 0f));
        // MimicBone("upper_waist",    "mixamorig:Spine1",         new Vector3(.0f, .0465f, .0f), 			Quaternion.Euler(90, 0f, 0f));
        MimicBone("torso",          "mixamorig:Spine2",         new Vector3(.0f, .04f, .0f), 			Quaternion.Euler(90, 0f, 0f));
		//Quaternion.Euler(90, -90f, 180f));
        // MimicBone("head",           "mixamorig:Head",           new Vector3(.0f, .05f, .0f), 			Quaternion.Euler(0, 0f, 0f));

        MimicBone("left_upper_arm",   "mixamorig:LeftArm", "mixamorig:LeftForeArm", new Vector3(.0f, .0f, .0f), Quaternion.Euler(0, 45, 180));
        MimicBone("left_larm",        "mixamorig:LeftForeArm",  "mixamorig:LeftHand", new Vector3(.0f, .0f, .0f), Quaternion.Euler(0, -180-45, 180));
        //MimicBone("left_hand",        "mixamorig:LeftHand", new Vector3(.0f, .0f, .0f), 			Quaternion.Euler(0, 90, 90+180));
        
        MimicBone("right_upper_arm",  "mixamorig:RightArm", "mixamorig:RightForeArm",      new Vector3(.0f, .0f, .0f), Quaternion.Euler(0, 180-45, 180));
        MimicBone("right_larm",       "mixamorig:RightForeArm", "mixamorig:RightHand",  new Vector3(.0f, .0f, .0f), Quaternion.Euler(0, 90-45, 180));
        // MimicBone("right_hand",       "mixamorig:RightHand",      new Vector3(.0f, .0f, .0f), Quaternion.Euler(0, 180-90, -90));

        MimicBone("left_thigh",       "mixamorig:LeftUpLeg",  "mixamorig:LeftLeg",    new Vector3(.0f, .0f, .0f), 			Quaternion.Euler(0, 0, 180));
        MimicBone("left_shin",        "mixamorig:LeftLeg",    "mixamorig:LeftFoot",   new Vector3(.0f, .02f, .0f), 			Quaternion.Euler(0, 0, 180));
        // MimicBone("left_left_foot",   "mixamorig:LeftToeBase",    new Vector3(.024f, .044f, -.06f), 			Quaternion.Euler(3, -90, 180));//3));
        // MimicBone("right_left_foot",  "mixamorig:LeftToeBase",    new Vector3(-.024f, .044f, -.06f),  			Quaternion.Euler(-8, -90, 180));//-8));
        // MimicLeftFoot("left_left_foot",   new Vector3(.024f, -.01215f, -.06f), 			Quaternion.Euler(3, -90, 180));//3));
        // MimicLeftFoot("right_left_foot",  new Vector3(-.024f, -.01215f, -.06f),  			Quaternion.Euler(-8, -90, 180));//-8));
        // MimicLeftFoot("left_left_foot",   new Vector3(-.024f, -.01215f, -.06f), 			Quaternion.Euler(-8, -90, 180));//3));

        MimicBone("right_thigh",      "mixamorig:RightUpLeg", "mixamorig:RightLeg", new Vector3(.0f, .0f, .0f), 			Quaternion.Euler(0, 0, 180));
        MimicBone("right_shin",       "mixamorig:RightLeg",   "mixamorig:RightFoot", new Vector3(.0f, .02f, .0f), 			Quaternion.Euler(0, 0, 180));
        // MimicBone("right_right_foot", "mixamorig:RightToeBase",   new Vector3(.024f, .044f, -.06f),  			Quaternion.Euler(3, -90, 180));//3));
        // MimicBone("left_right_foot",  "mixamorig:RightToeBase",   new Vector3(-.024f, .044f, -.06f), 		Quaternion.Euler(-8, -90, 180));//-8));
        // MimicRightFoot("right_right_foot", new Vector3(.024f, .044f, -.06f),  			Quaternion.Euler(3, -90, 180));//3));
        // MimicRightFoot("left_right_foot",  new Vector3(-.024f, .044f, -.06f), 		Quaternion.Euler(-8, -90, 180));//-8));
        // MimicRightFoot("right_right_foot", new Vector3(.024f, -.01215f, -.06f),  			Quaternion.Euler(3, -90, 180));//3));
        // MimicRightFoot("left_right_foot",  new Vector3(-.024f, -.01215f, -.06f), 		Quaternion.Euler(-8, -90, 180));//-8));
        // MimicRightFoot("right_right_foot", new Vector3(.024f, -.01215f, -.06f),  			Quaternion.Euler(3, -90, 180));//3));
        // MimicRightFoot("right_right_foot", new Vector3(.0243f, -.0f, -.0243f),  			Quaternion.Euler(3, -90, 180));//3));
        // MimicLeftFoot("left_left_foot",   new Vector3(-.0243f, -.0f, -.0243f), 			Quaternion.Euler(-8, -90, 180));//3));
        MimicRightFoot("right_right_foot", new Vector3(.0f, -.0f, -.0f),  			Quaternion.Euler(3, -90, 180));//3));
        MimicLeftFoot("left_left_foot",   new Vector3(-.0f, -.0f, -.0f), 			Quaternion.Euler(-8, -90, 180));//3));
		

	}
	void MimicBone(string name, string bodyPartName, Vector3 offset, Quaternion rotationOffset)
	{
		if (_rigidbodies == null || _transforms == null)
		{
			_rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
			_transforms = GetComponentsInChildren<Transform>().ToList();
		}

		var bodyPart = _transforms.First(x=>x.name == bodyPartName);
		var target = _rigidbodies.First(x=>x.name == name);

		target.transform.position = bodyPart.transform.position + offset;
		target.transform.rotation = bodyPart.transform.rotation * rotationOffset;
	}

	void MimicBone(string name, string animStartName, string animEndtName, Vector3 offset, Quaternion rotationOffset)
	{
		if (_rigidbodies == null || _transforms == null)
		{
			_rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
			_transforms = GetComponentsInChildren<Transform>().ToList();
		}


		var animStartBone = _transforms.First(x=>x.name == animStartName);
		var animEndBone = _transforms.First(x=>x.name == animEndtName);
		var target = _rigidbodies.First(x=>x.name == name);

		var pos = (animEndBone.transform.position - animStartBone.transform.position);
		target.transform.position = animStartBone.transform.position + (pos/2) + offset;
		target.transform.rotation = animStartBone.transform.rotation * rotationOffset;
	}
	[Range(0f,1f)]
	public float toePositionOffset = .3f;
	[Range(0f,1f)]
	public float toeRotationOffset = .7f;
	void MimicLeftFoot(string name, Vector3 offset, Quaternion rotationOffset)
	{
		string animStartName = "mixamorig:LeftFoot";
		// string animEndtName = "mixamorig:LeftToeBase";
		string animEndtName = "mixamorig:LeftToe_End";
		if (_rigidbodies == null || _transforms == null)
		{
			_rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
			_transforms = GetComponentsInChildren<Transform>().ToList();
		}

		var animStartBone = _transforms.First(x=>x.name == animStartName);
		var animEndBone = _transforms.First(x=>x.name == animEndtName);
		var target = _rigidbodies.First(x=>x.name == name);

		var rotation = Quaternion.Lerp(animStartBone.rotation, animEndBone.rotation, toeRotationOffset);
		var skinOffset = (animEndBone.transform.position - animStartBone.transform.position);
		target.transform.position = animStartBone.transform.position + (skinOffset * toePositionOffset) + offset;
		target.transform.rotation = rotation * rotationOffset;
	}
	void MimicRightFoot(string name, Vector3 offset, Quaternion rotationOffset)
	{
		string animStartName = "mixamorig:RightFoot";
		// string animEndtName = "mixamorig:RightToeBase";
		string animEndtName = "mixamorig:RightToe_End";
		if (_rigidbodies == null || _transforms == null)
		{
			_rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
			_transforms = GetComponentsInChildren<Transform>().ToList();
		}


		var animStartBone = _transforms.First(x=>x.name == animStartName);
		var animEndBone = _transforms.First(x=>x.name == animEndtName);
		var target = _rigidbodies.First(x=>x.name == name);

		var rotation = Quaternion.Lerp(animStartBone.rotation, animEndBone.rotation, toeRotationOffset);
		var skinOffset = (animEndBone.transform.position - animStartBone.transform.position);
		target.transform.position = animStartBone.transform.position + (skinOffset * toePositionOffset) + offset;
		target.transform.rotation = rotation * rotationOffset;

	}



    public void OnSensorCollisionEnter(Collider sensorCollider, GameObject other)
	{
		//if (string.Compare(other.name, "Terrain", true) !=0)
		if (other.GetComponent<Terrain>() == null)
			return;
		var sensor = _sensors
			.FirstOrDefault(x=>x == sensorCollider.gameObject);
		if (sensor != null) {
			var idx = _sensors.IndexOf(sensor);
			SensorIsInTouch[idx] = 1f;
		}
	}
	public void OnSensorCollisionExit(Collider sensorCollider, GameObject other)
	{
		//if (string.Compare(other.gameObject.name, "Terrain", true) !=0)
		if (other.GetComponent<Terrain>() == null)
			return;
		var sensor = _sensors
			.FirstOrDefault(x=>x == sensorCollider.gameObject);
		if (sensor != null) {
			var idx = _sensors.IndexOf(sensor);
			SensorIsInTouch[idx] = 0f;
		}
	}      
}