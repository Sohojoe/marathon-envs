using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;
using UnityEngine.Assertions;

public class DReConRewardStats : MonoBehaviour
{
    // [System.Serializable]
    // public class Stat
    // {
    //     public CapsuleCollider CapsuleCollider;
    //     public Vector3 DirectionVector;
    //     public float Scale;
    //     public float Radius;
    //     public float HalfHeight;
    // }

    [Header("Settings")]

    public MonoBehaviour ObjectToTrack;

    [Header("Stats")]
    public Vector3 CenterOfMassVelocity;
    public float CenterOfMassVelocityMagnitude;


    [HideInInspector]
    public Vector3 LastCenterOfMassInWorldSpace;
    [HideInInspector]
    public bool LastIsSet;

    SpawnableEnv _spawnableEnv;
    List<CapsuleCollider> _capsuleColliders;
    List<Rigidbody> _rigidbodyParts;
    List<ArticulationBody> _articulationBodyParts;
    List<GameObject> _bodyParts;

    List<Transform> _trackRotations;
    public List<Quaternion> Rotations;
    public Vector3[] Points;
    Vector3[] _lastPoints;
    public float[] PointVelocity;


    public void OnAwake(Transform defaultTransform, DReConRewardStats orderToCopy = null)
    {
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _articulationBodyParts = ObjectToTrack
            .GetComponentsInChildren<ArticulationBody>()
            .Distinct()
            .ToList();
        _rigidbodyParts = ObjectToTrack
            .GetComponentsInChildren<Rigidbody>()
            .Distinct()
            .ToList();
        if (_rigidbodyParts?.Count>0)
            _bodyParts = _rigidbodyParts.Select(x=>x.gameObject).ToList();
        else
            _bodyParts = _articulationBodyParts.Select(x=>x.gameObject).ToList();
        _trackRotations = _bodyParts
            .SelectMany(x=>x.GetComponentsInChildren<Transform>())
            .Distinct()
            .ToList();
        _capsuleColliders = _bodyParts
            .SelectMany(x=>x.GetComponentsInChildren<CapsuleCollider>())
            .Distinct()
            .ToList();
        if (orderToCopy != null)
        {
            _bodyParts = orderToCopy._bodyParts
                .Select(x=>_bodyParts.First(y=>y.name == x.name))
                .ToList();
            _trackRotations = orderToCopy._trackRotations
                .Select(x=>_trackRotations.First(y=>y.name == x.name))
                .ToList();
            _capsuleColliders = orderToCopy._capsuleColliders
                .Select(x=>_capsuleColliders.First(y=>y.name == x.name))
                .ToList();
        }
        Points = Enumerable.Range(0,_capsuleColliders.Count * 6)
            .Select(x=>Vector3.zero)
            .ToArray();
        _lastPoints = Enumerable.Range(0,_capsuleColliders.Count * 6)
            .Select(x=>Vector3.zero)
            .ToArray();            
        PointVelocity = Enumerable.Range(0,_capsuleColliders.Count * 6)
            .Select(x=>0f)
            .ToArray();
        Rotations = Enumerable.Range(0,_trackRotations.Count)
            .Select(x=>Quaternion.identity)
            .ToList();
        
        transform.position = defaultTransform.position;
        transform.rotation = defaultTransform.rotation;
    }
    public void OnReset()
    {
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
        var newHorizontalDirection = new Vector3(CenterOfMassVelocity.x, 0f, CenterOfMassVelocity.z);
        CenterOfMassVelocityMagnitude = CenterOfMassVelocity.magnitude;
        if (newHorizontalDirection.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(newHorizontalDirection.normalized, Vector3.up);
        }
        LastCenterOfMassInWorldSpace = newCOM;
        
        GetAllPoints(Points);
        if (!LastIsSet)
        {
            Array.Copy(Points, 0, _lastPoints, 0, Points.Length);
        }
        for (int i = 0; i < Points.Length; i++)
        {
            PointVelocity[i] = (Points[i] - _lastPoints[i]).magnitude / timeDelta;
        }
        Array.Copy(Points, 0, _lastPoints, 0, Points.Length);

        for (int i = 0; i < _trackRotations.Count; i++)
        {
            Quaternion localRotation = Quaternion.Inverse(transform.rotation) * _trackRotations[i].rotation;
            Rotations[i] = localRotation;
        }

        LastIsSet = true;
    }

