using UnityEngine;

namespace Octrees
{
    public class DefaultOctreeGenerator : MonoBehaviour
    {
        public GameObject[] objects;
        [SerializeField] public float minNodeSize = 1f;
        public Octree ot;

        public readonly Graph waypoints = new();

        void Awake() => ot = new Octree(objects, minNodeSize, waypoints);

        void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            Gizmos.color = Color.green;
            // Visualize total Octree Bounds
            //Gizmos.DrawWireCube(ot.bounds.center, ot.bounds.size);
            ot.root.DrawNode(); // Visualize all nodes
            // ot.graph.DrawGraph(); // Visualize all connections
        }
    }
}
