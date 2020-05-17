using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

public class DReConObservationStats : MonoBehaviour
{
    [System.Serializable]
    public class Stat
    {
        public string Name;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngualrVelocity;
        [HideInInspector]
        public Vector3 LastPosition;
        [HideInInspector]
        public Quaternion LastRotation;
        [HideInInspector]
        public bool LastIsSet;
    }

    public MonoBehaviour ObjectToTrack;
    List<string> _bodyPartsToTrack;

    [Header("Anchor stats")]
    public Vector3 HorizontalDirection; // Normalized vector in direction of travel (assume right angle to floor)
    // public Vector3 CenterOfMassInWorldSpace; 
    public Vector3 AngualrVelocity;

    [Header("Stats, relative to HorizontalDirection & Center Of Mass")]
    public Vector3 CenterOfMassVelocity;
    public float CenterOfMassVelocityMagnitude;
    public float CenterOfMassHorizontalVelocityMagnitude;
    public List<Stat> Stats;

    // [Header("... for debugging")]

    [HideInInspector]
    public Vector3 LastCenterOfMassInWorldSpace;
    [HideInInspector]
    public Quaternion LastRotation;
    [HideInInspector]
    public bool LastIsSet;


    SpawnableEnv _spawnableEnv;
    List<Transform> _bodyParts;
    internal List<Rigidbody> _rigidbodyParts;
    internal List<ArticulationBody> _articulationBodyParts;

    public void OnAwake(List<string> bodyPartsToTrack, Transform defaultTransform)
    {
        _bodyPartsToTrack = bodyPartsToTrack;
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _rigidbodyParts = ObjectToTrack.GetComponentsInChildren<Rigidbody>().ToList();
        _articulationBodyParts = ObjectToTrack.GetComponentsInChildren<ArticulationBody>().ToList();
        if (_rigidbodyParts?.Count > 0)
            _bodyParts = _rigidbodyParts
                .SelectMany(x=>x.GetComponentsInChildren<Transform>())
                .Distinct()
                .ToList();
        else
            _bodyParts = _articulationBodyParts
                .SelectMany(x=>x.GetComponentsInChildren<Transform>())
                .Distinct()
                .ToList();
        if (_bodyPartsToTrack?.Count > 0)
            _bodyParts = _bodyPartsToTrack
                .Where(x=>_bodyPartsToTrack.Contains(x))
                .Select(x=>_bodyParts.First(y=>y.name == x))
                .ToList();
        Stats = _bodyParts
            .Select(x=> new Stat{Name = x.name})
            .ToList();
        
        transform.position = defaultTransform.position;
        transform.rotation = defaultTransform.rotation;
    }

    public void OnReset()
    {
        foreach (var bodyPart in Stats)
        {
            bodyPart.LastIsSet = false;
        }
        LastIsSet = false;
    }

    public void SetStatusForStep(float timeDelta)
    {
        // find Center Of Mass and velocity
        Vector3 newCOM;
        if (_rigidbodyParts?.Count > 0)
            newCOM = GetCenterOfMass(_rigidbodyParts);
        else
            newCOM = GetCenterOfMass(_articulationBodyParts);
        if (!LastIsSet)
        {
            LastCenterOfMassInWorldSpace = newCOM;
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
            Stat bodyPartStat = Stats.First(x=>x.Name == bodyPart.name);
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
    }

	Vector3 GetCenterOfMass(IEnumerable<Rigidbody> bodies)
	{
		var centerOfMass = Vector3.zero;
		float totalMass = 0f;
		foreach (Rigidbody ab in bodies)
		{
			centerOfMass += ab.worldCenterOfMass * ab.mass;
			totalMass += ab.mass;
		}
		centerOfMass /= totalMass;
		centerOfMass -= _spawnableEnv.transform.position;
		return centerOfMass;
	}
	Vector3 GetCenterOfMass(IEnumerable<ArticulationBody> bodies)
	{
        // var root = bodies.First(x=>x.isRoot);
        // var centerOfMass = root.worldCenterOfMass;
		// centerOfMass -= _spawnableEnv.transform.position;
		// return centerOfMass;
		var centerOfMass = Vector3.zero;
		float totalMass = 0f;
		foreach (ArticulationBody ab in bodies)
		{
			centerOfMass += ab.worldCenterOfMass * ab.mass;
			totalMass += ab.mass;
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
