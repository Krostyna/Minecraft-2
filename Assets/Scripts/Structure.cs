using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Structure class right now only got Tree and Cacti but we can add anything from Buildings to Dungeons
public static class Structure
{
    // Function to add to queue block that make Flora - Nature - based on specific biomes 
    public static Queue<VoxelMod> GenerateMajorFlora(int index, Vector3 position, int minTrunkHeight, int maxTrunkHeight)
    {
        switch (index)
        {
            case 0:
                return MakeTree(position, minTrunkHeight, maxTrunkHeight);
            case 1:
                return MakeCacti(position, minTrunkHeight, maxTrunkHeight);
        }
        return new Queue<VoxelMod>();

    }

    // Function only for Trees - for making trunk from Wood and Crown from Leaves 
    public static Queue<VoxelMod> MakeTree(Vector3 position, int minTrunkHeight, int maxTrunkHeight)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();
        int height = (int)(maxTrunkHeight * Noise.Get2DPerlin(new Vector2(position.x, position.z), 250f, 3f));
        
        if(height < minTrunkHeight)
            height = minTrunkHeight;

        //Make trunk of the tree
        for(int i = 1; i < height; i++)
        {
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), 8));

        }

        //Make crown of the tree
        for (int x = -2; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int z = -2; z < 3; z++)
                {
                    queue.Enqueue(new VoxelMod(new Vector3(position.x + x, position.y + height + y, position.z + z), 11));

                }
            }
        }
        return queue;
    }

    // Function for only making Cacti - just a simple few blocks high "pole"
    public static Queue<VoxelMod> MakeCacti(Vector3 position, int minTrunkHeight, int maxTrunkHeight)
    {
        Queue<VoxelMod> queue = new Queue<VoxelMod>();
        int height = (int)(maxTrunkHeight * Noise.Get2DPerlin(new Vector2(position.x, position.z), 23456f, 2f));

        if (height < minTrunkHeight)
            height = minTrunkHeight;

        for (int i = 1; i <= height-1; i++)
        {
            queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + i, position.z), 13));
        }
        queue.Enqueue(new VoxelMod(new Vector3(position.x, position.y + height, position.z), 12));

        return queue;
    }
}
