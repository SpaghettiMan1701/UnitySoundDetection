using System.Linq;
using UnityEngine;

namespace Octrees
{
    public class MapOctreeGenerator : MonoBehaviour
    {
        [SerializeField] GameObject map;
        public GameObject[] objects;
        [SerializeField] public float minNodeSize = 1f;
        [SerializeField] public bool shownodes = true;
        public readonly Graph waypoints = new();
        public Octree ot;
        void Awake()
        {
            Collider[] colliders = FindObjectsByType<Collider>(
    FindObjectsInactive.Include,
    FindObjectsSortMode.None
);

            objects = colliders.Select(c => c.gameObject).ToArray();
            ot = new Octree(objects, minNodeSize, waypoints);
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(ot.bounds.center, ot.bounds.size);

            if(shownodes) ot.root.DrawNode(); // Visualize all nodes
            // ot.graph.DrawGraph(); // Visualize all connections
        }
    }
}
