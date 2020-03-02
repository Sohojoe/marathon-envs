using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;
using static BodyHelper002;


public class TerrainMarathonAntAgent : Agent, IOnTerrainCollision
{
	BodyManager002 _bodyManager;

	TerrainGenerator _terrainGenerator;
	SpawnableEnv _spawnableEnv;
	int _stepCountAtLastMeter;
	public int _lastXPosInMeters;
	public int maxXPosInMeters;
	float _pain;
	bool _modeRecover;

	List<float> distances;
	float fraction;
	bool _hasLazyInitialized;

	override public void CollectObservations()
	{
		var sensor = this;
		if (!_hasLazyInitialized)
		{
			AgentReset();
		}

		Vector3 normalizedVelocity = _bodyManager.GetNormalizedVelocity();
		var pelvis = _bodyManager.GetFirstBodyPart(BodyPartGroup.Torso);

		AddVectorObs(normalizedVelocity);
		AddVectorObs(pelvis.Rigidbody.transform.forward); // gyroscope 
		AddVectorObs(pelvis.Rigidbody.transform.up);

		//AddVectorObs(_bodyManager.GetSensorIsInTouch());
		var sensorsInTouch = _bodyManager.GetSensorIsInTouch();
		AddVectorObs(sensorsInTouch);

		// JointRotations.ForEach(x => AddVectorObs(x)); = 6*4 = 24
		(var rotationVector, var rotationVelocityVector) = _bodyManager.GetMusclesRotationAndRotationVelocity();
		rotationVector.ForEach(x => AddVectorObs(x));

		// AddVectorObs(JointVelocity); = 6
		rotationVelocityVector.ForEach(x => AddVectorObs(x));

		// AddVectorObs.  = 2
		var feetHeight = _bodyManager.GetSensorYPositions();
		AddVectorObs(feetHeight);

		(distances, fraction) =
			_terrainGenerator.GetDistances2d(
				pelvis.Rigidbody.transform.position, _bodyManager.ShowMonitor);

		sensor.AddVectorObs(distances);
		sensor.AddVectorObs(fraction);
		// _bodyManager.OnCollectObservationsHandleDebug(GetInfo());
	}

	public override void AgentAction(float[] vectorAction)
	{
		// apply actions to body
		_bodyManager.OnAgentAction(vectorAction);

		// manage reward
		float velocity = Mathf.Clamp(_bodyManager.GetNormalizedVelocity().x, 0f, 1f);
		var reward = velocity;
		AddReward(reward);
		_bodyManager.SetDebugFrameReward(reward);

		var pelvis = _bodyManager.GetFirstBodyPart(BodyPartGroup.Torso);
		float xpos =
			_bodyManager.GetBodyParts(BodyPartGroup.Foot)
			.Average(x => x.Transform.position.x);
		int newXPosInMeters = (int)xpos;
		if (newXPosInMeters > _lastXPosInMeters)
		{
			_lastXPosInMeters = newXPosInMeters;
			_stepCountAtLastMeter = this.GetStepCount();
		}
		if (newXPosInMeters > maxXPosInMeters)
			maxXPosInMeters = newXPosInMeters;
		var terminate = false;
		if (_terrainGenerator.IsPointOffEdge(pelvis.Transform.position))
		{
			terminate = true;
			AddReward(-1f);
		}
		if (this.GetStepCount() - _stepCountAtLastMeter >= (200 * 5))
			terminate = true;
		else if (xpos < 4f && _pain > 1f)
			terminate = true;
		else if (xpos < 2f && _pain > 0f)
			terminate = true;
		else if (_pain > 2f)
			terminate = true;
		if (terminate)
		{
			Done();
		}
		_pain = 0f;
		_modeRecover = false;
	}

	public override void AgentReset()
	{
		if (!_hasLazyInitialized)
		{
			_bodyManager = GetComponent<BodyManager002>();
			_bodyManager.BodyConfig = MarathonAntAgent.BodyConfig;
			_bodyManager.OnInitializeAgent();
			_hasLazyInitialized = true;
		}

		if (_bodyManager == null)
			_bodyManager = GetComponent<BodyManager002>();
		_bodyManager.OnAgentReset();
		if (_terrainGenerator == null)
			_terrainGenerator = GetComponent<TerrainGenerator>();
		if (_spawnableEnv == null)
			_spawnableEnv = GetComponentInParent<SpawnableEnv>();
		_terrainGenerator.Reset();
		_lastXPosInMeters = (int)
			_bodyManager.GetBodyParts(BodyPartGroup.Foot)
			.Average(x => x.Transform.position.x);
		_pain = 0f;
		_modeRecover = false;
	}
	public virtual void OnTerrainCollision(GameObject other, GameObject terrain)
	{
		// if (string.Compare(terrain.name, "Terrain", true) != 0)
		if (terrain.GetComponent<Terrain>() == null)
			return;
		// if (!_styleAnimator.AnimationStepsReady)
		// 	return;
		// HACK - for when agent has not been initialized
		if (_bodyManager == null)
			return;
		var bodyPart = _bodyManager.BodyParts.FirstOrDefault(x => x.Transform.gameObject == other);
		if (bodyPart == null)
			return;
		switch (bodyPart.Group)
		{
			case BodyHelper002.BodyPartGroup.None:
			case BodyHelper002.BodyPartGroup.Foot:
			case BodyHelper002.BodyPartGroup.LegLower:
				break;
			case BodyHelper002.BodyPartGroup.LegUpper:
			case BodyHelper002.BodyPartGroup.Hand:
			case BodyHelper002.BodyPartGroup.ArmLower:
			case BodyHelper002.BodyPartGroup.ArmUpper:
				_pain += .1f;
				_modeRecover = true;
				break;
			default:
				// AddReward(-100f);
				_pain += 5f;
				_modeRecover = true;
				break;
		}
	}
}
