using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;
using static BodyHelper002;

public class MarathonHopper2dAgent : Agent, IOnTerrainCollision
{
	BodyManager002 _bodyManager;

	public bool MoveRight = true;
	public bool MoveLeft;
	public bool Jump;
	// public int StepsUntilChange;

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

		AddVectorObs(MoveLeft);
		AddVectorObs(MoveRight);
		AddVectorObs(Jump);

		// _bodyManager.OnCollectObservationsHandleDebug(GetInfo());
	}

	public override void AgentAction(float[] vectorAction)
	{
		_isDone = false;
		// apply actions to body
		_bodyManager.OnAgentAction(vectorAction);

		// manage reward
		float velocity = _bodyManager.GetNormalizedVelocity().x;
		velocity = Mathf.Clamp(velocity, -1f, 1f);
		var actionDifference = _bodyManager.GetActionDifference();
		var actionsAbsolute = vectorAction.Select(x => Mathf.Abs(x)).ToList();
		var actionsAtLimit = actionsAbsolute.Select(x => x >= 1f ? 1f : 0f).ToList();
		float actionaAtLimitCount = actionsAtLimit.Sum();
		float notAtLimitBonus = 1f - (actionaAtLimitCount / (float)actionsAbsolute.Count);
		float reducedPowerBonus = 1f - actionsAbsolute.Average();

		var pelvis = _bodyManager.GetFirstBodyPart(BodyPartGroup.Torso);
		if (pelvis.Transform.position.y < 0)
		{
			Done();
		}

		bool goalStationary = false;
		bool goalRight = false;
		float reward = 0f;
		if (MoveRight && MoveLeft)
			goalStationary = true;
		else if (!MoveRight && !MoveLeft)
			goalStationary = true;
		else if (MoveRight)
			goalRight = true;

		var sensorsInTouch = _bodyManager.GetSensorIsInTouch();
		var anySensorInTouch = sensorsInTouch.Sum() != 0;
		var feetHeights = _bodyManager.GetSensorYPositions();
		var footHeight = feetHeights.Min();
		var jumpReward = 0f;
		if (!anySensorInTouch)
		{
			jumpReward += footHeight;
		}

		float stationaryVelocityReward = 1f - Mathf.Abs(velocity * 3);
		stationaryVelocityReward = Mathf.Clamp(stationaryVelocityReward, 0f, 1f);
		if (goalStationary)
		{
			float footReward = sensorsInTouch.Average();
			// float actionDifferenceReward = 1f-actionDifference;
			reward =
				footReward * .2f +
				reducedPowerBonus * .3f +
				stationaryVelocityReward * .5f;
		}
		else if (goalRight)
			reward = velocity;
		else
			reward = -velocity;
		if (Jump && goalStationary)
		{
			reward =
				jumpReward * .5f +
				reducedPowerBonus * .2f +
				stationaryVelocityReward * .3f;
		}
		else if (Jump)
		{
			reward = reward * .5f;
			reward += (jumpReward * .5f);
		}
		reward = Mathf.Clamp(reward, -1f, 1f);

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
		//StepsUntilChange = 0;
		//SetAction(0);
	}
	public virtual void OnTerrainCollision(GameObject other, GameObject terrain)
	{
		// if (string.Compare(terrain.name, "Terrain", true) != 0)
		if (terrain.GetComponent<Terrain>() == null)
			return;
		// if (!_styleAnimator.AnimationStepsReady)
		// 	return;
		var bodyPart = _bodyManager.BodyParts.FirstOrDefault(x => x.Transform.gameObject == other);
		if (bodyPart == null)
			return;
		switch (bodyPart.Group)
		{
			case BodyHelper002.BodyPartGroup.None:
			case BodyHelper002.BodyPartGroup.Foot:
			// case BodyHelper002.BodyPartGroup.LegUpper:
			case BodyHelper002.BodyPartGroup.LegLower:
			case BodyHelper002.BodyPartGroup.Hand:
				// case BodyHelper002.BodyPartGroup.ArmLower:
				// case BodyHelper002.BodyPartGroup.ArmUpper:
				break;
			default:
				// AddReward(-100f);
				if (!_isDone)
				{
					Done();
				}
				break;
		}
	}
    /*
	void HandleControllerTraining()
	{
		StepsUntilChange--;
		if (StepsUntilChange > 0)
			return;
		var rnd = UnityEngine.Random.value;
		bool repeateAction = false;
		int action = AsAction();
		if (action != 0 && rnd > .6f)
			repeateAction = true;
		if (!repeateAction)
		{
			rnd = UnityEngine.Random.value;
			if (rnd <= .4f)
				action = 1; // right
			else if (rnd <= .8f)
				action = 2; // left
			else
				action = 0; // stand
			rnd = UnityEngine.Random.value;
			if (rnd >= .75)
				action += 3; // add jump
		}
		StepsUntilChange = 40 + (int)(UnityEngine.Random.value * 200);
		SetAction(action);
	}
	int AsAction()
	{
		int action = 0;
		if (MoveRight && MoveLeft)
			action = 0;
		else if (!MoveRight && !MoveLeft)
			action = 0;
		else if (MoveRight)
			action = 1;
		else
			action = 2;
		if (Jump)
			action += 3;
		return action;
	}
	void SetAction(int action)
	{
		Jump = false;
		if (action >= 3)
		{
			action -= 3;
			Jump = true;
		}
		MoveRight = true ? action == 1 : false;
		MoveLeft = true ? action == 2 : false;
	}
    */

	public static BodyConfig BodyConfig = new BodyConfig
	{
		GetBodyPartGroup = (name) =>
		{
			name = name.ToLower();
			if (name.Contains("mixamorig"))
				return BodyPartGroup.None;

			if (name.Contains("torso"))
				return BodyPartGroup.Torso;
			if (name.Contains("thigh"))
				return BodyPartGroup.LegUpper;
			if (name.Contains("leg"))
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

			if (name.Contains("torso"))
				return MuscleGroup.Torso;
			if (name.Contains("thigh"))
				return MuscleGroup.LegUpper;
			if (name.Contains("leg"))
				return MuscleGroup.LegLower;
			if (name.Contains("foot"))
				return MuscleGroup.Foot;

			return MuscleGroup.None;
		},
        GetRootBodyPart = () => BodyPartGroup.Torso,
        GetRootMuscle = () => MuscleGroup.Torso
    };

}
