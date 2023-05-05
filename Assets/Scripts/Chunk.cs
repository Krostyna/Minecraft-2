using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    // This chunk coordinates
    public ChunkCoord coord;

    // Game object that represents this chunk
    GameObject chunkObject;

    // Mesh rendered and filter for the chunk
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    // Variables for tracking vertices, triangles, materials, UVs, and normals
    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2];
    List<Vector2> uvs = new List<Vector2>();
    List<Vector3> normals = new List<Vector3>();

    // Position of the chunk in the world
    public Vector3 position;

    // Flag to determine if the chunk is active - in View distance
    private bool _isActive;

    // Chunk data for this chunk
    ChunkData chunkData;

    // Constructor
    public Chunk(ChunkCoord _coord)
    {
        coord = _coord;
    }

    // Initialize the chunk object and mesh renderer/filter
    public void Init()
    {
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = World.Instance.material;
        materials[1] = World.Instance.transparentMaterial;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(World.Instance.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
        chunkObject.name = "Chunk " + coord.x + ", " + coord.z;
        position = chunkObject.transform.position;

        chunkData = World.Instance.worldData.RequestChunk(new Vector2Int((int)position.x, (int)position.z), true);

        // Add this chunk to the list of chunks to update - locking it so we don't use it from different threads at the same time
        lock (World.Instance.ChunkUpdateThreadLock)
        {
            World.Instance.chunksToUpdate.Add(this);
        }
        
    }

    // Update the mesh data for this chunk
    public void UpdateChunk()
    {
        // Clear the old mesh data
        ClearMeshData();

        // Loop through all voxels in the chunk and update mesh data for solid voxels
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    if (World.Instance.blockTypes[chunkData.map_id[x, y, z]].isSolid)
                        UpdateMeshData(new Vector3(x, y, z));
                }
            }
        }

        // Add this chunk to the list of chunks to draw
        lock (World.Instance.chunksToDraw)
        {
            World.Instance.chunksToDraw.Enqueue(this);
        }
    }

    // Clear the mesh data for this chunk
    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        normals.Clear();
    }

    // Flag to determine if the chunk is active
    public bool isActive
    {
        get { return _isActive; }
        set { 
            _isActive = value;
            if (chunkObject != null)
                chunkObject.SetActive(value);
        }

    }

    // Check if a voxel is inside this chunk
    bool IsVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x > VoxelData.ChunkWidth - 1 || y < 0 || y > VoxelData.ChunkHeight - 1 || z < 0 || z > VoxelData.ChunkWidth - 1)
            return false;
        else
            return true;
    }

    // Function to EditVoxel to a new ID - Air to block, block to block etc.
    public void EditVoxel(Vector3 pos, byte newID)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);

        chunkData.map_id[xCheck, yCheck, zCheck] = newID;
        World.Instance.worldData.AddToModifiedChunkList(chunkData);

        lock (World.Instance.ChunkUpdateThreadLock)
        {
            World.Instance.chunksToUpdate.Insert(0, this);
            UpdateSurroundingVoxels(xCheck, yCheck, zCheck);

        }
    }

    // If we need to update other voxels - are now visible after mining for example
    void UpdateSurroundingVoxels(int x, int y,int z)
    {
        Vector3 thisVoxel = new Vector3(x,y,z);

        for (int p = 0; p < 6; p++)
        {
            Vector3 currentVoxel = thisVoxel + VoxelData.faceChecks[p];

            if (!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
            {
                World.Instance.chunksToUpdate.Insert(0, World.Instance.GetChunkFromVector3(currentVoxel + position));
            }
            
            
        }
    }

    // Checking if Voxel is Transparent
    bool CheckVoxel(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (!IsVoxelInChunk(x, y, z))
            return World.Instance.CheckIfVoxelTransparent(pos + position);

        return World.Instance.blockTypes[chunkData.map_id[x, y, z]].isTransparent;
    }

    // Getting byte - block ID from global position of the block/voxel
    public byte GetVoxelFromGlobalVector3(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);

        return chunkData.map_id[xCheck,yCheck,zCheck];
    }

    // Update function to get new mesh data from postion
    void UpdateMeshData(Vector3 pos)
    {
        byte blockID = chunkData.map_id[(int)pos.x, (int)pos.y, (int)pos.z];
        bool isTransparent = World.Instance.blockTypes[blockID].isTransparent;

        for (int p = 0; p < 6; p++)
        {
            if (CheckVoxel(pos + VoxelData.faceChecks[p]))
            {
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

                for (int i = 0; i < 4; i++)
                    normals.Add(VoxelData.faceChecks[p]);

                AddTexture(World.Instance.blockTypes[blockID].GetTextureID(p));

                if (!isTransparent)
                {
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);
                }
                else
                {
                    transparentTriangles.Add(vertexIndex);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 3);
                }

                vertexIndex += 4;

            }
        }

    }

    // Creating mesh based on our verticies, trinagles, uvs
    public void CreateMesh()
    {
        Mesh mesh = new Mesh()
        {
            vertices = vertices.ToArray(),
            subMeshCount = 2,
            uv = uvs.ToArray()
        };
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);

        mesh.normals= normals.ToArray();
        meshFilter.mesh = mesh;
    }

    // Adding texture based on TextureId - posititon in Texture map
    void AddTexture(int textureID)
    {
        float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));
    }
}

// Chunk Coordinates that is based only on X and Z and not his Y - compare to other chunks 
public class ChunkCoord
{
    public int x;
    public int z;

    public ChunkCoord()
    {
        x = 0;
        z = 0;
    }

    public ChunkCoord (int _x, int _z)
    {
        x = _x;
        z = _z;
    }

    public ChunkCoord(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int zCheck = Mathf.FloorToInt(pos.z);

        x = xCheck/VoxelData.ChunkWidth;
        z = zCheck/VoxelData.ChunkWidth;
    }

    public bool Equals(ChunkCoord other)
    {
        if(other == null) return false;
        else if(other.x == x && other.z == z) return true;
        return false;
    }

}