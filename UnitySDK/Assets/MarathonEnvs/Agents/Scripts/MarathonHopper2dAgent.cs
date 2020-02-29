using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;
using static BodyHelper002;

public class MarathonHopper2dAgent : Agent, IOnTerrainCollision
{
	BodyManager002 _bodyManager;

	bool _isDone;
	bool _hasLazyInitialized;

	override public void CollectObservations()
	{
		if (!_hasLazyInitialized)
		{
			AgentReset();
		}

		Vector3 normalizedVelocity = _bodyManager.GetNormalizedVelocity();
		var pelvis = _bodyManager.GetFirstBodyPart(BodyPartGroup.Torso);
		var shoulders = _bodyManager.GetFirstBodyPart(BodyPartGroup.Torso);

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
	}

	public override void AgentAction(float[] vectorAction)
	{
		_isDone = false;
		// apply actions to body
		_bodyManager.OnAgentAction(vectorAction);

		// manage reward
		float velocity = _bodyManager.GetNormalizedVelocity().x;
		velocity = Mathf.Clamp(velocity, -1f, 1f);
		var reward = velocity;
		AddReward(reward);
		_bodyManager.SetDebugFrameReward(reward);
	}


	public override void AgentReset()
	{
		if (!_hasLazyInitialized)
		{
			_bodyManager = GetComponent<BodyManager002>();
			_bodyManager.BodyConfig = MarathonHopper2dAgent.BodyConfig;
			_bodyManager.OnInitializeAgent();
			_hasLazyInitialized = true;
		}
		_isDone = true;
		_bodyManager.OnAgentReset();
	}
	public virtual void OnTerrainCollision(GameObject other, GameObject terrain)
	{
		if (terrain.GetComponent<Terrain>() == null)
			return;
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
			case BodyHelper002.BodyPartGroup.Hand:
				break;
			default:
				if (!_isDone)
				{
					Done();
				}
				break;
		}
	}

	public static BodyConfig BodyConfig = new BodyConfig
	{
		GetBodyPartGroup = (name) =>
		{
			name = name.ToLower();
			if (name.Contains("mixamorig"))
				return BodyPartGroup.None;

			if (name.Contains("torso"))
				return BodyPartGroup.Torso;
			if (name.Contains("pelvis"))
				return BodyPartGroup.Spine;
			if (name.Contains("thigh"))
				return BodyPartGroup.LegUpper;
			if (name.Contains("calf"))
				return BodyPartGroup.LegLower;
			if (name.Contains("foot"))
				return BodyPartGroup.Foot;

			return BodyPartGroup.None;
		},
		GetMuscleGroup = (name) =>
		{
			name = name.ToLower();
			if (name.Contains("mixamorig"))
				return MuscleGroup.None;

			if (name.Contains("pelvis"))
				return MuscleGroup.Spine;
			if (name.Contains("thigh"))
				return MuscleGroup.LegUpper;
			if (name.Contains("calf"))
				return MuscleGroup.LegLower;
			if (name.Contains("foot"))
				return MuscleGroup.Foot;

			return MuscleGroup.None;
		},
		GetRootBodyPart = () => BodyPartGroup.Torso,
	};

}
