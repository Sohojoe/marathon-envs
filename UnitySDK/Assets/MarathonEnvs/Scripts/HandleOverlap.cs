using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MLAgents
{
    public class HandleOverlap : MonoBehaviour
    {
        public GameObject Parent;
        List<Transform> _transformsToIgnorCollision;
        int _numUpdates;

        /// <summary>
        /// OnCollisionEnter is called when this collider/rigidbody has begun
        /// touching another rigidbody/collider.
        /// </summary>
        /// <param name="other">The Collision data associated with this collision.</param>
        void OnCollisionEnter(Collision other)
        {
            Collider myCollider = GetComponent<Collider>();
            if (myCollider == null)
                return;
            // skip if not part of same parent
            if (_transformsToIgnorCollision == null)
                _transformsToIgnorCollision = Parent
                    .GetComponentsInChildren<HandleOverlap>()
                    .Select(x => x.transform)
                    .ToList();
            if (_transformsToIgnorCollision.Contains(other.collider.transform))
                Physics.IgnoreCollision(myCollider, other.collider);
        }

        private void Update()
        {
            if (_numUpdates > 0)
                this.enabled = false;
            _numUpdates++;
        }
    }
}
