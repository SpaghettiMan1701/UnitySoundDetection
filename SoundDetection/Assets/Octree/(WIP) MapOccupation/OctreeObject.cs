using UnityEngine;

namespace Octrees.Map
{
    public class OctreeObject
    {
        Bounds bounds;
        public OctreeObject(GameObject gameObject)
        {
            bounds = gameObject.GetComponent<Collider>().bounds;
        }

        public bool Intersects(Bounds otherBounds) => bounds.Intersects(otherBounds);
    }
}