using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class RagDollObservations : MonoBehaviour
{
    [Header("Observations")]

    [Tooltip("Kinematic character center of mass velocity, Vector3")]
    public Vector3 MocapCOMVelocity;

    [Tooltip("RagDoll character center of mass velocity, Vector3")]
    public Vector3 RagDollCOMVelocity;

    [Tooltip("User-input desired horizontal CM velocity. Vector2")]
    public Vector2 InputDesiredHorizontalVelocity;

    [Tooltip("User-input requests jump, bool")]
    public bool InputJump;

    [Tooltip("User-input requests backflip, bool")]
    public bool InputBackflip;

    [Tooltip("Difference between RagDoll character horizontal CM velocity and user-input desired horizontal CM velocity. Vector2")]
    public Vector2 HorizontalVelocityDifference;

    [Tooltip("Positions and velocities for subset of bodies")]
    public List<BodyPartDifferenceStats> BodyPartDifferenceStats;

    [Tooltip("Smoothed actions produced in the previous step of the policy are collected in t −1")]
    public float[] PreviousActions;

    [Header("Settings")]
    public List<string> BodyPartsToTrack;

    [Header("... debug")]
    public Vector2 MocapHorizontalVelocityDifference;
    public bool debugCopyMocap;


    InputController _inputController;
    SpawnableEnv _spawnableEnv;
    TrackBodyStatesInWorldSpace _trackBodyStatesInWorldSpace;
    DReConObservationStats _mocapBodyStats;
    DReConObservationStats _ragDollBodyStats;


    // Start is called before the first frame update
    void Awake()
    {
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _inputController = _spawnableEnv.GetComponentInChildren<InputController>();
        BodyPartDifferenceStats = BodyPartsToTrack
            .Select(x=> new BodyPartDifferenceStats{Name = x})
            .ToList();

        _mocapBodyStats= new GameObject("MocapDReConObservationStats").AddComponent<DReConObservationStats>();
        var mocapController = _spawnableEnv.GetComponentInChildren<MocapController>();
        _mocapBodyStats.ObjectToTrack = mocapController;
        _mocapBodyStats.transform.SetParent(_spawnableEnv.transform);
        _mocapBodyStats.OnAwake(BodyPartsToTrack, _mocapBodyStats.ObjectToTrack.transform);

        _ragDollBodyStats= new GameObject("RagDollDReConObservationStats").AddComponent<DReConObservationStats>();
        _ragDollBodyStats.ObjectToTrack = this;
        _ragDollBodyStats.transform.SetParent(_spawnableEnv.transform);
        _ragDollBodyStats.OnAwake(BodyPartsToTrack, transform);

        _trackBodyStatesInWorldSpace = mocapController.GetComponent<TrackBodyStatesInWorldSpace>();
    }

    public void OnStep()
    {
        float timeDelta = Time.fixedDeltaTime;
        _mocapBodyStats.SetStatusForStep(timeDelta);
        _ragDollBodyStats.SetStatusForStep(timeDelta);
        UpdateObservations(timeDelta);
        if (debugCopyMocap)
        {
            debugCopyMocap = false;
            _trackBodyStatesInWorldSpace.CopyStatesTo(this.gameObject);
            _mocapBodyStats.OnReset();
            _ragDollBodyStats.OnReset();
            _ragDollBodyStats.transform.position = _mocapBodyStats.transform.position;
            _ragDollBodyStats.transform.rotation = _mocapBodyStats.transform.rotation;
        }
    }

    public void UpdateObservations(float timeDelta)
    {

        MocapCOMVelocity = _mocapBodyStats.CenterOfMassVelocity;
        RagDollCOMVelocity = _ragDollBodyStats.CenterOfMassVelocity;
        InputDesiredHorizontalVelocity = _inputController.DesiredHorizontalVelocity;
        InputJump = _inputController.Jump;
        InputBackflip = _inputController.Backflip;
        Vector2 ragDollHorizontalVelocity = new Vector2(RagDollCOMVelocity.x, RagDollCOMVelocity.z);
        HorizontalVelocityDifference = InputDesiredHorizontalVelocity-ragDollHorizontalVelocity;
        // BodyPartStats = 
        foreach (var differenceStats in BodyPartDifferenceStats)
        {
            var mocapStats = _mocapBodyStats.Stats.First(x=>x.Name == differenceStats.Name);
            var ragDollStats = _ragDollBodyStats.Stats.First(x=>x.Name == differenceStats.Name);

            differenceStats.Position = mocapStats.Position - ragDollStats.Position;
            differenceStats.Velocity = mocapStats.Velocity - ragDollStats.Velocity;
            differenceStats.AngualrVelocity = mocapStats.AngualrVelocity - ragDollStats.AngualrVelocity;
            differenceStats.Rotation = DReConObservationStats.GetAngularVelocity(ragDollStats.Rotation, mocapStats.Rotation, timeDelta);
        }
        // PreviousActions =         

        // debug
        Vector2 mocapHorizontalVelocity = new Vector2(MocapCOMVelocity.x, MocapCOMVelocity.z);
        MocapHorizontalVelocityDifference = InputDesiredHorizontalVelocity-mocapHorizontalVelocity;

    }
}
