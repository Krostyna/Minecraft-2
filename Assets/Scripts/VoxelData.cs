using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Just Data class to store basic numbers about single voxels - verticies, trinagles, uvs etc. and some data we don't user to change (static)
public static class VoxelData
{
    public const int ChunkWidth = 16;
    public const int ChunkHeight = 128;
    public const int WorldSizeInChunks = 128;

    public static int seed;
    public static string worldName;

    public static int WorldSizeInVoxels
    {
        get { return WorldSizeInChunks * ChunkWidth; }
    }

    public const int TextureAtlasSizeInBlocks = 16;
    public static float NormalizedBlockTextureSize
    {
        get { return 1f / (float)TextureAtlasSizeInBlocks; }
    }

    public static readonly Vector3[] voxelVerts = new Vector3[8]
    {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 1.0f, 0.0f),
        new Vector3(0.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 0.0f, 1.0f),
        new Vector3(1.0f, 1.0f, 1.0f),
        new Vector3(0.0f, 1.0f, 1.0f)
    };

    public static readonly Vector3[] faceChecks = new Vector3[6]
    {
       new Vector3(0.0f, 0.0f, -1.0f),
       new Vector3(0.0f, 0.0f, 1.0f),
       new Vector3(0.0f, 1.0f, 0.0f),
       new Vector3(0.0f, -1.0f, 0.0f),
       new Vector3(-1.0f, 0.0f, 0.0f),
       new Vector3(1.0f, 0.0f, 0.0f)
    };

    public static readonly int[,] voxelTris = new int[6, 4]
    {
        //Back, Front, Top, Bottom, Left, Right
        {0,3,1,2 }, //Back Face
        {5,6,4,7 }, //Front Face
        {3,7,2,6 }, //Top Face
        {1,5,0,4 }, //Bottom Face
        {4,7,0,3 }, //Left Face
        {1,2,5,6 } //Right Face
    };



    public static readonly Vector2[] voxelUvs = new Vector2[4]
    {
        new Vector2(0.0f,0.0f),
        new Vector2(0.0f,1.0f),
        new Vector2(1.0f,0.0f),
        new Vector2(1.0f,1.0f)
    };
    
}
