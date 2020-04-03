using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAgents;
using System;

public class StyleTransfer002Master : MonoBehaviour {
	public float FixedDeltaTime = 0.005f;
	public bool visualizeAnimator = true;

	// general observations
	public List<Muscle002> Muscles;
	public List<BodyPart002> BodyParts;
	public float ObsPhase;
	public Vector3 ObsCenterOfMass;
	public Vector3 ObsVelocity;

	// model observations
	// i.e. model = difference between mocap and actual)
	// ideally we dont want to generate model at inference
	// public float PositionDistance;
	public float EndEffectorDistance; // feet, hands, head
	public float FeetRotationDistance; 
	public float EndEffectorVelocityDistance; // feet, hands, head
	public float RotationDistance;
	public float VelocityDistance;
	public float CenterOfMassDistance;
	public float SensorDistance;

	public float MaxEndEffectorDistance; // feet, hands, head
	public float MaxFeetRotationDistance; 
	public float MaxEndEffectorVelocityDistance; // feet, hands, head
	public float MaxRotationDistance;
	public float MaxVelocityDistance;
	public float MaxCenterOfMassDistance;
	public float MaxSensorDistance;

	


	// debug variables
	public bool IgnorRewardUntilObservation;
	public bool DebugDisableMotor;

	public bool DebugPauseOnReset;
	public bool DebugPauseOnStep;

	public float TimeStep;
	public int AnimationIndex;
	public int EpisodeAnimationIndex;
	public int StartAnimationIndex;
	public bool UseRandomIndexForTraining;
	public bool UseRandomIndexForInference;
	public bool CameraFollowMe;
	public Transform CameraTarget;

	private bool _isDone;
	bool _resetCenterOfMassOnLastUpdate;
	bool _fakeVelocity;
	bool _waitingForAnimation;

	private StyleTransfer002Agent _agent;
	StyleTransfer002Animator _styleAnimator;
	DecisionRequester _decisionRequester;
	SpawnableEnv _spawnableEnv;
	public bool IsInferenceMode;
	bool _phaseIsRunning;
    UnityEngine.Random _random = new UnityEngine.Random();
	Vector3 _lastCenterOfMass;

	public BodyConfig BodyConfig;

	// Use this for initialization
	void Awake () {
		foreach (var rb in GetComponentsInChildren<Rigidbody>())
		{
			if (rb.useGravity == false)
				rb.solverVelocityIterations = 255;
		}
		var masters = FindObjectsOfType<StyleTransfer002Master>().ToList();
		if (masters.Count(x=>x.CameraFollowMe) < 1)
			CameraFollowMe = true;
	}

	public void OnInitializeAgent()
    {
		Time.fixedDeltaTime = FixedDeltaTime;
		_waitingForAnimation = true;
		_decisionRequester = GetComponent<DecisionRequester>();
		_spawnableEnv = GetComponentInParent<SpawnableEnv>();
		_styleAnimator = _spawnableEnv.gameObject.GetComponentInChildren<StyleTransfer002Animator>();
		_agent = GetComponent<StyleTransfer002Agent>();
		var animatorTransforms = _styleAnimator.GetComponentsInChildren<Transform>();

		BodyParts = new List<BodyPart002> ();
		BodyPart002 root = null;
		foreach (var t in GetComponentsInChildren<Transform>())
		{
			if (BodyConfig.GetBodyPartGroup(t.name) == BodyHelper002.BodyPartGroup.None)
				continue;
			
			Transform kinematicTransform = animatorTransforms.First(x=>x.name==t.name);
			Rigidbody kinematicRigidbody = kinematicTransform.GetComponent<Rigidbody>();
			var bodyPart = new BodyPart002{
				Rigidbody = t.GetComponent<Rigidbody>(),
				Transform = t,
				Name = t.name,
				Group = BodyConfig.GetBodyPartGroup(t.name),
				KinematicTransform = kinematicTransform,
				KinematicRigidbody = kinematicRigidbody
			};
			if (bodyPart.Group == BodyConfig.GetRootBodyPart())
				root = bodyPart;
			bodyPart.Root = root;
			bodyPart.Init();
			BodyParts.Add(bodyPart);
		}
		var partCount = BodyParts.Count;

		Muscles = new List<Muscle002> ();
		var muscles = GetComponentsInChildren<ConfigurableJoint>();
		var ragDoll = GetComponent<RagDoll002>();
		foreach (var m in muscles)
		{
			var maximumForce = ragDoll.MusclePowers.First(x=>x.Muscle == m.name).PowerVector;
			// maximumForce *= 2f;
			var muscle = new Muscle002{
				Rigidbody = m.GetComponent<Rigidbody>(),
				Transform = m.GetComponent<Transform>(),
				ConfigurableJoint = m,
				Name = m.name,
				Group = BodyConfig.GetMuscleGroup(m.name),
				MaximumForce = maximumForce
			};
			muscle.RootTransform = root.Transform;
			muscle.Init();

			Muscles.Add(muscle);			
		}
		IsInferenceMode = !Academy.Instance.IsCommunicatorOn;
	}
	
