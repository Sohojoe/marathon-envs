using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ROMparser : MonoBehaviour
{
    [SerializeField]
    Animator theAnimator;

    [SerializeField]
    Transform theRoot;

    Transform[] joints;

    
    //[SerializeField]
    //Vector3[] maxRotations;

    //[SerializeField]
    //Vector3[] minRotations;



    float duration;

    [SerializeField]
    ROMinfoCollector info2store;


    // Start is called before the first frame update
    void Start()
    {

        //GameObject o = Instantiate( theReference.gameObject, new Vector3(0, 0, 0), Quaternion.identity);
        //Animator a = o.GetComponent<Animator>();
        //AnimatorStateInfo = a.GetCurrentAnimatorStateInfo();
        //a.Play();

       joints = theRoot.GetComponentsInChildren<Transform>();


        info2store.maxRotations = new Vector3[joints.Length];
        info2store.minRotations = new Vector3[joints.Length];

        info2store.jointNames = new string[joints.Length];

        for (int i = 0; i < joints.Length; i++)
        {
            info2store.jointNames[i] = joints[i].name;


        }

            AnimatorClipInfo[] info = theAnimator.GetCurrentAnimatorClipInfo(0);
        AnimationClip theClip = info[0].clip;
        duration = theClip.length;
        Debug.Log("The animation " + theClip.name + " has a duration of: " + duration);


    }

    // Update is called once per frame
    void Update()
    {

        for (int i = 0; i< joints.Length; i++) {

            if (info2store.maxRotations[i].x < joints[i].rotation.eulerAngles.x)
                info2store.maxRotations[i].x = joints[i].rotation.eulerAngles.x;
            if (info2store.maxRotations[i].y < joints[i].rotation.eulerAngles.y)
                info2store.maxRotations[i].y = joints[i].rotation.eulerAngles.y;
            if (info2store.maxRotations[i].z < joints[i].rotation.eulerAngles.z)
                info2store.maxRotations[i].z = joints[i].rotation.eulerAngles.z;


            if (info2store.minRotations[i].x > joints[i].rotation.eulerAngles.x)
                info2store.minRotations[i].x = joints[i].rotation.eulerAngles.x;
            if (info2store.minRotations[i].y > joints[i].rotation.eulerAngles.y)
                info2store.minRotations[i].y = joints[i].rotation.eulerAngles.y;
            if (info2store.minRotations[i].z > joints[i].rotation.eulerAngles.z)
                info2store.minRotations[i].z = joints[i].rotation.eulerAngles.z;


        }
        //info2store.maxRotations = maxRotations;
        //info2store.minRotations = minRotations;

        if (duration < Time.time) { 
            Debug.Log("animation played");
            //Application.Quit();
            

        }


    }

   
}
