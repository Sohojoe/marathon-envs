using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;

public class StyleTransfer002Agent : Agent, IOnSensorCollision, IOnTerrainCollision {

	public float FrameReward;
	public float AverageReward;
	public float RotationReward;
	public float VelocityReward;
	public float EndEffectorReward;
	public float CenterMassReward;
	public float SensorReward;
	public float JointsNotAtLimitReward;
	public float MaxRotationReward;
	public float MaxVelocityReward;
	public float MaxEndEffectorReward;
	public float MaxCenterMassReward;
	public float MaxSensorReward;
	public float MaxJointsNotAtLimitReward;

	public float RewardTerminateValue;
	public List<float> RewardTerminateValues = new List<float>{.3f};

	public List<float> Rewards;
	public List<float> SensorIsInTouch;
	StyleTransfer002Master _master;
	StyleTransfer002Animator _localStyleAnimator;
	StyleTransfer002Animator _styleAnimator;
	DecisionRequester _decisionRequester;
	// StyleTransfer002TrainerAgent _trainerAgent;

	List<GameObject> _sensors;

	public bool ShowMonitor = false;

	static int _startCount;
	static ScoreHistogramData _scoreHistogramData;
	int _totalAnimFrames;
	bool _ignorScoreForThisFrame;
	bool _isDone;
	bool _hasLazyInitialized;
	bool _callDoneOnNextAction;
	bool _firstStepAfterReset;
	Vector3 _startPosition;
	Quaternion _startRotation;

	// Use this for initialization
	void Start () {
		_master = GetComponent<StyleTransfer002Master>();
		_decisionRequester = GetComponent<DecisionRequester>();
		var spawnableEnv = GetComponentInParent<SpawnableEnv>();
		_localStyleAnimator = spawnableEnv.gameObject.GetComponentInChildren<StyleTransfer002Animator>();
		_styleAnimator = _localStyleAnimator.GetFirstOfThisAnim();
		_startCount++;
	}

	// Update is called once per frame
	void Update () {
	}
	void LateUpdate ()
	{
		if (_styleAnimator == _localStyleAnimator)
			_styleAnimator.MimicAnimation(true);
	}

	override public void CollectObservations()
	{
		var sensor = this;
		if (!_hasLazyInitialized)
		{
			AgentReset();
		}

		sensor.AddVectorObs(_master.ObsPhase);
		foreach (var bodyPart in _master.BodyParts)
		{
			sensor.AddVectorObs(bodyPart.ObsLocalPosition);
			sensor.AddVectorObs(bodyPart.ObsRotation);
			sensor.AddVectorObs(bodyPart.ObsRotationVelocity);
			sensor.AddVectorObs(bodyPart.ObsVelocity);
		}
		foreach (var muscle in _master.Muscles)
		{
			if (muscle.ConfigurableJoint.angularXMotion != ConfigurableJointMotion.Locked)
				sensor.AddVectorObs(muscle.TargetNormalizedRotationX);
			if (muscle.ConfigurableJoint.angularYMotion != ConfigurableJointMotion.Locked)
				sensor.AddVectorObs(muscle.TargetNormalizedRotationY);
			if (muscle.ConfigurableJoint.angularZMotion != ConfigurableJointMotion.Locked)
				sensor.AddVectorObs(muscle.TargetNormalizedRotationZ);
		}
		sensor.AddVectorObs(_master.ObsCenterOfMass);
		sensor.AddVectorObs(_master.ObsVelocity);
		sensor.AddVectorObs(SensorIsInTouch);	
	}

