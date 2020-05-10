using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class RagDollController : MonoBehaviour 
{
    [Header("... debug")]
    public bool debugCopyMocap;

    MocapController _mocapController;
    List<Rigidbody> _mocapBodyParts;
    List<Rigidbody> _bodyParts;
    SpawnableEnv _spawnableEnv;
    RagDollObservations _ragDollObservations;
    RagDollRewards _ragDollRewards;
    TrackBodyStatesInWorldSpace _trackBodyStatesInWorldSpace;


	void Awake()
    {
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
        _mocapBodyParts = _mocapController.GetComponentsInChildren<Rigidbody>().ToList();
        _bodyParts = GetComponentsInChildren<Rigidbody>().ToList();
        _ragDollObservations = GetComponent<RagDollObservations>();
        _ragDollRewards = GetComponent<RagDollRewards>();
        var mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
        _trackBodyStatesInWorldSpace = mocapController.GetComponent<TrackBodyStatesInWorldSpace>();
    }
    void FixedUpdate()
    {
        _ragDollObservations.OnStep();
        _ragDollRewards.OnStep();
        if (debugCopyMocap)
        {
            debugCopyMocap = false;
            _trackBodyStatesInWorldSpace.CopyStatesTo(this.gameObject);
            _ragDollObservations.OnReset();
            _ragDollRewards.OnReset();
        }
    }
}
