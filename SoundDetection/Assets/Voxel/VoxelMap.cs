using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelMap : MonoBehaviour
{
    [Header("Bounds")]
    [SerializeField] public bool AutoBounds = false;
    [SerializeField] public Bounds bounds;
    [Header("Voxels")]
    [SerializeField] public bool generateAtBoundsCenter = true;
    [SerializeField] public bool ShowVoxels = false;
    [SerializeField] public float VoxelSize = 1;
    Dictionary<Vector3Int, Voxel> voxelMap = new();
    Vector3 voxelOrigin;
    void Awake()
    {
        bounds.center = transform.position;

        if (AutoBounds)
            CalculateBounds();

        Bake();

        Debug.Log($"Generated {voxelMap.Count} voxels");
    }

    /*
    Calculate bounds to fit all worldspace objects.
    Since Bounds.Encapsulate() recalculates the bounds (modifying the earlier set center),
        separate logic must be applied in the flood to generate the original voxel from the transform
    */
    void CalculateBounds()
    {
        Collider[] colliders = FindObjectsByType<Collider>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var c in colliders)
        {
            bounds.Encapsulate(c.bounds);
        }
    }

    //TODO: Check if HashSet 'generated' is even necessary
    private void Flood()
    {
        if (generateAtBoundsCenter)
            GenerateOriginalVoxelBounds();
        else
            GenerateOriginalVoxelTransform();

        //Prepare generation recursion
        HashSet<Vector3Int> generated = new();
        HashSet<Vector3Int> indexed = new();
        Queue<Vector3Int> openQueue = new();

        //Index original voxel & it's neighbors
        generated.Add(WorldToGrid(voxelOrigin));
        indexed.Add(WorldToGrid(voxelOrigin));

        HashSet<Vector3Int> originalNeigbors = GetNeighbors(WorldToGrid(voxelOrigin));
        indexed.UnionWith(originalNeigbors);
        foreach (Vector3Int vint in originalNeigbors)
        {
            openQueue.Enqueue(vint);
        }

        //Start recursively generating new neighbors
        while (openQueue.Count > 0)
        {
            Vector3Int currentPos = openQueue.Dequeue();

            //Can the voxel be generated? Is it already generated?
            Vector3 worldPos = GridToWorld(currentPos);

            if (!InBounds(currentPos))
                continue;
            if (IsOccupied(worldPos))
                continue;


            Voxel newVoxel = new(currentPos, worldPos, VoxelSize);
            voxelMap.Add(currentPos, newVoxel);
            generated.Add(currentPos);

            HashSet<Vector3Int> currentNeighbors = GetNeighbors(currentPos);

            // Enqueue new neighbors that were not indexed previously
            foreach (Vector3Int vint in currentNeighbors)
            {
                if (!InBounds(currentPos))
                    continue;
                if (IsOccupied(worldPos))
                    continue;

                //Edge case where a plane on the border of 2 voxels does not return true on IsOccupied()
                //vint is not indexed as another voxel might be able to reach it.
                //For some reason this breaks initial generation
                Vector3 from = GridToWorld(currentPos);
                Vector3 to = GridToWorld(vint);

                Vector3 dir = to - from;
                float dist = VoxelSize;

                if (Physics.Raycast(from, dir.normalized, dist))
                {
                    Debug.DrawLine(from, to, Color.red);
                    continue;
                }

                if (indexed.Add(vint))
                {
                    openQueue.Enqueue(vint);
                }
            }
        }

        BuildNeighbors();
    }

    private void GenerateOriginalVoxelTransform()
    {
        //Starting position must be valid
        if (IsOccupied(transform.position))
        {
            Debug.LogError("transform.position is not valid for generation");
            return;
        }

        //Generate original voxel 
        voxelOrigin = transform.position; //Worldpos

        Vector3Int originGrid = WorldToGrid(transform.position);
        Vector3 originWorld = transform.position;

        Voxel originVoxel = new(originGrid, originWorld, VoxelSize);
        voxelMap.Add(originGrid, originVoxel);
    }

    private void GenerateOriginalVoxelBounds()
    {
        //Starting position must be valid
        if (IsOccupied(bounds.center))
        {
            Debug.LogError("Bounds.Center is not valid for generation");
            return;
        }

        //Generate original voxel 
        voxelOrigin = bounds.center; //Worldpos

        Vector3Int originGrid = Vector3Int.zero; //Grid position (0,0,0)
        Vector3 originWorld = GridToWorld(originGrid);

        Voxel originVoxel = new(originGrid, originWorld, VoxelSize);
        voxelMap.Add(originGrid, originVoxel);
    }

    Vector3 GridToWorld(Vector3Int grid)
    {
        return voxelOrigin + (Vector3)grid * VoxelSize;
    }

    Vector3Int WorldToGrid(Vector3 world)
    {
        return Vector3Int.FloorToInt((world - voxelOrigin) / VoxelSize);
    }

    HashSet<Vector3Int> GetNeighbors(Vector3Int origin)
    {
        HashSet<Vector3Int> neighbors = new();

        foreach (var dir in Directions)
        {
            Vector3Int nPos = origin + dir;
            neighbors.Add(nPos);
        }

        return neighbors;
    }

    void BuildNeighbors()
    {
        foreach (var kvp in voxelMap)
        {
            kvp.Value.Neighbours.Clear();

            Vector3Int pos = kvp.Key;
            Voxel voxel = kvp.Value;

            foreach (var dir in Directions)
            {
                Vector3Int nPos = pos + dir;

                if (voxelMap.TryGetValue(nPos, out Voxel neighbor))
                {
                    if (Physics.Raycast(pos, nPos)) //Can actually reach neighbor?
                        continue;

                    voxel.Neighbours.Add(neighbor);
                }
            }
        }
    }

    static readonly Vector3Int[] Directions =
    {
    Vector3Int.right,
    Vector3Int.left,
    Vector3Int.up,
    Vector3Int.down,
    new Vector3Int(0,0,1),
    new Vector3Int(0,0,-1)
};

    bool InBounds(Vector3Int grid)
    {
        Vector3 world = GridToWorld(grid);
        return bounds.Contains(world);
    }
    bool IsOccupied(Vector3 worldPos)
    {
        float half = VoxelSize * 0.49f;

        if (Physics.CheckBox(worldPos, Vector3.one * half, Quaternion.identity))
            return true;

        return false;
    }

    [ContextMenu("Bake Voxels")]
    void Bake()
    {
        Flood();
    }

    #region visualization
    void OnDrawGizmos()
    {
        //Visualize each Voxel, can cause editor lag for larger maps
        if (ShowVoxels)
        {
            Gizmos.color = Color.blue;
            foreach (Voxel v in voxelMap.Values)
            {
                Gizmos.DrawWireCube(v.Bounds.center, v.Bounds.size);
            }
        }

        //Debugs
        if (!Application.isPlaying)
        {
            //Visualize Total Bounds (editor)
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, bounds.size);

            return;
        }

        //Visualize Total Bounds (true / runtime)
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        //Visualize original Cube
        Gizmos.color = Color.red;
        Voxel ogv = voxelMap[Vector3Int.zero];
        Gizmos.DrawCube(ogv.Bounds.center, Vector3.one * VoxelSize);

        //Check neighbor generation on original
        // if (voxelMap.TryGetValue(Vector3Int.zero, out Voxel1 original))
        // {
        //     Gizmos.color = Color.red;
        //     foreach (Voxel1 v in original.Neighbours)
        //     {
        //         Gizmos.DrawCube(v.WorldPos, Vector3.one * VoxelSize * 0.3f);
        //     }
        // }
    }

    [ContextMenu("Visualize random neighbors")]
    public void ShowRandomNeighbors()
    {
        List<Voxel> voxels = voxelMap.Values.ToList();
        Voxel randomVoxel = voxels[UnityEngine.Random.Range(0, voxels.Count)];

        foreach (Voxel v in randomVoxel.Neighbours)
        {
            Debug.DrawLine(randomVoxel.WorldPos, v.WorldPos, Color.yellow, 10);
        }
    }
    #endregion
}