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

    [Header("debug")]
    public Vector3 debugA;
    public Vector3 debugB;
    public Vector3 debugC;

    [HideInInspector]
    public Vector3 LastCenterOfMassInWorldSpace;
    [HideInInspector]
    public bool LastIsSet;

    SpawnableEnv _spawnableEnv;
    List<CapsuleCollider> _capsuleColliders;
    List<Rigidbody> _rigidbodyParts;
    List<ArticulationBody> _articulationBodyParts;
    List<GameObject> _bodyParts;

    GameObject _root;
    Quaternion _rootDefaultTransorm;

    List<GameObject> _trackRotations;
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
            .Select(x=>x.gameObject)
            .Distinct()
            .Where(x=>x.GetComponent<Rigidbody>() != null || x.GetComponent<ArticulationBody>() != null)
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
        if (_root == null)
        {
            _root = _bodyParts
                .First(x=>x.name=="butt");
            _rootDefaultTransorm = _root.transform.rotation;
        }        
        transform.position = defaultTransform.position;
        transform.rotation = defaultTransform.rotation;
    }
    public void OnReset()
    {
        OnAwake(this.transform, this);
        ResetStatus();
        LastIsSet = false;
    }
    public void ResetStatus()
    {
        CenterOfMassVelocity = Vector3.zero;
        CenterOfMassVelocityMagnitude = 0f;
        LastCenterOfMassInWorldSpace = transform.position;
        GetAllPoints(Points);
        Array.Copy(Points, 0, _lastPoints, 0, Points.Length);
        for (int i = 0; i < Points.Length; i++)
        {
            PointVelocity[i] = 0f;
        }
        for (int i = 0; i < _trackRotations.Count; i++)
        {
            Quaternion localRotation = Quaternion.Inverse(transform.rotation) * _trackRotations[i].transform.rotation;
            Rotations[i] = localRotation;
        }
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
        CenterOfMassVelocityMagnitude = CenterOfMassVelocity.magnitude;
        // var newHorizontalDirection = new Vector3(CenterOfMassVelocity.x, 0f, CenterOfMassVelocity.z);
        // if (newHorizontalDirection.magnitude > 0.1f)
        // {
        //     transform.rotation = Quaternion.LookRotation(newHorizontalDirection.normalized, Vector3.up);
        // }
        var newHorizontalDirection = new Vector3(
            _rootDefaultTransorm.eulerAngles.x, 
            _root.transform.rotation.y,
            _rootDefaultTransorm.eulerAngles.z);
        // debugA = _root.transform.TransformDirection(_root.transform.localRotation.eulerAngles);
        // debugB = _root.transform.localRotation.eulerAngles;
        // debugC = _root.transform.rotation.eulerAngles;
        debugA = _root.transform.forward;
        // debugB = _root.transform.TransformDirection(_root.transform.forward);
        debugB = _root.transform.localRotation.eulerAngles;
        debugC = _root.transform.up;
        debugA *= 180f;
        // debugB *= 180f;
        debugC *= 180f;
        debugA += new Vector3(180f, 180f, 180f);
        // debugB += new Vector3(180f, 180f, 180f);
        debugC += new Vector3(180f, 180f, 180f);
        newHorizontalDirection = new Vector3(
            _rootDefaultTransorm.eulerAngles.x,
            debugC.z,
            _rootDefaultTransorm.eulerAngles.z);        
        this.transform.rotation = Quaternion.Euler(newHorizontalDirection);
        // transform.rotation = Quaternion.LookRotation(newHorizontalDirection.normalized, _rootDefaultTransorm.eulerAngles);
        // transform.rotation = Quaternion.LookRotation(newHorizontalDirection.normalized, _rootDefaultTransorm.eulerAngles.normalized);
        // transform.rotation = Quaternion.LookRotation(newHorizontalDirection);
        // transform.rotation = _root.transform.TransformDirection(newHorizontalDirection);
        // debugA = newHorizontalDirection;
        // debugB = _rootDefaultTransorm.eulerAngles;
        // debugC = transform.rotation.eulerAngles;
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
            Quaternion localRotation = Quaternion.Inverse(transform.rotation) * _trackRotations[i].transform.rotation;
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
