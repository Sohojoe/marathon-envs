using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "ROMinfo", menuName = "Parser/CreateROMinfo")]
public class ROMinfoCollector : ScriptableObject
{
    // Start is called before the first frame update
    public Vector3[] maxRotations;

    public Vector3[] minRotations;

    public string[] jointNames;

}