	// Update is called once per frame
	void Update () {
	}
	static float SumAbs(Vector3 vector)
	{
		var sum = Mathf.Abs(vector.x);
		sum += Mathf.Abs(vector.y);
		sum += Mathf.Abs(vector.z);
		return sum;
	}
	static float SumAbs(Quaternion q)
	{
		var sum = Mathf.Abs(q.w);
		sum += Mathf.Abs(q.x);
		sum += Mathf.Abs(q.y);
		sum += Mathf.Abs(q.z);
		return sum;
	}

	public void OnAgentAction(float[] vectorAction)
	{
		if (DebugDisableMotor)
			vectorAction = vectorAction.Select(x=>0f).ToArray();
		if (_waitingForAnimation && _styleAnimator.AnimationStepsReady){
			_waitingForAnimation = false;
			ResetPhase();
		}
		int i = 0;
		foreach (var muscle in Muscles)
		{
			if (muscle.ConfigurableJoint.angularXMotion != ConfigurableJointMotion.Locked)
				muscle.TargetNormalizedRotationX = vectorAction[i++];
			if (muscle.ConfigurableJoint.angularYMotion != ConfigurableJointMotion.Locked)
				muscle.TargetNormalizedRotationY = vectorAction[i++];
			if (muscle.ConfigurableJoint.angularZMotion != ConfigurableJointMotion.Locked)
				muscle.TargetNormalizedRotationZ = vectorAction[i++];
			muscle.UpdateMotor();
		}
		var animStep = UpdateObservations();
		IncrementStep();
		SetAnimStep(animStep);
#if UNITY_EDITOR
		if (DebugPauseOnStep)
	        UnityEditor.EditorApplication.isPaused = true;
#endif
	}
	StyleTransfer002Animator.AnimationStep UpdateObservations()
	{
		StyleTransfer002Animator.AnimationStep animStep = null;
		if (_phaseIsRunning) {
			animStep = _styleAnimator.AnimationSteps[AnimationIndex];
		}
		EndEffectorDistance = 0f;
		FeetRotationDistance = 0f;
		EndEffectorVelocityDistance = 0;
		RotationDistance = 0f;
		VelocityDistance = 0f;
		CenterOfMassDistance = 0f;
		SensorDistance = 0f;
		if (_phaseIsRunning)
			CompareAnimationFrame(animStep);
		foreach (var muscle in Muscles)
		{
			var i = Muscles.IndexOf(muscle);
			muscle.UpdateObservations();
		}
		foreach (var bodyPart in BodyParts)
		{
			if (_phaseIsRunning){
				bodyPart.UpdateObservations();
				
				// var rotDistance = bodyPart.ObsAngleDeltaFromAnimationRotation;
				var rotDistance = bodyPart.ObsAngleDeltaFromKinematicRotation;
				var squareRotDistance = Mathf.Pow(rotDistance,2);
				RotationDistance += squareRotDistance;
				if (bodyPart.Group == BodyHelper002.BodyPartGroup.Hand
					|| bodyPart.Group == BodyHelper002.BodyPartGroup.Torso
					|| bodyPart.Group == BodyHelper002.BodyPartGroup.Foot)
				{
					// EndEffectorDistance += bodyPart.ObsDeltaFromAnimationPosition.sqrMagnitude;
					EndEffectorDistance += bodyPart.ObsDeltaFromKinematicPosition.sqrMagnitude;
				}
				if (bodyPart.Group == BodyHelper002.BodyPartGroup.Foot)
				{
					FeetRotationDistance += squareRotDistance;
				}
			}
		}

		// RotationDistance *= RotationDistance; // take the square;
		ObsCenterOfMass = GetCenterOfMass();
		if (_phaseIsRunning)
			CenterOfMassDistance = (animStep.CenterOfMass - ObsCenterOfMass).sqrMagnitude;
		ObsVelocity = ObsCenterOfMass-_lastCenterOfMass;
		if (_fakeVelocity)
			ObsVelocity = animStep.Velocity;
		_lastCenterOfMass = ObsCenterOfMass;
		if (!_resetCenterOfMassOnLastUpdate)
			_fakeVelocity = false;

		if (_phaseIsRunning){
			var animVelocity = animStep.Velocity / Time.fixedDeltaTime;
			ObsVelocity /= Time.fixedDeltaTime;
			var velocityDistance = ObsVelocity-animVelocity;
			VelocityDistance = velocityDistance.sqrMagnitude;
			var sensorDistance = 0.0;
			var sensorDistanceStep = 1.0 / _agent.SensorIsInTouch.Count;
			for (int i = 0; i < _agent.SensorIsInTouch.Count; i++)
			{
				if (animStep.SensorIsInTouch[i] != _agent.SensorIsInTouch[i])
					sensorDistance += sensorDistanceStep;
			}
			SensorDistance = (float) sensorDistance;
		}

		if (!IgnorRewardUntilObservation){
			MaxEndEffectorDistance = Mathf.Max(MaxEndEffectorDistance, EndEffectorDistance);
			MaxFeetRotationDistance = Mathf.Max(MaxFeetRotationDistance, FeetRotationDistance);
			MaxEndEffectorVelocityDistance = Mathf.Max(MaxEndEffectorVelocityDistance, EndEffectorVelocityDistance);
			MaxRotationDistance = Mathf.Max(MaxRotationDistance, RotationDistance);
			MaxVelocityDistance = Mathf.Max(MaxVelocityDistance, VelocityDistance);
			MaxCenterOfMassDistance = Mathf.Max(MaxCenterOfMassDistance, CenterOfMassDistance);
			MaxSensorDistance = Mathf.Max(MaxSensorDistance, SensorDistance);
		}

		if (IgnorRewardUntilObservation)
			IgnorRewardUntilObservation = false;
		ObsPhase = _styleAnimator.AnimationSteps[AnimationIndex].NormalizedTime % 1f;
		return animStep;
	}
	void IncrementStep()
	{
		if (_phaseIsRunning){
			AnimationIndex++;
			if (AnimationIndex>=_styleAnimator.AnimationSteps.Count-1) {
				Done();
			}
		}
	}
	void SetAnimStep(StyleTransfer002Animator.AnimationStep animStep)
	{
		if (_phaseIsRunning && IsInferenceMode)
		{
			_styleAnimator.anim.enabled = true;
			_styleAnimator.anim.Play("Record",0, animStep.NormalizedTime);
			_styleAnimator.anim.transform.position = animStep.TransformPosition;
			_styleAnimator.anim.transform.rotation = animStep.TransformRotation;
		}
	}
	void CompareAnimationFrame(StyleTransfer002Animator.AnimationStep animStep)
	{
		MimicAnimationFrame(animStep, true);
	}

