using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class ScaleBodyParts : MonoBehaviour, IOnHandleModelReset
{

    public Collider[] LimbsToScale;

    public float[] LimbScale;

    float[] _capsuleHeights;
    float[] _yPositions;
    float[] _masses;
    Dictionary<string, Vector3> _configurableJointAnchors;
    Dictionary<string, Vector3> _configurableJointConnectedAnchors;

    public void OnHandleModelReset()
    {
        Scale();
    }

    private void Awake()
    {
        // get default params
        _capsuleHeights = LimbsToScale.Select(x => {
            CapsuleCollider capsuleCollider = x as CapsuleCollider;
            if (x == null)
                return 0f;
            return capsuleCollider.height;
        }).ToArray();
        _yPositions = LimbsToScale.Select(x => x.transform.position.y).ToArray();
        _masses = LimbsToScale.Select(x => x.GetComponent<Rigidbody>().mass).ToArray();
        _configurableJointAnchors = GetComponentsInChildren<ConfigurableJoint>()
            .ToDictionary(x=>x.name, x=>x.anchor);
        _configurableJointConnectedAnchors = GetComponentsInChildren<ConfigurableJoint>()
            .ToDictionary(x => x.name, x => x.connectedAnchor);

    }

    void Scale()
    {
        for (var i=0; i<LimbsToScale.Length; i++)
        {
            var limbCollider = LimbsToScale[i];
            var obj = limbCollider.gameObject;
            Rigidbody rigidbody = obj.GetComponent<Rigidbody>();
            ConfigurableJoint configurableJoint = obj.GetComponent<ConfigurableJoint>();
            ProceduralCapsule proceduralCapsule = obj.GetComponent<ProceduralCapsule>();
            var scale = LimbScale[i];

            float height = _capsuleHeights[i] * scale;
            float yOffset = (_capsuleHeights[i] - height) / 2;
            float mass = _masses[i] * scale;
            CapsuleCollider capsuleCollider = limbCollider as CapsuleCollider;
            if (limbCollider != null)
            {
                capsuleCollider.height = height;
                rigidbody.mass = mass;
                Vector3 v = ColliderDirectionToVector(capsuleCollider.direction);
                obj.transform.position += v * yOffset;
                if (configurableJoint != null)
                {
                    var anchor = _configurableJointAnchors[configurableJoint.name];
                    anchor += v * -yOffset;
                    configurableJoint.anchor = anchor;
                }
                var children = obj.GetComponentsInChildren<Transform>()
                    .Where(x => x.parent == obj.transform);
                foreach (var childTransform in children)
                {

                    var localV = childTransform.InverseTransformDirection(v);
                    ConfigurableJoint childConfigurableJoint = childTransform.GetComponent<ConfigurableJoint>();
                    if (childConfigurableJoint != null)
                    {
                        childConfigurableJoint.autoConfigureConnectedAnchor = false;
                        var connectedAnchor = _configurableJointConnectedAnchors[childConfigurableJoint.name];
                        connectedAnchor += v * yOffset;
                        childConfigurableJoint.connectedAnchor = connectedAnchor;
                    }
                }
                if (proceduralCapsule != null)
                {
                    proceduralCapsule.height = capsuleCollider.height * .90f;
                    proceduralCapsule.radius = capsuleCollider.radius * .90f;
                    proceduralCapsule.CreateMesh();
                }

            }
        }
    }
    Vector3 ColliderDirectionToVector(int direction)
    {
        switch (direction)
        {
            case 0:
                return new Vector3(1f, 0, 0);
            case 1:
                return new Vector3(0, 1f, 0);
            case 2:
                return new Vector3(0, 0, 1f);
            default:
                throw new System.ArgumentException();
        }
    }
}