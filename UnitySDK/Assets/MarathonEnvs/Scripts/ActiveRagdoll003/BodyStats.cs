﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class BodyStats : MonoBehaviour
{

    public MonoBehaviour ObjectToTrack; // Normalized vector in direction of travel (assume right angle to floor)
    List<string> _bodyPartsToTrack;

    [Header("Anchor stats")]
    public Vector3 HorizontalDirection; // Normalized vector in direction of travel (assume right angle to floor)
    // public Vector3 CenterOfMassInWorldSpace; 
    public Vector3 AngualrVelocity;

    [Header("Stats, relative to HorizontalDirection & Center Of Mass")]
    public Vector3 CenterOfMassVelocity;
    public float CenterOfMassVelocityMagnitude;
    public float CenterOfMassHorizontalVelocityMagnitude;
    public List<BodyPartStats> BodyPartStats;

    [Header("... for debugging")]
    public Vector3 ButtVelocity;
    public Vector3 ButtAngualrVelocity;
    public float ButtMagnatude;
    Vector3 _lastButtPossition;
    Quaternion _lastButtRotation;


    [HideInInspector]
    public Vector3 LastCenterOfMassInWorldSpace;
    [HideInInspector]
    public Quaternion LastRotation;
    [HideInInspector]
    public bool LastIsSet;


    SpawnableEnv _spawnableEnv;
    List<Transform> _bodyParts;
    internal List<Rigidbody> _rigidbodyParts;

    // Start is called before the first frame update
    void Awake()
    {

    }
    public void OnAwake(List<string> bodyPartsToTrack, Transform defaultTransform)
    {
        _bodyPartsToTrack = bodyPartsToTrack;
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _rigidbodyParts = ObjectToTrack.GetComponentsInChildren<Rigidbody>().ToList();
        _bodyParts = _rigidbodyParts
            .SelectMany(x=>x.GetComponentsInChildren<Transform>())
            .Distinct()
            .ToList();
        if (_bodyPartsToTrack?.Count > 0)
            _bodyParts = _bodyPartsToTrack
                .Where(x=>_bodyPartsToTrack.Contains(x))
                .Select(x=>_bodyParts.First(y=>y.name == x))
                .ToList();
        BodyPartStats = _bodyParts
            .Select(x=> new BodyPartStats{Name = x.name})
            .ToList();
        
        transform.position = defaultTransform.position;
        transform.rotation = defaultTransform.rotation;
    }

    public void OnReset()
    {
        foreach (var bodyPart in BodyPartStats)
        {
            bodyPart.LastIsSet = false;
        }
        LastIsSet = false;
    }

    public void SetStatusForStep(float timeDelta)
    {
        var butt = _bodyParts.First(x=>x.name.ToLower()=="butt");
        var buttStats = BodyPartStats.First(x=>x.Name.ToLower()=="butt");
        // find Center Of Mass and velocity
        Vector3 newCOM = GetCenterOfMass(_rigidbodyParts);
        if (!LastIsSet)
        {
            LastCenterOfMassInWorldSpace = newCOM;
            _lastButtPossition = butt.position;
            _lastButtRotation = butt.rotation;
        }
        transform.position = newCOM;
        CenterOfMassVelocity = transform.position - LastCenterOfMassInWorldSpace;
        CenterOfMassVelocity /= timeDelta;
        // generate Horizontal Direction
        var newHorizontalDirection = new Vector3(CenterOfMassVelocity.x, 0f, CenterOfMassVelocity.z);
        CenterOfMassVelocityMagnitude = CenterOfMassVelocity.magnitude;
        CenterOfMassHorizontalVelocityMagnitude = newHorizontalDirection.magnitude;
        if (newHorizontalDirection.magnitude > 0.1f)
        {
            HorizontalDirection = newHorizontalDirection.normalized;
            transform.rotation = Quaternion.LookRotation(newHorizontalDirection.normalized, Vector3.up);
        }
        if (!LastIsSet)
        {
            LastRotation = transform.rotation;
        }
        AngualrVelocity = GetAngularVelocity(transform.rotation, LastRotation, timeDelta);
        LastRotation = transform.rotation;
        LastCenterOfMassInWorldSpace = newCOM;
        LastIsSet = true;

        // get bodyParts stats in local space
        foreach (var bodyPart in _bodyParts)
        {
            BodyPartStats bodyPartStat = BodyPartStats.First(x=>x.Name == bodyPart.name);
            Vector3 localPosition = transform.InverseTransformPoint(bodyPart.position);
            // localPosition -= CenterOfMassVelocity * timeDelta;
            Quaternion localRotation = Quaternion.Inverse(transform.rotation) * bodyPart.rotation;
            if (!bodyPartStat.LastIsSet)
            {
                bodyPartStat.LastPosition = localPosition;
                bodyPartStat.LastRotation = localRotation;
            }
            bodyPartStat.Position = localPosition;
            bodyPartStat.Rotation = localRotation;
            bodyPartStat.Velocity = localPosition - bodyPartStat.LastPosition;
            bodyPartStat.Velocity /= timeDelta;
            bodyPartStat.AngualrVelocity = GetAngularVelocity(localRotation, bodyPartStat.LastRotation, timeDelta);
            bodyPartStat.AngualrVelocity += AngualrVelocity;
            bodyPartStat.LastPosition = localPosition;
            bodyPartStat.LastRotation = localRotation;
            bodyPartStat.LastIsSet = true;
        }

        // debug stats
        ButtVelocity = butt.position-_lastButtPossition;
        ButtVelocity /= timeDelta;
        ButtAngualrVelocity = GetAngularVelocity(butt.rotation, _lastButtRotation, timeDelta);
        _lastButtPossition = butt.position;
        _lastButtRotation = butt.rotation;
        ButtMagnatude = ButtVelocity.magnitude;
    }

	Vector3 GetCenterOfMass(IEnumerable<Rigidbody> bodies)
	{
		var centerOfMass = Vector3.zero;
		float totalMass = 0f;
		foreach (Rigidbody rb in bodies)
		{
			centerOfMass += rb.worldCenterOfMass * rb.mass;
			totalMass += rb.mass;
		}
		centerOfMass /= totalMass;
		centerOfMass -= _spawnableEnv.transform.position;
		return centerOfMass;
	}
    public static Vector3 GetAngularVelocity(Quaternion rotation, Quaternion lastRotation, float timeDelta)
    {
        var q = lastRotation * Quaternion.Inverse(rotation);
        float gain;
        if(q.w < 0.0f)
        {
            var angle = Mathf.Acos(-q.w);
            gain = -2.0f * angle / (Mathf.Sin(angle)*timeDelta);
            // gain = -2.0f * angle / (Mathf.Sin(angle)/timeDelta);
        }
        else
        {
            var angle = Mathf.Acos(q.w);
            gain = 2.0f * angle / (Mathf.Sin(angle)*timeDelta);
            // gain = 2.0f * angle / (Mathf.Sin(angle)/timeDelta);
        }
        var result = new Vector3(q.x * gain,q.y * gain,q.z * gain);
        var safeResult = new Vector3(
            float.IsNaN(result.x) ? 0f : result.x,
            float.IsNaN(result.y) ? 0f : result.y,
            float.IsNaN(result.z) ? 0f : result.z
        );
        return safeResult;
    }    
}
