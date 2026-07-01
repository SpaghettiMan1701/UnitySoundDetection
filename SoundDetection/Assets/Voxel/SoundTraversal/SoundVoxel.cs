using System.Collections.Generic;
using UnityEngine;

public class SoundVoxel
{
    public Vector3Int GridPos;
    public Vector3 WorldPos;
    public List<SoundVoxel> Neighbours = new();

    // Optional: for sound propagation later
    // public float SoundValue;
    // Debug / rendering helper
    public Bounds Bounds;

    public SoundVoxel(Vector3Int gridPos, Vector3 worldPos, float SoundVoxelSize)
    {
        GridPos = gridPos;
        WorldPos = worldPos;
        Bounds = new Bounds(worldPos, Vector3.one * SoundVoxelSize);
        Neighbours = new List<SoundVoxel>();
    }

    public bool Equals(SoundVoxel other)
    {
        if (other is null)
            return false;

        return GridPos == other.GridPos;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as SoundVoxel);
    }

    public override int GetHashCode()
    {
        return GridPos.GetHashCode();
    }

    // public void DrawSoundVoxel()
    // {
    //     Gizmos.color = Color.green;
    //     Gizmos.DrawWireCube(Bounds.center, Bounds.size);
    // }
}