	void MimicAnimationFrame(StyleTransfer002Animator.AnimationStep animStep, bool onlySetAnimation = false)
	{
		if (!onlySetAnimation)
		{
			foreach (var rb in GetComponentsInChildren<Rigidbody>())
			{
				rb.angularVelocity = Vector3.zero;
				rb.velocity = Vector3.zero;
			}
		}
		var deltaTime = Time.fixedDeltaTime;
		if (_decisionRequester?.DecisionPeriod > 1)
			deltaTime *= this._decisionRequester.DecisionPeriod;
		foreach (var bodyPart in BodyParts)
		{
			var i = animStep.Names.IndexOf(bodyPart.Name);
			Vector3 animPosition = bodyPart.InitialRootPosition + animStep.Positions[0];
            Quaternion animRotation = bodyPart.InitialRootRotation * animStep.Rotaions[0];
			if (i != 0) {
				animPosition += animStep.Positions[i];
				animRotation = bodyPart.InitialRootRotation * animStep.Rotaions[i];
			}
			Vector3 angularVelocity = animStep.AngularVelocities[i] / Time.fixedDeltaTime;
			Vector3 velocity = animStep.Velocities[i] / deltaTime;
			// angularVelocity = Vector3.zero;
			// velocity = Vector3.zero;
			bool setAnim = !onlySetAnimation;
			if (bodyPart.Name.Contains("head") || bodyPart.Name.Contains("upper_waist"))
				setAnim = false;
			if (setAnim)
				bodyPart.MoveToAnim(animPosition, animRotation, angularVelocity, velocity);
			bodyPart.SetAnimationPosition(animStep.Positions[i], animStep.Rotaions[i]);
		}
	}

	protected virtual void LateUpdate() {
		if (_resetCenterOfMassOnLastUpdate){
			ObsCenterOfMass = GetCenterOfMass();
			_lastCenterOfMass = ObsCenterOfMass;
			_resetCenterOfMassOnLastUpdate = false;
		}
		#if UNITY_EDITOR
			VisualizeTargetPose();
		#endif
	}

