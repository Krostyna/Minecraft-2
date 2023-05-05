using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkData
{
    // The global position of the chunk. ie (16,16) NOT (1, 1). We want to be able to
    // access it as a Vector2Int, but Vector2Int's are not serialized so we won't  be able
    // to save them. So we'll store them as ints.
    int x;
    int y;
    public Vector2Int position
    {
        get
        {
            return new Vector2Int(x, y);
        }
        set
        {
            x = value.x;
            y = value.y;
        }
    }

    // Map - 3D array to store information of every ID of every voxel in One chunk
    [HideInInspector] // Displaying lots of data in inspector slows it down even more so hide this one
    public byte[,,] map_id = new byte[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];


    // Two constructors we can use to create new ChunkData
    public ChunkData(Vector2Int pos)
    {
        position = pos;
    }
    public ChunkData(int _x, int _y)
    {
        x = _x;
        y = _y;
    }

   // Function to Populate or Chunk with Voxels from our predifined values/IDs of each blocks
    public void Populate()
    {
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    map_id[x, y, z] = World.Instance.GetVoxel(new Vector3(x + position.x, y, z + position.y));

                }
            }
        }
        // Adding it to modfied list Update it 
        World.Instance.worldData.AddToModifiedChunkList(this);
    }


}
