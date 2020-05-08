using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BodyPartStats
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
