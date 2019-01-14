using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using System.Linq;

public class RewardHackAgent : Agent
{
    public int NumSteps;
    public List<float> Actions = new List<float>();
    public List<float> SoftMaxActions = new List<float>();

    int _curStep;

    void Start()
    {
    }
    void Init()
    {
        var numActions = brain.brainParameters.vectorObservationSize;
        Actions = Enumerable.Range(0, numActions).Select(x => 0f).ToList();
        SoftMaxActions = Enumerable.Range(0, numActions).Select(x => 0f).ToList();
        _curStep = NumSteps;
    }

	override public void CollectObservations()
    {
        if (Actions.Count == 0) {
            Init();
        }

        AddVectorObs(Actions); 
    }

	public override void AgentAction(float[] vectorAction, string textAction)
    {
        if (Actions.Count == 0) {
            Init();
        }
        var softmaxActions = SoftMax(vectorAction).ToArray();;
        for (int i = 0; i < vectorAction.Length; i++)
        {
            Actions[i] = vectorAction[i];
            SoftMaxActions[i] = softmaxActions[i];
        }
    }

    public float ScoreObservations(List<float> hints, float targetReward)
    {
        RecordReward(targetReward);
        var reward = 0f;

        if (Actions.Count == 0) {
            Init();
            return reward;
        }
        // using Softmax
        // var scoredObs = observations.Zip(SoftMaxActions, (x,y)=>(x*y));
        // reward = scoredObs.Sum();

        // using clamp & divide over size
        var actions = Actions.Select(x=>(x+1f)/2f).ToList(); // convert -1 to 1, to, 0 to 1
        var hintScores = hints
            .Select(x=>x*targetReward) // scale hints to be no more than target
            .Zip(actions, (x,y)=>(x*y))
            .ToList();
        reward = hintScores.Sum();
        reward /= Actions.Count;
        reward += targetReward;
        reward /= 2f;

        return reward;
    }

    void RecordReward(float reward)
    {
        AddReward(reward);
        // _curStep++;
        // if (_curStep >= NumSteps)
        // {
            RequestDecision();
            _curStep = 0;
        // }
    }

    IEnumerable<float> SoftMax(IEnumerable<float> vector)
    {
        var z_exp = vector.Select(Mathf.Exp);
    	var sum_z_exp = z_exp.Sum();
    	var softmax = z_exp.Select(i => i / sum_z_exp);
        return softmax;
    }
}
