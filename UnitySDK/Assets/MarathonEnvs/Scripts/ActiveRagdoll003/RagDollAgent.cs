using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class RagDollAgent : Agent 
{
    [Header("Settings")]
	public float FixedDeltaTime = 1f/60f;
    public float SmoothBeta = 0.2f;
    [Header("... debug")]
    public bool SkipRewardSmoothing;
    public bool debugCopyMocap;
    public bool ignorActions;

    MocapController _mocapController;
    List<ArticulationBody> _mocapBodyParts;
    List<ArticulationBody> _bodyParts;
    SpawnableEnv _spawnableEnv;
    DReConObservations _dReConObservations;
    DReConRewards _dReConRewards;
    RagDoll002 _ragDollSettings;
    TrackBodyStatesInWorldSpace _trackBodyStatesInWorldSpace;
    List<ArticulationBody> _motors;
    MarathonTestBedController _debugController;    
    bool _hasLazyInitialized;
    float[] _smoothedActions;
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
        bool shouldDebug = _debugController != null;
        shouldDebug &= _debugController.isActiveAndEnabled;
        shouldDebug &= _debugController.gameObject.activeInHierarchy;
        if (shouldDebug)
        {
            if (_debugController.Actions == null || _debugController.Actions.Length == 0)
            {
                _debugController.Actions = vectorAction.Select(x=>0f).ToArray();
            }
            vectorAction = _debugController.Actions.Select(x=>Mathf.Clamp(x,-1f,1f)).ToArray();
        }
        if (!SkipRewardSmoothing)
            vectorAction = SmoothActions(vectorAction);
        if (ignorActions)
            vectorAction = vectorAction.Select(x=>0f).ToArray();
		int i = 0;
        Vector3 targetNormalizedRotation = Vector3.zero;
		foreach (var m in _motors)
		{
            if (m.swingYLock == ArticulationDofLock.LimitedMotion)
				targetNormalizedRotation.x = vectorAction[i++];
            if (m.swingZLock == ArticulationDofLock.LimitedMotion)
				targetNormalizedRotation.y = vectorAction[i++];
			if (m.twistLock == ArticulationDofLock.LimitedMotion)
				targetNormalizedRotation.z = vectorAction[i++];
            UpdateMotor(m, targetNormalizedRotation);
            
        }
        _dReConObservations.PreviousActions = vectorAction;

        AddReward(_dReConRewards.Reward);
        if (_dReConRewards.Reward <= 0f)
        {
            Done();
        }
    }

    float[] SmoothActions(float[] vectorAction)
    {
        // yt =β at +(1−β)yt−1
        if (_smoothedActions == null)
            _smoothedActions = vectorAction.Select(x=>0f).ToArray();
        _smoothedActions = vectorAction
            .Zip(_smoothedActions, (a, y)=> SmoothBeta * a + (1f-SmoothBeta) * y)
            .ToArray();
        return _smoothedActions;
    }
	public override void AgentReset()
	{
		if (!_hasLazyInitialized)
		{
            _debugController = FindObjectOfType<MarathonTestBedController>();
    		Time.fixedDeltaTime = FixedDeltaTime;
            _spawnableEnv = GetComponentInParent<SpawnableEnv>();
            _mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
            _mocapBodyParts = _mocapController.GetComponentsInChildren<ArticulationBody>().ToList();
            _bodyParts = GetComponentsInChildren<ArticulationBody>().ToList();
            _dReConObservations = GetComponent<DReConObservations>();
            _dReConRewards = GetComponent<DReConRewards>();
            var mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
            _trackBodyStatesInWorldSpace = mocapController.GetComponent<TrackBodyStatesInWorldSpace>();
            _ragDollSettings = GetComponent<RagDoll002>();

            _motors = GetComponentsInChildren<ArticulationBody>()
                .Where(x=>x.jointType == ArticulationJointType.SphericalJoint)
                .Where(x=>!x.isRoot)
                .Distinct()
                .ToList();
            var individualMotors = new List<float>();
            foreach (var m in _motors)
            {
                if (m.swingYLock == ArticulationDofLock.LimitedMotion)
                    individualMotors.Add(0f);
                if (m.swingZLock == ArticulationDofLock.LimitedMotion)
                    individualMotors.Add(0f);
                if (m.twistLock == ArticulationDofLock.LimitedMotion)
                    individualMotors.Add(0f);
            }
            _dReConObservations.PreviousActions = individualMotors.ToArray();
			_hasLazyInitialized = true;
		}
        _smoothedActions = null;
        debugCopyMocap = false;
        // _trackBodyStatesInWorldSpace.CopyStatesTo(this.gameObject);
        _dReConObservations.OnReset();
        _dReConRewards.OnReset();
        _dReConObservations.OnStep();
        _dReConRewards.OnStep();
    }    

    void UpdateMotor(ArticulationBody joint, Vector3 targetNormalizedRotation)
    {
		var drive = joint.yDrive;
        var scale = (drive.upperLimit-drive.lowerLimit) / 2f;
        var midpoint = drive.lowerLimit + scale;
        var target = midpoint + (targetNormalizedRotation.x *scale);
        drive.target = target;
		joint.yDrive = drive;

		drive = joint.zDrive;
        scale = (drive.upperLimit-drive.lowerLimit) / 2f;
        midpoint = drive.lowerLimit + scale;
        target = midpoint + (targetNormalizedRotation.y *scale);
        drive.target = target;
		joint.zDrive = drive;

		drive = joint.xDrive;
        scale = (drive.upperLimit-drive.lowerLimit) / 2f;
        midpoint = drive.lowerLimit + scale;
        target = midpoint + (targetNormalizedRotation.z *scale);
        drive.target = target;
		joint.xDrive = drive;
	}

    void FixedUpdate()
    {
        if (debugCopyMocap)
        {
            Done();
        }
    }
}
