using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//a class used to find out the offsets that we need to apply to the method Remap2Character, to go from a Physics-based character to a kinematic one
//make sure you use the axis in local mode to find out rapidly the offset needed
public class DebugAddOffset : MonoBehaviour
{
    /* [SerializeField]
     Vector3 axis;
     [SerializeField]
     float angleDegrees;
    */

    [SerializeField]
    Vector3 eulerAngles;




    [SerializeField]
    bool applyOffset = false;




    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (applyOffset)
            // transform.localRotation =   transform.localRotation * Quaternion.AngleAxis(angleDegrees, axis);
            transform.localRotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z) * transform.localRotation;
    }
}