    public List<float> GetPointDistancesFrom(DReConRewardStats target)
    {
        List<float> distances = new List<float>();
        for (int i = 0; i < Points.Length; i++)
        {
            float distance = (Points[i] - target.Points[i]).magnitude;
            distances.Add(distance);
        }
        return distances;
    }
    public void AssertIsCompatible(DReConRewardStats target)
    {
        Assert.AreEqual(Points.Length, target.Points.Length);
        Assert.AreEqual(_lastPoints.Length, target._lastPoints.Length);
        Assert.AreEqual(PointVelocity.Length, target.PointVelocity.Length);
        Assert.AreEqual(Points.Length, _lastPoints.Length);
        Assert.AreEqual(Points.Length, PointVelocity.Length);
        Assert.AreEqual(_capsuleColliders.Count, target._capsuleColliders.Count);
        for (int i = 0; i < _capsuleColliders.Count; i++)
        {
            string debugStr = $" _capsuleColliders.{_capsuleColliders[i].name} vs target._capsuleColliders.{target._capsuleColliders[i].name}";
            Assert.AreEqual(_capsuleColliders[i].name, target._capsuleColliders[i].name, $"name:{debugStr}");
            Assert.AreEqual(_capsuleColliders[i].direction, target._capsuleColliders[i].direction, $"direction:{debugStr}");
            Assert.AreEqual(_capsuleColliders[i].height, target._capsuleColliders[i].height, $"height:{debugStr}");
            Assert.AreEqual(_capsuleColliders[i].radius, target._capsuleColliders[i].radius, $"radius:{debugStr}");
        }
    }

    void GetAllPoints(Vector3[] pointBuffer)
    {
        int idx = 0;
        foreach (var capsule in _capsuleColliders)
        {
            idx = SetCapusalPoints(capsule, pointBuffer, idx);
        }
    }

    int SetCapusalPoints(CapsuleCollider capsule, Vector3[] pointBuffer, int idx)
    {
        Vector3 ls = capsule.transform.lossyScale;
        Vector3 direction;
        float rScale;
        switch (capsule.direction)
        {
            case (0):
                direction = capsule.transform.right;
                rScale = Mathf.Max(Mathf.Abs(ls.y), Mathf.Abs(ls.z));
                break;
            case (1):
                direction = capsule.transform.forward;
                rScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y));
                break;
            default:
                direction = capsule.transform.up;
                rScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.z));
                break;
        }
        Vector3 toCenter = capsule.transform.TransformDirection(new Vector3(capsule.center.x * ls.x, capsule.center.y * ls.y, capsule.center.z * ls.z));
        Vector3 center = capsule.transform.position + toCenter;
        float radius = capsule.radius * rScale;
        float halfHeight = capsule.height * Mathf.Abs(ls[capsule.direction]) * 0.5f;
        switch (capsule.direction)
        {
            case (0):
                pointBuffer[idx++] = new Vector3(center.x + halfHeight, center.y, center.z);
                pointBuffer[idx++] = new Vector3(center.x - halfHeight, center.y, center.z);
                pointBuffer[idx++] = new Vector3(center.x, center.y + radius, center.z);
                pointBuffer[idx++] = new Vector3(center.x, center.y - radius, center.z);
                pointBuffer[idx++] = new Vector3(center.x, center.y, center.z + radius);
                pointBuffer[idx++] = new Vector3(center.x, center.y, center.z - radius);
                break;
            case (1):
                pointBuffer[idx++] = new Vector3(center.x + radius, center.y, center.z);
                pointBuffer[idx++] = new Vector3(center.x - radius, center.y, center.z);
                pointBuffer[idx++] = new Vector3(center.x, center.y + halfHeight, center.z);
                pointBuffer[idx++] = new Vector3(center.x, center.y - halfHeight, center.z);
                pointBuffer[idx++] = new Vector3(center.x, center.y, center.z + radius);
                pointBuffer[idx++] = new Vector3(center.x, center.y, center.z - radius);
                break;
            case (2):
                pointBuffer[idx++] = new Vector3(center.x + radius, center.y, center.z);
                pointBuffer[idx++] = new Vector3(center.x - radius, center.y, center.z);
                pointBuffer[idx++] = new Vector3(center.x, center.y + radius, center.z);
                pointBuffer[idx++] = new Vector3(center.x, center.y - radius, center.z);
                pointBuffer[idx++] = new Vector3(center.x, center.y, center.z + halfHeight);
                pointBuffer[idx++] = new Vector3(center.x, center.y, center.z - halfHeight);
                break;
        }
        return idx;
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
	Vector3 GetCenterOfMass(IEnumerable<ArticulationBody> bodies)
	{
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
}
