using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrackBodyStatesInWorldSpace : MonoBehaviour
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
    public List<TrackBodyStatesInWorldSpace.Stat> Stats;

    internal List<ArticulationBody> _articulationBodies;

    // Start is called before the first frame update
    void Awake()
    {
        _articulationBodies = GetComponentsInChildren<ArticulationBody>().ToList();
        Stats = _articulationBodies
            .Select(x=> new TrackBodyStatesInWorldSpace.Stat{Name = x.name})
            .ToList();        
    }

    void FixedUpdate()
    {
        float timeDelta = Time.fixedDeltaTime;

        foreach (var rb in _articulationBodies)
        {
            Stat stat = Stats.First(x=>x.Name == rb.name);
            if (!stat.LastIsSet)
            {
                stat.LastPosition = rb.transform.position;
                stat.LastRotation = rb.transform.rotation;
            }
            stat.Position = rb.transform.position;
            stat.Rotation = rb.transform.rotation;
            stat.Velocity = rb.transform.position - stat.LastPosition;
            stat.Velocity /= timeDelta;
            stat.AngualrVelocity = DReConObservationStats.GetAngularVelocity(rb.transform.rotation, stat.LastRotation, timeDelta);
            stat.LastPosition = rb.transform.position;
            stat.LastRotation = rb.transform.rotation;
            stat.LastIsSet = true;
        }        
    }

    public void Reset()
    {
        foreach (var rb in _articulationBodies)
        {
            Stat stat = Stats.First(x=>x.Name == rb.name);
            stat.LastPosition = rb.transform.position;
            stat.LastRotation = rb.transform.rotation;
            stat.Position = rb.transform.position;
            stat.Rotation = rb.transform.rotation;
            stat.Velocity = Vector3.zero;
            stat.AngualrVelocity = Vector3.zero;
            stat.LastPosition = rb.transform.position;
            stat.LastRotation = rb.transform.rotation;
            stat.LastIsSet = true;
        }        
        
    }

    public void CopyStatesTo(GameObject target)
    {
        var targets = target.GetComponentsInChildren<ArticulationBody>().ToList();
        foreach (var stat in Stats)
        {
            var targetRb = targets.First(x=>x.name == stat.Name);
            targetRb.transform.position = stat.Position;
            targetRb.transform.rotation = stat.Rotation;
            // targetRb.velocity = stat.Velocity;
            // targetRb.angularVelocity = stat.AngualrVelocity;
        }
    }
}
