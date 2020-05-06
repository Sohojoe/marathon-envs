using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAgents;
using UnityEngine;

[System.Serializable]
public class BodyPartDistance
{
    public string Name;
    public Vector3 PositionDistance;
    public Vector3 VelocityDistance;
    public Vector3 RotationDistance;
    public Vector3 AngualrVelocityDistance;
    public Vector3 LastPositionSource;
    public Vector3 LastPositionTarget;
    public Quaternion LastRotationSource;
    public Quaternion LastRotationTarget;
    public bool LastIsSet;

}

public class RagDollController : MonoBehaviour 
{
    ControllerAnimator _controllerAnimator;
    List<Rigidbody> _animatorBodyParts;
    List<Rigidbody> _bodyParts;
    SpawnableEnv _spawnableEnv;
    public List<BodyPartDistance> BodyPartDistances;

    // center of mass
    Vector3 _animatorCenterOfMass;
    Vector3 _ragdollCenterOfMass;
    bool _comLastIsSet;
    public Vector3 AnimatorCenterOfMassVelocity;
    public Vector3 RagdollCenterOfMassVelocity;



    public bool debugCopyAnimator;

	void Awake()
    {
        _spawnableEnv = GetComponentInParent<SpawnableEnv>();
        _controllerAnimator = _spawnableEnv.GetComponentInChildren<ControllerAnimator>();
        _animatorBodyParts = _controllerAnimator.GetComponentsInChildren<Rigidbody>().ToList();
        _bodyParts = GetComponentsInChildren<Rigidbody>().ToList();
        BodyPartDistances = _bodyParts.Select(x=>new BodyPartDistance{Name = x.name}).ToList();
    }
    void FixedUpdate()
    {
        if (debugCopyAnimator)
        {
            debugCopyAnimator = false;
            CopyAnimator();
        }
        UpdateDistances();
        UpdateCenterOfMasses();
    }
    void CopyAnimator()
    {
        foreach (var bodyPart in _bodyParts)
        {
            Rigidbody target = _animatorBodyParts.First(x=>x.name == bodyPart.name);
            bodyPart.position = target.position;
            bodyPart.rotation = target.rotation;
            bodyPart.velocity = target.velocity;
            bodyPart.angularVelocity = target.angularVelocity;
        }
        BodyPartDistances = _bodyParts.Select(x=>new BodyPartDistance{Name = x.name}).ToList();
        _comLastIsSet = false;
    }
    void UpdateDistances()
    {
        foreach (var distance in BodyPartDistances)
        {
            Rigidbody source = _bodyParts.First(x=>x.name == distance.Name);
            Rigidbody target = _animatorBodyParts.First(x=>x.name == distance.Name);
            if (!distance.LastIsSet)
            {
                distance.LastPositionSource = source.position;
                distance.LastRotationSource = source.rotation;
                distance.LastPositionTarget = target.position;
                distance.LastRotationTarget = target.rotation;
            }
            Vector3 sourceVelocity = source.position - distance.LastPositionSource;
            Vector3 targetVelocity = target.position - distance.LastPositionTarget;
            distance.PositionDistance = source.position - target.position;
            distance.VelocityDistance = sourceVelocity - targetVelocity;
            // distance.RotationDistance = source.rotation * Quaternion.Inverse(target.rotation);
            distance.RotationDistance = GetAngularVelocity(source.rotation, target.rotation);
            var sourceAngularVelocity = GetAngularVelocity(distance.LastRotationSource, source.rotation);
            var targetAngularVelocity = GetAngularVelocity(distance.LastRotationTarget, target.rotation);
            distance.AngualrVelocityDistance = sourceAngularVelocity-targetAngularVelocity;
            distance.LastIsSet = true;
            distance.LastPositionSource = source.position;
            distance.LastRotationSource = source.rotation;
            distance.LastPositionTarget = target.position;
            distance.LastRotationTarget = target.rotation;
        }
    }
    void UpdateCenterOfMasses()
    {
        Vector3 newAnimatorCOM = GetCenterOfMass(_animatorBodyParts);
        Vector3 newRagdollCOM = GetCenterOfMass(_bodyParts);
        if (!_comLastIsSet)
        {
            _animatorCenterOfMass = newAnimatorCOM;
            _ragdollCenterOfMass = newRagdollCOM;
        }
        AnimatorCenterOfMassVelocity = newAnimatorCOM-_animatorCenterOfMass;
        RagdollCenterOfMassVelocity = newRagdollCOM-_ragdollCenterOfMass;
        AnimatorCenterOfMassVelocity *= Time.fixedDeltaTime;
        RagdollCenterOfMassVelocity *= Time.fixedDeltaTime;
        _animatorCenterOfMass = newAnimatorCOM;
        _ragdollCenterOfMass = newRagdollCOM;
        _comLastIsSet = true;
    }

    Vector3 GetAngularVelocity(Quaternion foreLastFrameRotation, Quaternion lastFrameRotation)
    {
        var q = lastFrameRotation * Quaternion.Inverse(foreLastFrameRotation);
        // no rotation?
        // You may want to increase this closer to 1 if you want to handle very small rotations.
        // Beware, if it is too close to one your answer will be Nan
        // if(Mathf.Abs(q.w) > 1023.5f / 1024.0f)
            // return new Vector3(0,0,0);
        float gain;
        // handle negatives, we could just flip it but this is faster
        if(q.w < 0.0f)
        {
            var angle = Mathf.Acos(-q.w);
            gain = -2.0f * angle / (Mathf.Sin(angle)*Time.fixedDeltaTime);
        }
        else
        {
            var angle = Mathf.Acos(q.w);
            gain = 2.0f * angle / (Mathf.Sin(angle)*Time.fixedDeltaTime);
        }
        var result = new Vector3(q.x * gain,q.y * gain,q.z * gain);
        var safeResult = new Vector3(
            float.IsNaN(result.x) ? 0f : result.x,
            float.IsNaN(result.y) ? 0f : result.y,
            float.IsNaN(result.z) ? 0f : result.z
        );
        return safeResult;
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
    
}
