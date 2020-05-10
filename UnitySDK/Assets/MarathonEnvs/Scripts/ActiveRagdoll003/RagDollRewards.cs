using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;
using UnityEngine.Assertions;
public class RagDollRewards : MonoBehaviour
{
    [Header("Reward")]
    public float SumOfSubRewards;
    public float Reward;

    [Header("Position Reward")]
    public float SumOfDistances;
    public float PositionReward;

    // [Header("Velocity Reward")]
    // [Header("Local Pose Reward")]
   
    
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
        _ragDoll = _spawnableEnv.GetComponentInChildren<RagDollController>().gameObject;
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

        // fall factor
        HeadDistance = (_mocapHead.position - _ragDollHead.position).magnitude;
        FallFactor = Mathf.Pow(HeadDistance,2);
        FallFactor = 1.4f*FallFactor;
        FallFactor = 1.3f-FallFactor;
        FallFactor = Mathf.Clamp(FallFactor, 0f, 1f);

        // reward
        SumOfSubRewards = PositionReward+ComReward;
        Reward = FallFactor*SumOfSubRewards;
    }


    // List<float> CompareCapusals(CapsuleCollider capsuleA, CapsuleCollider capsuleB)
    // {
    //     var pointsA = GetCapusalPoints(capsuleA);
    //     var pointsB = GetCapusalPoints(capsuleB);
    //     var distances = new List<float>();
    //     for (int i = 0; i < pointsB.Count; i++)
    //     {
    //         var d = (pointsA[i]-pointsB[i]).magnitude;
    //         distances.Add(d);
    //     }
    //     return distances;
    // }
    // List<Vector3> GetCapusalPoints(CapsuleCollider capsule)
    // {
    //     Vector3 ls = capsule.transform.lossyScale;
    //     Vector3 direction;
    //     float rScale;
    //     switch (capsule.direction)
    //     {
    //         case (0):
    //             direction = capsule.transform.right;
    //             rScale = Mathf.Max(Mathf.Abs(ls.y), Mathf.Abs(ls.z));
    //             break;
    //         default:
    //         case (1):
    //             direction = capsule.transform.up;
    //             rScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.z));
    //             break;
    //         case (2):
    //             direction = capsule.transform.forward;
    //             rScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y));
    //             break;
    //     }
    //     Vector3 toCenter = capsule.transform.TransformDirection(new Vector3(capsule.center.x * ls.x, capsule.center.y * ls.y, capsule.center.z * ls.z));
    //     Vector3 center = capsule.transform.position + toCenter;
    //     float radius = capsule.radius * rScale;
    //     float halfHeight = capsule.height * Mathf.Abs(ls[capsule.direction]) * 0.5f;
    //     var result = new List<Vector3>();
    //     switch (capsule.direction)
    //     {
    //         case (0):
    //             result.Add( new Vector3(center.x + halfHeight, center.y, center.z));
    //             result.Add( new Vector3(center.x - halfHeight, center.y, center.z));
    //             result.Add( new Vector3(center.x, center.y + radius, center.z));
    //             result.Add( new Vector3(center.x, center.y - radius, center.z));
    //             result.Add( new Vector3(center.x, center.y, center.z + radius));
    //             result.Add( new Vector3(center.x, center.y, center.z - radius));
    //             break;
    //         case (1):
    //             result.Add( new Vector3(center.x + radius, center.y, center.z));
    //             result.Add( new Vector3(center.x - radius, center.y, center.z));
    //             result.Add( new Vector3(center.x, center.y + halfHeight, center.z));
    //             result.Add( new Vector3(center.x, center.y - halfHeight, center.z));
    //             result.Add( new Vector3(center.x, center.y, center.z + radius));
    //             result.Add( new Vector3(center.x, center.y, center.z - radius));
    //             break;
    //         case (2):
    //             result.Add( new Vector3(center.x + radius, center.y, center.z));
    //             result.Add( new Vector3(center.x - radius, center.y, center.z));
    //             result.Add( new Vector3(center.x, center.y + radius, center.z));
    //             result.Add( new Vector3(center.x, center.y - radius, center.z));
    //             result.Add( new Vector3(center.x, center.y, center.z + halfHeight));
    //             result.Add( new Vector3(center.x, center.y, center.z - halfHeight));
    //             break;
    //     }

    //     return result;
    //     // capsule.transform.forward * 
    // }    
}
