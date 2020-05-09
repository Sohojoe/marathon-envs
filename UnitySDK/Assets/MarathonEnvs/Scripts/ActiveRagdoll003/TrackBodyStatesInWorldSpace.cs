using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrackBodyStatesInWorldSpace : MonoBehaviour
{
    public List<BodyPartStats> BodyPartStats;

    internal List<Rigidbody> _rigidbodyParts;

    // Start is called before the first frame update
    void Awake()
    {
        _rigidbodyParts = GetComponentsInChildren<Rigidbody>().ToList();
        BodyPartStats = _rigidbodyParts
            .Select(x=> new BodyPartStats{Name = x.name})
            .ToList();        
    }

    void FixedUpdate()
    {
        float timeDelta = Time.fixedDeltaTime;

        foreach (var rb in _rigidbodyParts)
        {
            BodyPartStats bodyPartStat = BodyPartStats.First(x=>x.Name == rb.name);
            if (!bodyPartStat.LastIsSet)
            {
                bodyPartStat.LastPosition = rb.position;
                bodyPartStat.LastRotation = rb.rotation;
            }
            bodyPartStat.Position = rb.position;
            bodyPartStat.Rotation = rb.rotation;
            bodyPartStat.Velocity = rb.position - bodyPartStat.LastPosition;
            bodyPartStat.Velocity /= timeDelta;
            bodyPartStat.AngualrVelocity = BodyStats.GetAngularVelocity(rb.rotation, bodyPartStat.LastRotation, timeDelta);
            bodyPartStat.LastPosition = rb.position;
            bodyPartStat.LastRotation = rb.rotation;
            bodyPartStat.LastIsSet = true;
        }        
    }

    public void CopyStatesTo(GameObject target)
    {
        var targetRbs = target.GetComponentsInChildren<Rigidbody>().ToList();
        foreach (var bodyPartStat in BodyPartStats)
        {
            var targetRb = targetRbs.First(x=>x.name == bodyPartStat.Name);
            targetRb.transform.position = bodyPartStat.Position;
            targetRb.transform.rotation = bodyPartStat.Rotation;
            targetRb.velocity = bodyPartStat.Velocity;
            targetRb.angularVelocity = bodyPartStat.AngualrVelocity;
        }
    }
}