	public override void AgentAction(float[] vectorAction)
	{

#if UNITY_EDITOR		
		if (_master.DebugPauseOnReset && _firstStepAfterReset)
		{
	        UnityEditor.EditorApplication.isPaused = true;
		}
#endif		
		_isDone = false;
		_firstStepAfterReset = false;
		if (_callDoneOnNextAction)
		{
			Done();
			return;
		}
		if (_styleAnimator == _localStyleAnimator)
			_styleAnimator.OnAgentAction();
		_master.OnAgentAction(vectorAction);
        float effort = GetEffort();
        var effortPenality = 0.05f * (float)effort;
		
		var rotationDistanceScale = (float)_master.BodyParts.Count;
		var velocityDistanceScale = 170f; // 3f;
		var endEffectorDistanceScale = 8f;
		var centerOfMassDistancScalee = 1f;
		var sensorDistanceScale = 1f;
		var rotationDistance = _master.RotationDistance;
		var velocityDistance = Mathf.Abs(_master.VelocityDistance);
		var endEffectorDistance = _master.EndEffectorDistance;
		var centerOfMassDistance = _master.CenterOfMassDistance;
		var sensorDistance = _master.SensorDistance;
		rotationDistance = Mathf.Clamp(rotationDistance, 0f, rotationDistanceScale);
		velocityDistance = Mathf.Clamp(velocityDistance, 0f, velocityDistanceScale);
		endEffectorDistance = Mathf.Clamp(endEffectorDistance, 0f, endEffectorDistanceScale);
		centerOfMassDistance = Mathf.Clamp(centerOfMassDistance, 0f, centerOfMassDistancScalee);
		sensorDistance = Mathf.Clamp(sensorDistance, 0f, sensorDistanceScale);

		RotationReward = (rotationDistanceScale - rotationDistance) / rotationDistanceScale;
		VelocityReward = (velocityDistanceScale - velocityDistance) / velocityDistanceScale;
		EndEffectorReward = (endEffectorDistanceScale - endEffectorDistance) / endEffectorDistanceScale;
		CenterMassReward = (centerOfMassDistancScalee - centerOfMassDistance) / centerOfMassDistancScalee;
		SensorReward = (sensorDistanceScale - sensorDistance) / sensorDistanceScale;
		RotationReward = Mathf.Pow(RotationReward, rotationDistanceScale);
		VelocityReward = Mathf.Pow(VelocityReward, 17f);
		EndEffectorReward = Mathf.Pow(EndEffectorReward, endEffectorDistanceScale);
		CenterMassReward = Mathf.Pow(CenterMassReward, 5f);
		SensorReward = Mathf.Pow(SensorReward, sensorDistanceScale);

		// float rotationRewardScale = .45f*.9f;
		// float velocityRewardScale = .2f*.9f;
		// float endEffectorRewardScale = .15f*.9f;
		// float centerMassRewardScale = .1f*.9f;
		// float sensorRewardScale = .1f*.9f;
        // JointsNotAtLimitReward = 1f - JointsAtLimit();
		// var jointsNotAtLimitRewardScale = .09f;

		float rotationRewardScale = .5f;
		float velocityRewardScale = .1f;
		float endEffectorRewardScale = .15f;
		float centerMassRewardScale = .1f;
		float sensorRewardScale = .05f;
        JointsNotAtLimitReward = 1f - JointsAtLimit();
		var jointsNotAtLimitRewardScale = .1f;

		RotationReward = RotationReward * rotationRewardScale;
		VelocityReward = VelocityReward * velocityRewardScale;
		EndEffectorReward = EndEffectorReward * endEffectorRewardScale;
		CenterMassReward = CenterMassReward * centerMassRewardScale;
		SensorReward = SensorReward * sensorRewardScale;
		JointsNotAtLimitReward = JointsNotAtLimitReward * jointsNotAtLimitRewardScale;

		MaxRotationReward = Mathf.Max(MaxRotationReward, RotationReward);
		MaxVelocityReward = Mathf.Max(MaxVelocityReward, VelocityReward);
		MaxEndEffectorReward = Mathf.Max(MaxEndEffectorReward, EndEffectorReward);
		MaxCenterMassReward = Mathf.Max(MaxCenterMassReward, CenterMassReward);
		MaxSensorReward = Mathf.Max(MaxSensorReward, SensorReward);
		MaxJointsNotAtLimitReward = Mathf.Max(MaxJointsNotAtLimitReward, JointsNotAtLimitReward);

		float distanceReward = 
			RotationReward +
            VelocityReward +
            EndEffectorReward +
			CenterMassReward + 
			SensorReward;
		float reward = 
			distanceReward
			+ JointsNotAtLimitReward;

		if (!_master.IgnorRewardUntilObservation)
			AddReward(reward);
		FrameReward = reward;
		var stepCount = GetStepCount();
		if (_decisionRequester?.DecisionPeriod > 1)
			stepCount /= _decisionRequester.DecisionPeriod;
		stepCount = Mathf.Max(stepCount, 1);
		AverageReward = GetCumulativeReward() / (float)stepCount;
		if (distanceReward < RewardTerminateValue && _master.IsInferenceMode == false)
		{
			Done();
			return;
		}
		if (_master.IsDone())
		{
			Done();
		}
	}
	float GetEffort(string[] ignorJoints = null)
	{
		double effort = 0;
		foreach (var muscle in _master.Muscles)
		{
			if(muscle.Parent == null)
				continue;
			var name = muscle.Name;
			if (ignorJoints != null && ignorJoints.Contains(name))
				continue;
			var jointEffort = Mathf.Pow(Mathf.Abs(muscle.TargetNormalizedRotationX),2);
			effort += jointEffort;
			jointEffort = Mathf.Pow(Mathf.Abs(muscle.TargetNormalizedRotationY),2);
			effort += jointEffort;
			jointEffort = Mathf.Pow(Mathf.Abs(muscle.TargetNormalizedRotationZ),2);
			effort += jointEffort;
		}
		return (float)effort;
	}	
	float JointsAtLimit(string[] ignorJoints = null)
	{
		int atLimitCount = 0;
		int totalJoints = 0;
		foreach (var muscle in _master.Muscles)
		{
			if(muscle.Parent == null)
				continue;

			var name = muscle.Name;
			if (ignorJoints != null && ignorJoints.Contains(name))
				continue;
			if (Mathf.Abs(muscle.TargetNormalizedRotationX) >= 1f)
				atLimitCount++;
			if (Mathf.Abs(muscle.TargetNormalizedRotationY) >= 1f)
				atLimitCount++;
			if (Mathf.Abs(muscle.TargetNormalizedRotationZ) >= 1f)
				atLimitCount++;
			totalJoints++;
		}
		float fractionOfJointsAtLimit = (float)atLimitCount / (float)totalJoints;
		return fractionOfJointsAtLimit;
	}
	public void SetTotalAnimFrames(int totalAnimFrames)
	{
		_totalAnimFrames = totalAnimFrames;
		if (_scoreHistogramData == null) {
			var columns = _totalAnimFrames;
			if (_decisionRequester?.DecisionPeriod > 1)
				columns /= _decisionRequester.DecisionPeriod;
			_scoreHistogramData = new ScoreHistogramData(columns, 30);
		}
			Rewards = _scoreHistogramData.GetAverages().Select(x=>(float)x).ToList();
	}

