using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour
{
    World world;
    TextMeshProUGUI text;

    float frameRate;
    float timer;

    int halfWorldSizeInVoxels;
    int halfWorldSizeInChunks;

    private void Start()
    {
        world = GameObject.Find("World").GetComponent<World>();
        text = GetComponent<TextMeshProUGUI>();

        halfWorldSizeInVoxels = VoxelData.WorldSizeInVoxels / 2;
        halfWorldSizeInChunks = VoxelData.WorldSizeInChunks / 2;
    }

    private void Update()
    {
        string debugText = "Minecraft 2 in Unity";
        debugText += "\n";
        debugText += frameRate + " FPS";
        debugText += "\n\n";
        debugText += "XYZ: " + (Mathf.FloorToInt(world.player.transform.position.x) - halfWorldSizeInVoxels) + " / " + Mathf.FloorToInt(world.player.transform.position.y) + " / " + (Mathf.FloorToInt(world.player.transform.position.z) - halfWorldSizeInVoxels);
        debugText += "\n";
        debugText += "Chunk: " + ( world.playerChunkCoord.x - halfWorldSizeInChunks)+ " / " + (world.playerChunkCoord.z - halfWorldSizeInChunks);


        text.text = debugText;

        if (timer > 1f)
        {
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;
        }
        else
            timer += Time.deltaTime;
    }

}
