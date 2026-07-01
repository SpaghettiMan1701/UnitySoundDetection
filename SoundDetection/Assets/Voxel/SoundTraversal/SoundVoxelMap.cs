using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SoundVoxelMap : MonoBehaviour
{
    [Header("Bounds")]
    [SerializeField] public bool AutoBounds = false;
    [SerializeField] public Bounds bounds;
    [Header("SoundVoxels")]
    [SerializeField] public bool generateAtBoundsCenter = true;
    [SerializeField] public bool ShowSoundVoxels = false;
    [SerializeField] public float SoundVoxelSize = 1;
    Dictionary<Vector3Int, SoundVoxel> soundVoxelMap = new();
    Vector3 SoundVoxelOrigin;
    void Awake()
    {
        bounds.center = transform.position;

        if (AutoBounds)
            CalculateBounds();

        Bake();

        Debug.Log($"Generated {soundVoxelMap.Count} SoundVoxels");
    }

    /*
    Calculate bounds to fit all worldspace objects.
    Since Bounds.Encapsulate() recalculates the bounds (modifying the earlier set center),
        separate logic must be applied in the flood to generate the original SoundVoxel from the transform
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
            GenerateOriginalSoundVoxelBounds();
        else
            GenerateOriginalSoundVoxelTransform();

        //Prepare generation recursion
        HashSet<Vector3Int> generated = new();
        HashSet<Vector3Int> indexed = new();
        Queue<Vector3Int> openQueue = new();

        //Index original SoundVoxel & it's neighbors
        generated.Add(WorldToGrid(SoundVoxelOrigin));
        indexed.Add(WorldToGrid(SoundVoxelOrigin));

        HashSet<Vector3Int> originalNeigbors = GetNeighbors(WorldToGrid(SoundVoxelOrigin));
        indexed.UnionWith(originalNeigbors);
        foreach (Vector3Int vint in originalNeigbors)
        {
            openQueue.Enqueue(vint);
        }

        //Start recursively generating new neighbors
        while (openQueue.Count > 0)
        {
            Vector3Int currentPos = openQueue.Dequeue();

            //Can the SoundVoxel be generated? Is it already generated?
            Vector3 worldPos = GridToWorld(currentPos);

            if (!InBounds(currentPos))
                continue;
            if (IsOccupied(worldPos))
                continue;


            SoundVoxel newSoundVoxel = new(currentPos, worldPos, SoundVoxelSize);
            soundVoxelMap.Add(currentPos, newSoundVoxel);
            generated.Add(currentPos);

            HashSet<Vector3Int> currentNeighbors = GetNeighbors(currentPos);

            // Enqueue new neighbors that were not indexed previously
            foreach (Vector3Int vint in currentNeighbors)
            {
                if (!InBounds(currentPos))
                    continue;
                if (IsOccupied(worldPos))
                    continue;

                //Edge case where a plane on the border of 2 SoundVoxels does not return true on IsOccupied()
                //vint is not indexed as another SoundVoxel might be able to reach it.
                //For some reason this breaks initial generation
                Vector3 from = GridToWorld(currentPos);
                Vector3 to = GridToWorld(vint);

                Vector3 dir = to - from;
                float dist = SoundVoxelSize;

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

    private void GenerateOriginalSoundVoxelTransform()
    {
        //Starting position must be valid
        if (IsOccupied(transform.position))
        {
            Debug.LogError("transform.position is not valid for generation");
            return;
        }

        //Generate original SoundVoxel 
        SoundVoxelOrigin = transform.position; //Worldpos

        Vector3Int originGrid = WorldToGrid(transform.position);
        Vector3 originWorld = transform.position;

        SoundVoxel originSoundVoxel = new(originGrid, originWorld, SoundVoxelSize);
        soundVoxelMap.Add(originGrid, originSoundVoxel);
    }

    private void GenerateOriginalSoundVoxelBounds()
    {
        //Starting position must be valid
        if (IsOccupied(bounds.center))
        {
            Debug.LogError("Bounds.Center is not valid for generation");
            return;
        }

        //Generate original SoundVoxel 
        SoundVoxelOrigin = bounds.center; //Worldpos

        Vector3Int originGrid = Vector3Int.zero; //Grid position (0,0,0)
        Vector3 originWorld = GridToWorld(originGrid);

        SoundVoxel originSoundVoxel = new(originGrid, originWorld, SoundVoxelSize);
        soundVoxelMap.Add(originGrid, originSoundVoxel);
    }

    Vector3 GridToWorld(Vector3Int grid)
    {
        return SoundVoxelOrigin + (Vector3)grid * SoundVoxelSize;
    }

    Vector3Int WorldToGrid(Vector3 world)
    {
        return Vector3Int.FloorToInt((world - SoundVoxelOrigin) / SoundVoxelSize);
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
        foreach (var kvp in soundVoxelMap)
        {
            kvp.Value.Neighbours.Clear();

            Vector3Int pos = kvp.Key;
            SoundVoxel SoundVoxel = kvp.Value;

            foreach (var dir in Directions)
            {
                Vector3Int nPos = pos + dir;

                if (soundVoxelMap.TryGetValue(nPos, out SoundVoxel neighbor))
                {
                    if (Physics.Raycast(pos, nPos)) //Can actually reach neighbor?
                        continue;

                    SoundVoxel.Neighbours.Add(neighbor);
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
        float half = SoundVoxelSize * 0.49f;

        if (Physics.CheckBox(worldPos, Vector3.one * half, Quaternion.identity))
            return true;

        return false;
    }

    [ContextMenu("Bake SoundVoxels")]
    void Bake()
    {
        Flood();
    }

    #region visualization
    void OnDrawGizmos()
    {
        //Visualize each SoundVoxel, can cause editor lag for larger maps
        if (ShowSoundVoxels)
        {
            Gizmos.color = Color.blue;
            foreach (SoundVoxel v in soundVoxelMap.Values)
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
        SoundVoxel ogv = soundVoxelMap[Vector3Int.zero];
        Gizmos.DrawCube(ogv.Bounds.center, Vector3.one * SoundVoxelSize);

        //Check neighbor generation on original
        // if (soundVoxelMap.TryGetValue(Vector3Int.zero, out SoundVoxel1 original))
        // {
        //     Gizmos.color = Color.red;
        //     foreach (SoundVoxel1 v in original.Neighbours)
        //     {
        //         Gizmos.DrawCube(v.WorldPos, Vector3.one * SoundVoxelSize * 0.3f);
        //     }
        // }
    }

    [ContextMenu("Visualize random neighbors")]
    public void ShowRandomNeighbors()
    {
        List<SoundVoxel> SoundVoxels = soundVoxelMap.Values.ToList();
        SoundVoxel randomSoundVoxel = SoundVoxels[UnityEngine.Random.Range(0, SoundVoxels.Count)];

        foreach (SoundVoxel v in randomSoundVoxel.Neighbours)
        {
            Debug.DrawLine(randomSoundVoxel.WorldPos, v.WorldPos, Color.yellow, 10);
        }
    }
    #endregion
}