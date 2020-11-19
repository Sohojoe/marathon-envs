using UnityEngine;

namespace MLAgents
{
    public class HandleOverlap : MonoBehaviour
    {
        public GameObject Parent;

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
            if (!enabled)
                return;
            // skip if other does not share Parent
            HandleOverlap otherOverlap = other.gameObject.GetComponent<HandleOverlap>();
            if (otherOverlap == null)
                return;
            if (otherOverlap.Parent != Parent)
                return;
            Physics.IgnoreCollision(myCollider, other.collider);
        }
        void FixedUpdate()
        {
            enabled = false;
        }
    }
}