	public override void AgentReset()
	{
		if (!_hasLazyInitialized)
		{
			_master = GetComponent<StyleTransfer002Master>();
			_master.BodyConfig = MarathonManAgent.BodyConfig;
			_master.OnInitializeAgent();
			_decisionRequester = GetComponent<DecisionRequester>();
			var spawnableEnv = GetComponentInParent<SpawnableEnv>();
			_localStyleAnimator = spawnableEnv.gameObject.GetComponentInChildren<StyleTransfer002Animator>();
			_styleAnimator = _localStyleAnimator.GetFirstOfThisAnim();
			_styleAnimator.BodyConfig = MarathonManAgent.BodyConfig;
			_styleAnimator.OnInitializeAgent();
			_hasLazyInitialized = true;
			_localStyleAnimator.DestoryIfNotFirstAnim();
			_startPosition = this.transform.position;
			_startRotation = this.transform.rotation;
		}
		int idx = UnityEngine.Random.Range(0, RewardTerminateValues.Count);
		RewardTerminateValue = RewardTerminateValues[idx];
		_isDone = true;
		this.transform.position = _startPosition;
		this.transform.rotation = _startRotation;
		var rb = GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.angularVelocity = Vector3.zero;
			rb.velocity = Vector3.zero;
		}		
		_master.ResetPhase();
		_sensors = GetComponentsInChildren<SensorBehavior>()
			.Select(x=>x.gameObject)
			.ToList();
		SensorIsInTouch = Enumerable.Range(0,_sensors.Count).Select(x=>0f).ToList();
		if (_scoreHistogramData != null) {
			var column = _master.StartAnimationIndex;
			if (_decisionRequester?.DecisionPeriod > 1)
				column /= _decisionRequester.DecisionPeriod;
			_scoreHistogramData.SetItem(column, AverageReward);
        }
		_callDoneOnNextAction = false;
		_firstStepAfterReset = true;
	}
	public virtual void OnTerrainCollision(GameObject other, GameObject terrain)
	{
		if (terrain.GetComponent<Terrain>() == null)
    		return;
		if (!_styleAnimator.AnimationStepsReady)
			return;
		var bodyPart = _master.BodyParts.FirstOrDefault(x=>x.Transform.gameObject == other);
		if (bodyPart == null)
			return;
		switch (bodyPart.Group)
		{
			case BodyHelper002.BodyPartGroup.None:
			case BodyHelper002.BodyPartGroup.Foot:
			case BodyHelper002.BodyPartGroup.LegUpper:
			case BodyHelper002.BodyPartGroup.LegLower:
			case BodyHelper002.BodyPartGroup.Hand:
			case BodyHelper002.BodyPartGroup.ArmLower:
			case BodyHelper002.BodyPartGroup.ArmUpper:
				break;
			default:
                // re-enable for early exit on body collisions
                _callDoneOnNextAction=true;
                break;
		}
	}


	public void OnSensorCollisionEnter(Collider sensorCollider, GameObject other) {
			if (other.GetComponent<Terrain>() == null)
				return;
            var sensor = _sensors
                ?.FirstOrDefault(x=>x == sensorCollider.gameObject);
            if (sensor != null) {
                var idx = _sensors.IndexOf(sensor);
                SensorIsInTouch[idx] = 1f;
            }
		}
        public void OnSensorCollisionExit(Collider sensorCollider, GameObject other)
        {
    		if (other.GetComponent<Terrain>() == null)
                return;
            var sensor = _sensors
                ?.FirstOrDefault(x=>x == sensorCollider.gameObject);
            if (sensor != null) {
                var idx = _sensors.IndexOf(sensor);
                SensorIsInTouch[idx] = 0f;
            }
        }  

}
