using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class RagDollRewards : MonoBehaviour
{
    [Header("Reward")]
    public float Reward;

    [Header("Position Reward")]
    [Header("Velocity Reward")]
    [Header("Center of Mass Velocity Reward")]
    public Vector3 MocapCOMVelocity;
    public Vector3 RagDollCOMVelocity;
    public float COMVelocityDifference;
    public float ComReward;


//  fall factor
    [Header("Fall Factor")]

    SpawnableEnv _spawnableEnv;
    GameObject _mocap;
    GameObject _ragDoll;

    BodyStats _mocapBodyStats;
    BodyStats _ragDollBodyStats;

    bool _hasLazyInit;

    void Awake()
    {
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _mocap = _spawnableEnv.GetComponentInChildren<MocapController>().gameObject;
        _ragDoll = _spawnableEnv.GetComponentInChildren<RagDollController>().gameObject;
    }

    void LazyInit()
    {
        _hasLazyInit = true;
        var bodyStats = _spawnableEnv.GetComponentsInChildren<BodyStats>();
        _mocapBodyStats = bodyStats.First(x=>x.name == "MocapCenterOfMass");
        _ragDollBodyStats = bodyStats.First(x=>x.name == "RagDollCenterOfMass");
    }

    // Update is called once per frame
    public void OnStep()
    {
        if (!_hasLazyInit)
            LazyInit();

        // center of mass velocity reward
        MocapCOMVelocity = _mocapBodyStats.CenterOfMassVelocity;
        RagDollCOMVelocity = _ragDollBodyStats.CenterOfMassVelocity;
        COMVelocityDifference = (MocapCOMVelocity-RagDollCOMVelocity).magnitude;
        ComReward = -Mathf.Pow(COMVelocityDifference,2);
        ComReward = Mathf.Exp(ComReward);
    }
}
