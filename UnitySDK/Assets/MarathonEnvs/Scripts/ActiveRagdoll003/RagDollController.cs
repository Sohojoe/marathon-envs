using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class RagDollController : MonoBehaviour 
{
    MocapController _mocapController;
    List<Rigidbody> _mocapBodyParts;
    List<Rigidbody> _bodyParts;
    SpawnableEnv _spawnableEnv;
    RagDollObservations _ragDollObservations;
    RagDollRewards _ragDollRewards;

	void Awake()
    {
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
        _mocapBodyParts = _mocapController.GetComponentsInChildren<Rigidbody>().ToList();
        _bodyParts = GetComponentsInChildren<Rigidbody>().ToList();
        _ragDollObservations = GetComponent<RagDollObservations>();
        _ragDollRewards = GetComponent<RagDollRewards>();
    }
    void FixedUpdate()
    {
        _ragDollObservations.OnStep();
        _ragDollRewards.OnStep();
    }    
}
