using System.Collections.Generic;
using UnityEngine;

public class Voxel
{
    public Vector3Int GridPos;
    public Vector3 WorldPos;
    public List<Voxel> Neighbours = new();

    // Optional: for sound propagation later
    // public float SoundValue;
    // Debug / rendering helper
    public Bounds Bounds;

    public Voxel(Vector3Int gridPos, Vector3 worldPos, float voxelSize)
    {
        GridPos = gridPos;
        WorldPos = worldPos;
        Bounds = new Bounds(worldPos, Vector3.one * voxelSize);
        Neighbours = new List<Voxel>();
    }

    public bool Equals(Voxel other)
    {
        if (other is null)
            return false;

        return GridPos == other.GridPos;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as Voxel);
    }

    public override int GetHashCode()
    {
        return GridPos.GetHashCode();
    }

    // public void DrawVoxel()
    // {
    //     Gizmos.color = Color.green;
    //     Gizmos.DrawWireCube(Bounds.center, Bounds.size);
    // }
}
