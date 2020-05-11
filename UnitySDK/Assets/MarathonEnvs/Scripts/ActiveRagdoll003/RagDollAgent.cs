using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class RagDollAgent : Agent 
{
    [Header("... debug")]
    public bool debugCopyMocap;

    MocapController _mocapController;
    List<Rigidbody> _mocapBodyParts;
    List<Rigidbody> _bodyParts;
    SpawnableEnv _spawnableEnv;
    DReConObservations _dReConObservations;
    DReConRewards _dReConRewards;
    RagDoll002 _ragDollSettings;
    TrackBodyStatesInWorldSpace _trackBodyStatesInWorldSpace;
    List<ConfigurableJoint> _motors;
    bool _hasLazyInitialized;
	override public void CollectObservations()
    {
		var sensor = this;
		if (!_hasLazyInitialized)
		{
			AgentReset();
		}
        // hadle mocap going out of bounds
        if(!_spawnableEnv.IsPointWithinBoundsInWorldSpace(_mocapController.transform.position))
        {
            _mocapController.transform.position = _spawnableEnv.transform.position;
            _trackBodyStatesInWorldSpace.Reset();
            Done();
        }

        _dReConObservations.OnStep();
        _dReConRewards.OnStep();        

        sensor.AddVectorObs(_dReConObservations.MocapCOMVelocity);
        sensor.AddVectorObs(_dReConObservations.RagDollCOMVelocity);
        sensor.AddVectorObs(_dReConObservations.RagDollCOMVelocity-_dReConObservations.MocapCOMVelocity);
        sensor.AddVectorObs(_dReConObservations.InputDesiredHorizontalVelocity);
        sensor.AddVectorObs(_dReConObservations.InputJump);
        sensor.AddVectorObs(_dReConObservations.InputBackflip);
        sensor.AddVectorObs(_dReConObservations.HorizontalVelocityDifference);
        // foreach (var stat in _dReConObservations.MocapBodyStats)
        // {
        //     sensor.AddVectorObs(stat.Position);
        //     sensor.AddVectorObs(stat.Velocity);
        // }
        foreach (var stat in _dReConObservations.RagDollBodyStats)
        {
            sensor.AddVectorObs(stat.Position);
            sensor.AddVectorObs(stat.Velocity);
        }                
        foreach (var stat in _dReConObservations.BodyPartDifferenceStats)
        {
            sensor.AddVectorObs(stat.Position);
            sensor.AddVectorObs(stat.Velocity);
        }
        sensor.AddVectorObs(_dReConObservations.PreviousActions);
    }
	public override void AgentAction(float[] vectorAction)
    {
		int i = 0;
        Vector3 targetNormalizedRotation = Vector3.zero;
		foreach (var m in _motors)
		{
            Vector3 maximumForce = _ragDollSettings.MusclePowers
                .First(x=>x.Muscle == m.name)
                .PowerVector;
            maximumForce *= _ragDollSettings.MotorScale;

			if (m.angularXMotion != ConfigurableJointMotion.Locked)
				targetNormalizedRotation.x = vectorAction[i++];
			if (m.angularYMotion != ConfigurableJointMotion.Locked)
				targetNormalizedRotation.y = vectorAction[i++];
			if (m.angularZMotion != ConfigurableJointMotion.Locked)
				targetNormalizedRotation.z = vectorAction[i++];
            UpdateMotor(m, targetNormalizedRotation, maximumForce);
            
        }
        _dReConObservations.PreviousActions = vectorAction;

        AddReward(_dReConRewards.Reward);
        if (_dReConRewards.Reward <= 0f)
        {
            Done();
        }
    }
	public override void AgentReset()
	{
		if (!_hasLazyInitialized)
		{
            _spawnableEnv = GetComponentInParent<SpawnableEnv>();
            _mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
            _mocapBodyParts = _mocapController.GetComponentsInChildren<Rigidbody>().ToList();
            _bodyParts = GetComponentsInChildren<Rigidbody>().ToList();
            _dReConObservations = GetComponent<DReConObservations>();
            _dReConRewards = GetComponent<DReConRewards>();
            var mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
            _trackBodyStatesInWorldSpace = mocapController.GetComponent<TrackBodyStatesInWorldSpace>();
            _ragDollSettings = GetComponent<RagDoll002>();

            _motors = GetComponentsInChildren<ConfigurableJoint>().ToList();
            var individualMotors = new List<float>();
            foreach (var m in _motors)
            {
                if (m.angularXMotion != ConfigurableJointMotion.Locked)
                    individualMotors.Add(0f);
                if (m.angularYMotion != ConfigurableJointMotion.Locked)
                    individualMotors.Add(0f);
                if (m.angularZMotion != ConfigurableJointMotion.Locked)
                    individualMotors.Add(0f);
            }
            _dReConObservations.PreviousActions = individualMotors.ToArray();
			_hasLazyInitialized = true;
		}
        debugCopyMocap = false;
        _trackBodyStatesInWorldSpace.CopyStatesTo(this.gameObject);
        _dReConObservations.OnReset();
        _dReConRewards.OnReset();
        _dReConObservations.OnStep();
        _dReConRewards.OnStep();
    }    

    void UpdateMotor(ConfigurableJoint configurableJoint, Vector3 targetNormalizedRotation, Vector3 maximumForce)
    {
        float powerMultiplier = 2.5f;
		var t = configurableJoint.targetAngularVelocity;
		t.x = targetNormalizedRotation.x * maximumForce.x;
		t.y = targetNormalizedRotation.y * maximumForce.y;
		t.z = targetNormalizedRotation.z * maximumForce.z;
		configurableJoint.targetAngularVelocity = t;

		var angX = configurableJoint.angularXDrive;
		angX.positionSpring = 1f;
		var scale = maximumForce.x * Mathf.Pow(Mathf.Abs(targetNormalizedRotation.x), 3);
		angX.positionDamper = Mathf.Max(1f, scale);
		angX.maximumForce = Mathf.Max(1f, maximumForce.x * powerMultiplier);
		configurableJoint.angularXDrive = angX;

        var maxForce = Mathf.Max(maximumForce.y, maximumForce.z);
		var angYZ = configurableJoint.angularYZDrive;
		angYZ.positionSpring = 1f;
        var maxAbsRotXY = Mathf.Max(Mathf.Abs(targetNormalizedRotation.y) + Mathf.Abs(targetNormalizedRotation.z));
		scale = maxForce * Mathf.Pow(maxAbsRotXY, 3);
		angYZ.positionDamper = Mathf.Max(1f, scale);
		angYZ.maximumForce = Mathf.Max(1f, maxForce * powerMultiplier);
		configurableJoint.angularYZDrive = angYZ;
	}

    void FixedUpdate()
    {
        if (debugCopyMocap)
        {
            Done();
        }
    }
}
