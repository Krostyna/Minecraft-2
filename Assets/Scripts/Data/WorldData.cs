using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WorldData
{
    // Our world name from seed - or default one
    public string worldName = "Prototype";
    // Seed of our world that is used to Random for Generating consistently
    public int seed;

    // Our main storage of every Chunk based on his position (x,z)
    [System.NonSerialized]
    public Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();

    // List of modifiedChunks that needs to be updated
    [System.NonSerialized]
    public List<ChunkData> modifiedChunks = new List<ChunkData>();

    // If we want to Apply modifications we can add chunk to this list 
    public void AddToModifiedChunkList (ChunkData chunk)
    {
        if(!modifiedChunks.Contains(chunk))
            modifiedChunks.Add(chunk);
    }

    // Two constructors we can use to create world
    public WorldData(string _worldName, int _seed)
    {
        worldName = _worldName;
        seed = _seed;
    }

    public WorldData(WorldData wD)
    {
        worldName = wD.worldName;
        seed = wD.seed;
    }

    // Trying to request chunk - create if needed or return existing one
    public ChunkData RequestChunk (Vector2Int coord, bool create)
    {
        ChunkData c;

        lock (World.Instance.ChunkListThreadLock)
        {
            if (chunks.ContainsKey(coord))
            {
                c = chunks[coord];
            }
            else if (!create)
            {
                c = null;
            }
            else
            {
                LoadChunk(coord);
                c = chunks[coord];
            }
        }
        return c;
    }

    // Addding Chunk to our Chunks Dictionary if is not already there
    public void LoadChunk(Vector2Int coord)
    {
        if (chunks.ContainsKey(coord))
        {
            return;
        }

        ChunkData chunk = SaveSystem.LoadChunk(worldName, coord);
        if (chunk != null)
        {
            chunks.Add(coord, chunk);
            return;
        }

        chunks.Add(coord, new ChunkData(coord));
        chunks[coord].Populate();
    }

    // Flag if Voxel is in the World
    bool isVoxelInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
            return true;
        else
            return false;
    }

    // Setting voxel from postiton to specific value - grass, snow, air etc.
    public void SetVoxel(Vector3 pos, byte value)
    {
        // If the voxel is outside of the world we don't need to do anything with it
        if (!isVoxelInWorld(pos))
            return;

        // Find out the ChunkCoord value of our voxel's chunk
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        // Then reverse that to get the position of the chunk
        x *= VoxelData.ChunkWidth;
        z *= VoxelData.ChunkWidth;

        // Chucek if the chunk exists. If not create it
        ChunkData chunk = RequestChunk(new Vector2Int(x, z), true);

        // Then create a Vector3Int with the position of our voxel *within* te chunk
        Vector3Int voxel = new Vector3Int((int)(pos.x - x), (int)pos.y, (int)(pos.z - z));

        // Then set the voxel in our chunk
        chunk.map_id[voxel.x, voxel.y, voxel.z] = value;
        AddToModifiedChunkList(chunk);
    }


    public byte GetVoxel(Vector3 pos)
    {
        // If the voxel is outside of the world we don't need to do anything with it
        if (!isVoxelInWorld(pos))
            return 0;

        // Find out the ChunkCoord value of our voxel's chunk
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        // Then reverse that to get the position of the chunk
        x *= VoxelData.ChunkWidth;
        z *= VoxelData.ChunkWidth;

        // Chucek if the chunk exists. If not create it
        ChunkData chunk = RequestChunk(new Vector2Int(x, z), true);

        // Then create a Vector3Int with the position of our voxel *within* te chunk
        Vector3Int voxel = new Vector3Int((int)(pos.x - x), (int)pos.y, (int)(pos.z - z));

        // Then set the voxel in our chunk
        return chunk.map_id[voxel.x, voxel.y, voxel.z];
    }
}
