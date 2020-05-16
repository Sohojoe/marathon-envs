using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;
using UnityEngine.Assertions;
public class DReConRewards : MonoBehaviour
{
    [Header("Reward")]
    public float SumOfSubRewards;
    public float Reward;

    [Header("Position Reward")]
    public float SumOfDistances;
    public float PositionReward;

    [Header("Velocity Reward")]
    public float MocapPointsVelocity;
    public float RagDollPointsVelocity;    
    public float PointsVelocityDifference;
    public float PointsVelocityReward;

    [Header("Local Pose Reward")]
    public List<float> RotationDifferencesInAngles;
    public float SumOfRotationDifferences;
    public float LocalPoseReward;

   
    
    [Header("Center of Mass Velocity Reward")]
    public Vector3 MocapCOMVelocity;
    public Vector3 RagDollCOMVelocity;
    public float COMVelocityDifference;
    public float ComReward;


//  fall factor
    [Header("Fall Factor")]
    public float HeadDistance;
    public float FallFactor;

    SpawnableEnv _spawnableEnv;
    GameObject _mocap;
    GameObject _ragDoll;

    DReConRewardStats _mocapBodyStats;
    DReConRewardStats _ragDollBodyStats;

    List<Rigidbody> _mocapBodyParts;
    List<Rigidbody> _ragDollBodyParts;
    Transform _mocapHead;
    Transform _ragDollHead;

    bool _hasLazyInit;

    void Awake()
    {
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        Assert.IsNotNull(_spawnableEnv);
        _mocap = _spawnableEnv.GetComponentInChildren<MocapController>().gameObject;
        _ragDoll = _spawnableEnv.GetComponentInChildren<RagDollAgent>().gameObject;
        Assert.IsNotNull(_mocap);
        Assert.IsNotNull(_ragDoll);
        _mocapBodyParts = _mocap.GetComponentsInChildren<Rigidbody>().ToList();
        _ragDollBodyParts = _ragDoll.GetComponentsInChildren<Rigidbody>().ToList();
        Assert.AreEqual(_mocapBodyParts.Count, _ragDollBodyParts.Count);
        _mocapHead = _mocap
            .GetComponentsInChildren<Transform>()
            .First(x=>x.name == "head");
        _ragDollHead = _ragDoll
            .GetComponentsInChildren<Transform>()
            .First(x=>x.name == "head");
        _mocapBodyStats= new GameObject("MocapDReConRewardStats").AddComponent<DReConRewardStats>();
        var mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
        _mocapBodyStats.ObjectToTrack = mocapController;
        _mocapBodyStats.transform.SetParent(_spawnableEnv.transform);
        _mocapBodyStats.OnAwake(_mocapBodyStats.ObjectToTrack.transform);

        _ragDollBodyStats= new GameObject("RagDollDReConRewardStats").AddComponent<DReConRewardStats>();
        _ragDollBodyStats.ObjectToTrack = this;
        _ragDollBodyStats.transform.SetParent(_spawnableEnv.transform);
        _ragDollBodyStats.OnAwake(transform, _mocapBodyStats);      

        _mocapBodyStats.AssertIsCompatible(_ragDollBodyStats);      
    }

    // Update is called once per frame
    public void OnStep()
    {
        float timeDelta = Time.fixedDeltaTime;
        _mocapBodyStats.SetStatusForStep(timeDelta);
        _ragDollBodyStats.SetStatusForStep(timeDelta);

        // position reward
        List<float> distances = _mocapBodyStats.GetPointDistancesFrom(_ragDollBodyStats);
        SumOfDistances = distances.Sum();
        PositionReward = -10f/distances.Count;
        PositionReward *= Mathf.Pow(SumOfDistances, 2);
        PositionReward = Mathf.Exp(PositionReward);

        // center of mass velocity reward
        MocapCOMVelocity = _mocapBodyStats.CenterOfMassVelocity;
        RagDollCOMVelocity = _ragDollBodyStats.CenterOfMassVelocity;
        COMVelocityDifference = (MocapCOMVelocity-RagDollCOMVelocity).magnitude;
        ComReward = -Mathf.Pow(COMVelocityDifference,2);
        ComReward = Mathf.Exp(ComReward);

        // points velocity
        MocapPointsVelocity = _mocapBodyStats.PointVelocity.Sum();
        RagDollPointsVelocity = _ragDollBodyStats.PointVelocity.Sum();
        var pointsDifference = _mocapBodyStats.PointVelocity
            .Zip(_ragDollBodyStats.PointVelocity, (a,b )=> a-b);
        PointsVelocityDifference = pointsDifference.Sum();
        PointsVelocityReward = Mathf.Pow(PointsVelocityDifference, 2);
        PointsVelocityReward = (-1f/_mocapBodyStats.PointVelocity.Length) * PointsVelocityReward;
        PointsVelocityReward = Mathf.Exp(PointsVelocityReward);

        // local pose reward
        if (RotationDifferencesInAngles == null || RotationDifferencesInAngles.Count < _mocapBodyStats.Rotations.Count)
            RotationDifferencesInAngles = Enumerable.Range(0,_mocapBodyStats.Rotations.Count)
            .Select(x=>0f)
            .ToList();
        SumOfRotationDifferences = 0f;
        for (int i = 0; i < _mocapBodyStats.Rotations.Count; i++)
        {
            var angle = Quaternion.Angle(_mocapBodyStats.Rotations[i], _ragDollBodyStats.Rotations[i]);
            RotationDifferencesInAngles[i] = angle;
            angle = Mathf.Abs(angle);
            Assert.IsTrue(angle <= 180f);
            SumOfRotationDifferences += angle/180f;
        }
        LocalPoseReward = -10f/_mocapBodyStats.Rotations.Count;
        LocalPoseReward *= SumOfRotationDifferences;
        LocalPoseReward = Mathf.Exp(LocalPoseReward);

        // fall factor
        HeadDistance = (_mocapHead.position - _ragDollHead.position).magnitude;
        FallFactor = Mathf.Pow(HeadDistance,2);
        FallFactor = 1.4f*FallFactor;
        FallFactor = 1.3f-FallFactor;
        FallFactor = Mathf.Clamp(FallFactor, 0f, 1f);

        // reward
        SumOfSubRewards = PositionReward+ComReward+PointsVelocityReward+LocalPoseReward;
        Reward = FallFactor*SumOfSubRewards;
    }
    public void OnReset()
    {
        _mocapBodyStats.OnReset();
        _ragDollBodyStats.OnReset();
        _ragDollBodyStats.transform.position = _mocapBodyStats.transform.position;
        _ragDollBodyStats.transform.rotation = _mocapBodyStats.transform.rotation;
    }
}
