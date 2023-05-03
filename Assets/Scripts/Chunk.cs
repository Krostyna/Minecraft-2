using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public ChunkCoord coord;

    GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2];
    List<Vector2> uvs = new List<Vector2>();
    List<Vector3> normals = new List<Vector3>();

    public Vector3 position;

    private bool _isActive;

    ChunkData chunkData;

    public Chunk(ChunkCoord _coord)
    {
        coord = _coord;
    }

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

        lock(World.Instance.ChunkUpdateThreadLock)
        {
            World.Instance.chunksToUpdate.Add(this);
        }
        
    }

    
    public void UpdateChunk()
    { 

        ClearMeshData();

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

        lock (World.Instance.chunksToDraw)
        {
            World.Instance.chunksToDraw.Enqueue(this);
        }
    }

    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        normals.Clear();
    }

    public bool isActive
    {
        get { return _isActive; }
        set { 
            _isActive = value;
            if (chunkObject != null)
                chunkObject.SetActive(value);
        }

    }


    bool IsVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x > VoxelData.ChunkWidth - 1 || y < 0 || y > VoxelData.ChunkHeight - 1 || z < 0 || z > VoxelData.ChunkWidth - 1)
            return false;
        else
            return true;
    }

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

    bool CheckVoxel(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (!IsVoxelInChunk(x, y, z))
            return World.Instance.CheckIfVoxelTransparent(pos + position);

        return World.Instance.blockTypes[chunkData.map_id[x, y, z]].isTransparent;
    }

    public byte GetVoxelFromGlobalVector3(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);

        return chunkData.map_id[xCheck,yCheck,zCheck];
    }


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