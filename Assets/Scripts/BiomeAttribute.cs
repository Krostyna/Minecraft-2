using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// For easy use can add more Biomes with simple click and setup (with default values)
[CreateAssetMenu(fileName ="BiomeAttributes",menuName = "Minecraft/Biome Attribute")]
public class BiomeAttribute : ScriptableObject
{
    [Header("Biome Settings")]
    public string biomeName;
    public int offset;
    public float scale;

    public int terrainHeight;
    public float terrainScale;

    public byte surfaceBlock;
    public byte subSurfaceBlock;

    [Header("Major Floras")]
    public int majorFloraIndex;
    public float majorFloraZoneScale = 1.3f;
    [Range(0.1f,1f)]
    public float majorFloraZoneTreshold = 0.6f;
    public float majorFloraPlacementScale = 15f;
    [Range(0.1f, 1f)]
    public float majorFloraPlacementTreshold = 0.8f;
    public bool placeMajorFlora = true;

    public int maxHeight = 12;
    public int minHeight = 5;

    public Lode[] lodes;
}

// Lode can be coal, diamonds or iron ores - we don't have any right now in game
[System.Serializable]
public class Lode
{
    public string nodeName;
    public byte blockID;
    public int minHeight;
    public int maxHeight;
    public float scale;
    public float treshold;
    public float noiseOffset;
}