	public bool IsDone()
	{
		return _isDone;
	}
	void Done()
	{
		_isDone = true;
	}

	public void ResetPhase()
	{
		if (_waitingForAnimation)
			return;
		_decisionRequester.enabled = true;
		// _trainerAgent.SetBrainParams(_muscleAnimator.AnimationSteps.Count);
		_agent.SetTotalAnimFrames(_styleAnimator.AnimationSteps.Count);
		// _trainerAgent.RequestDecision(_agent.AverageReward);
		SetStartIndex();
		var animStep = UpdateObservations();
	}

	public void SetStartIndex()
	{
		_decisionRequester.enabled = false;

		// _animationIndex =  UnityEngine.Random.Range(0, _muscleAnimator.AnimationSteps.Count);
		if (!_phaseIsRunning){
			if (CameraFollowMe){
				var camera = FindObjectOfType<Camera>();
				var follow = camera.GetComponent<SmoothFollow>();
				follow.target = CameraTarget;
			}
		}

		StartAnimationIndex = Mathf.Clamp(StartAnimationIndex, 0, _styleAnimator.AnimationSteps.Count-1);

		// start with random
		AnimationIndex = UnityEngine.Random.Range(0, _styleAnimator.AnimationSteps.Count);
		if (IsInferenceMode && !UseRandomIndexForInference){
			AnimationIndex = StartAnimationIndex;
		} else if (!IsInferenceMode && !UseRandomIndexForTraining){
			AnimationIndex = StartAnimationIndex;
		}
		StartAnimationIndex = AnimationIndex;
		EpisodeAnimationIndex = AnimationIndex;
		_phaseIsRunning = true;
		_isDone = false;
		var animStep = _styleAnimator.AnimationSteps[AnimationIndex];
		TimeStep = animStep.TimeStep;
		EndEffectorDistance = 0f;
		FeetRotationDistance = 0f;
		EndEffectorVelocityDistance = 0f;
		RotationDistance = 0f;
		VelocityDistance = 0f;
		IgnorRewardUntilObservation = true;
		_resetCenterOfMassOnLastUpdate = true;
		_fakeVelocity = true;
		foreach (var muscle in Muscles)
			muscle.Init();
		foreach (var bodyPart in BodyParts)
			bodyPart.Init();
		MimicAnimationFrame(animStep);
		EpisodeAnimationIndex = AnimationIndex;
		SetAnimStep(animStep);
	}

	Vector3 GetCenterOfMass()
	{
		var centerOfMass = Vector3.zero;
		float totalMass = 0f;
		var bodies = BodyParts
			.Select(x=>x.Rigidbody)
			.Where(x=>x!=null)
			.ToList();
		foreach (Rigidbody rb in bodies)
		{
			centerOfMass += rb.worldCenterOfMass * rb.mass;
			totalMass += rb.mass;
		}
		centerOfMass /= totalMass;
		centerOfMass -= _spawnableEnv.transform.position;
		return centerOfMass;
	}

	float NextGaussian(float mu = 0, float sigma = 1)
	{
		var u1 = UnityEngine.Random.value;
		var u2 = UnityEngine.Random.value;

		var rand_std_normal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *
							Mathf.Sin(2.0f * Mathf.PI * u2);

		var rand_normal = mu + sigma * rand_std_normal;

		return rand_normal;
	}

	private void VisualizeTargetPose() {
		if (!visualizeAnimator) return;
		if (!Application.isEditor) return;

		// foreach (Muscle002 m in Muscles) {
		// 	if (m.ConfigurableJoint.connectedBody != null && m.connectedBodyTarget != null) {
		// 		Debug.DrawLine(m.target.position, m.connectedBodyTarget.position, Color.cyan);
				
		// 		bool isEndMuscle = true;
		// 		foreach (Muscle002 m2 in Muscles) {
		// 			if (m != m2 && m2.ConfigurableJoint.connectedBody == m.rigidbody) {
		// 				isEndMuscle = false;
		// 				break;
		// 			}
		// 		}
				
		// 		if (isEndMuscle) VisualizeHierarchy(m.target, Color.cyan);
		// 	}
		// }
	}
	
	// Recursively visualizes a bone hierarchy
	private void VisualizeHierarchy(Transform t, Color color) {
		for (int i = 0; i < t.childCount; i++) {
			Debug.DrawLine(t.position, t.GetChild(i).position, color);
			VisualizeHierarchy(t.GetChild(i), color);
		}
	}


}
