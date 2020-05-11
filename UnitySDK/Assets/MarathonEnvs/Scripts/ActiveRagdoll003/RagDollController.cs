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
    DReConObservations _dReConObservations;
    DReConRewards _dReConRewards;
    TrackBodyStatesInWorldSpace _trackBodyStatesInWorldSpace;


	void Awake()
    {
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
        _mocapBodyParts = _mocapController.GetComponentsInChildren<Rigidbody>().ToList();
        _bodyParts = GetComponentsInChildren<Rigidbody>().ToList();
        _dReConObservations = GetComponent<DReConObservations>();
        _dReConRewards = GetComponent<DReConRewards>();
        var mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
        _trackBodyStatesInWorldSpace = mocapController.GetComponent<TrackBodyStatesInWorldSpace>();
    }
    void FixedUpdate()
    {
        _dReConObservations.OnStep();
        _dReConRewards.OnStep();
        if (debugCopyMocap)
        {
            debugCopyMocap = false;
            _trackBodyStatesInWorldSpace.CopyStatesTo(this.gameObject);
            _dReConObservations.OnReset();
            _dReConRewards.OnReset();
        }
    }
}
