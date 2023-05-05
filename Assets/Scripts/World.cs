using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.IO;

public class World : MonoBehaviour
{
    public Settings settings;

    // Array to hold biome attribute data
    [Header("World Generation Values")]
    public BiomeAttribute[] biomes;

    // Reference to the player object
    public Transform player;
    // The player's spawn position in the world
    public Vector3 spawnPosition;

    // Materials used for rendering chunks
    public Material material;
    public Material transparentMaterial;

    // Array to hold block type data
    public BlockType[] blockTypes;

    // 2D array to hold all the chunks in the world
    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    // List of chunk coordinates that are currently active in the world
    List<ChunkCoord> activeChunks = new List<ChunkCoord>();

    // The chunk coordinates of the player's current and previous chunk
    public ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    // List of chunks that need to be created, updated and drawn
    List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    public List<Chunk> chunksToUpdate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    // Flag indicating whether modifications are currently being applied to the world
    bool applyingModifications = false;

    // Queue of modification requests to be applied to the world
    Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();

    // Flag indicating whether the player is currently in the UI
    private bool _inUI = false;

    // Reference to the debug screen object
    public GameObject debugScreen;

    // Reference to the creative inventory window object
    public GameObject creativeInventoryWindow;

    // Reference to the cursor slot object
    public GameObject cursorSlot;

    // References to threads that handles chunk updates
    Thread ChunkUpdateThread;
    public object ChunkUpdateThreadLock = new object();
    public object ChunkListThreadLock = new object();

    // Singleton instance of the World class
    private static World _instance;
    public static World Instance
    {
        get { return _instance; }
    }

    // Object containing data for the world (e.g. seed, name)
    public WorldData worldData;

    // The path to the application's data directory (your config file will be here)
    public string appPath;

    private void Awake()
    {
        // Ensure that only one instance of the World class exists in the scene
        if (_instance!=null && _instance!=this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }

        // Get the path to the application's data directory
        appPath = Application.persistentDataPath;
    }

    private void Start()
    {
        // If no world name has been set, use a default name
        if (VoxelData.worldName == null)
            VoxelData.worldName = "New World";

        worldData.worldName = VoxelData.worldName;

        Debug.Log("Generating New World using seed " + VoxelData.seed);

        // Load settings from the settings.cfg file
        string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
        settings = JsonUtility.FromJson<Settings>(jsonImport);

        // Load data or create it if they don't exists
        worldData = SaveSystem.LoadWorld(worldData.worldName);

        // Make sure the view and load distances are within the appropriate limits
        if (settings.viewDistance > VoxelData.WorldSizeInChunks / 2)
            settings.viewDistance = Mathf.FloorToInt(VoxelData.WorldSizeInChunks / 2 - 1);
        if (settings.loadDistance > VoxelData.WorldSizeInChunks / 2)
            settings.loadDistance = Mathf.FloorToInt(VoxelData.WorldSizeInChunks / 2 - 1);

        // Initialize the random number generator with the seed defined in VoxelData (generated from World Name)
        Random.InitState(VoxelData.seed);

        // If multithreading is enabled, start a new thread to handle chunk updates
        if (settings.enableThreading)
        {
            ChunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
            ChunkUpdateThread.Start();
        }

        // Set the spawn position to the center of the world and start generating the world around player
        spawnPosition = new Vector3((VoxelData.WorldSizeInChunks*VoxelData.ChunkWidth) / 2f, VoxelData.ChunkHeight, (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f);
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);

    }


    private void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        //Only update the chunks if the player has moved from the chunk they were previously on
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
            CheckViewDistance();

        // Check if we need to create more chunks - new world or we moved and create it
        if (chunksToCreate.Count > 0)
            CreateChunk();

        if (chunksToDraw.Count > 0)
        {
            chunksToDraw.Dequeue().CreateMesh();
        }

        if (!settings.enableThreading)
        {
            if (!applyingModifications)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
                UpdateChunks();
        }

        // Some default "non gameplay" inputs handlers
        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);

        if (Input.GetKeyDown(KeyCode.F1))
            SaveSystem.SaveWorld(worldData);
    }

    void LoadWorld()
    {
        for (int x = (VoxelData.WorldSizeInChunks / 2) - settings.loadDistance; x < (VoxelData.WorldSizeInChunks / 2) + settings.loadDistance; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - settings.loadDistance; z < (VoxelData.WorldSizeInChunks / 2) + settings.loadDistance; z++)
            {
                worldData.LoadChunk(new Vector2Int(x,z));
            }
        }
    }

    // Main Function to Generate World at the begging of the game
    void GenerateWorld()
    {
        for(int x = (VoxelData.WorldSizeInChunks / 2)-settings.viewDistance; x < (VoxelData.WorldSizeInChunks / 2) + settings.viewDistance; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - settings.viewDistance; z < (VoxelData.WorldSizeInChunks / 2) + settings.viewDistance; z++)
            {
                ChunkCoord newChunk = new ChunkCoord(x, z);
                chunks[x,z] = new Chunk(newChunk);
                chunksToCreate.Add(newChunk);
            }
        }

        player.position = spawnPosition;
        CheckViewDistance();
    }


    // Creating Chunk
    void CreateChunk()
    {
        ChunkCoord c = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);
        chunks[c.x,c.z].Init();
    }

    // Updating Chunk 
    void UpdateChunks()
    {
        lock (ChunkUpdateThreadLock)
        {
            chunksToUpdate[0].UpdateChunk();
            if (!activeChunks.Contains(chunksToUpdate[0].coord))
                activeChunks.Add(chunksToUpdate[0].coord);
            chunksToUpdate.RemoveAt(0);
        }
    }

    // Simple function we can call in Debug Screen to get if we are generating chunks without them being public
    public int ChunksToGenerate()
    {
        return chunksToCreate.Count;
    }

    // When we are using Threads use them to apply changes and updated them so FPS won't drop - main thread
    void ThreadedUpdate()
    {
        while(true)
        {
            if (!applyingModifications)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
                UpdateChunks();
        }
    }

    // Just to save memory/Threads we want to have this little save function to disable/abort threads
    private void OnDisable()
    {
        if (settings.enableThreading)
        {
            ChunkUpdateThread.Abort();
        }
    }


    // Appply Modification - edits of the world 
    void ApplyModifications()
    {
        applyingModifications = true;

        while(modifications.Count > 0)
        {
            Queue<VoxelMod> queue = modifications.Dequeue();

            while (queue.Count > 0)
            {
                VoxelMod v = queue.Dequeue();

                worldData.SetVoxel(v.position, v.id);
            }
        }

        applyingModifications = false;
    }

    // Getting Chunk Coord - in map of chunks - based on position from the world
    ChunkCoord GetChunkCoordFromVector3 (Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);
    }

    // Getting Chunk based on position from world
    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return chunks[x,z];
    }

    // Checking player distance and loading/unloading chunks based on that
    void CheckViewDistance()
    {
        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        activeChunks.Clear();

        // Loop through all chunks currently within view distance of the player
        for (int x = coord.x - settings.viewDistance; x < coord.x + settings.viewDistance; x++)
        {
            for (int z = coord.z - settings.viewDistance; z < coord.z + settings.viewDistance; z++)
            {
                ChunkCoord thisChunkCoord = new ChunkCoord(x,z);

                // If the current chunk is in the world...
                if (isChunkInWorld(thisChunkCoord))
                {
                    // Check if it active, if not, activate it.
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(thisChunkCoord);
                        chunksToCreate.Add(thisChunkCoord);
                    }
                    else if (!chunks[x, z].isActive)
                    {
                        chunks[x, z].isActive = true;

                    }
                    activeChunks.Add(thisChunkCoord);
                }

                // Check through previously active chunks to see if this chunk is here. If it is, remove it from the list
                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(thisChunkCoord))
                        previouslyActiveChunks.RemoveAt(i);
                }
            }
        }    

        // Deactivating chunks to save memory
        foreach(ChunkCoord c in previouslyActiveChunks)
        {
            chunks[c.x,c.z].isActive = false;
        }
    }

    // Check if voxel is solid or not - Air or not right now
    public bool CheckForVoxel(Vector3 pos)
    {
        byte voxel_id = worldData.GetVoxel(pos);

        return blockTypes[voxel_id].isSolid;
    }

    // Check if Voxel is Transparent (we need to render blocks behind it)
    public bool CheckIfVoxelTransparent(Vector3 pos)
    {
        byte voxel_id = worldData.GetVoxel(pos);

        return blockTypes[voxel_id].isTransparent;
    }

    // Flag if the player is right now in UI - creative invenory right now only
    public bool inUI
    {
        get
        {
            return _inUI;
        }
        set
        {
            _inUI = value;
            if (_inUI)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                creativeInventoryWindow.SetActive(true);
                cursorSlot.SetActive(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                creativeInventoryWindow.SetActive(false);
                cursorSlot.SetActive(false);
            }
        }
    }

    // Generating function to get voxel from our noises
    public byte GetVoxel(Vector3 pos)
    {
        int yPos = Mathf.FloorToInt(pos.y);

        /* Air or Not Air generation */

        // If Outside world, return Air.
        if (!isVoxelInWorld(pos))
            return 0;

        // If Bottom block of chunk, return Bedrock.
        if (yPos == 0)
            return 1;

        /* Calculating right biomes and heights for them */

        int solidGroundHeight = 42;

        float sumOfHeights = 0f;
        int count = 0;
        float strongestWeight = 0f;
        int strongestWeightIndex = 0;
        
        for(int i = 0; i < biomes.Length;i++)
        {
            float weight = Noise.Get2DPerlin(new Vector2(pos.x, pos.z), biomes[i].offset, biomes[i].scale);

            // Keep track of which weight is strongest.
            if(weight > strongestWeight)
            {
                strongestWeight = weight;
                strongestWeightIndex = i;
            }

            // Get the height of the terrain (for the current biome) and multiply it by its weight
            float height = biomes[i].terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biomes[i].terrainScale) * weight;

            // If the height value is greater 0 add it to the sum of heights.
            if(height > 0)
            {
                sumOfHeights += height;
                count ++;
            }
        }

        // Set biome to the one with the strongest weight
        BiomeAttribute biome = biomes[strongestWeightIndex];

        // Get the average of the heights
        sumOfHeights /= count;

        int terrainHeight = Mathf.FloorToInt(sumOfHeights + solidGroundHeight);
        int snowHeight = 78;

        /* Biome generation - top 5 overlay blocks */

        byte voxelValue = 0; // Air block

        if (yPos == terrainHeight)
        {
            if (yPos > snowHeight)
                voxelValue = 14; // Snow block
            else
            {
                voxelValue = biome.surfaceBlock; // Solid biome block
            }
        }
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
            voxelValue = biome.subSurfaceBlock; // Solid subsurface biome block
        else if (yPos > terrainHeight)
            return 0; // Air block
        else voxelValue = 2; // Stone block (underground)

        /* Underground generation - Ores or caves not set right now */

        if (voxelValue == 2)
        {
            foreach(Lode lode in biome.lodes)
            {
                if(yPos > lode.minHeight &&  yPos < lode.maxHeight)
                {
                    if(Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.treshold))
                    {
                        voxelValue = lode.blockID;
                    }
                }
            }
        }

        /* Structures, Trees and decoration Generation */
        
        if(yPos == terrainHeight && biome.placeMajorFlora)
        {
            if(Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0,biome.majorFloraZoneScale) > biome.majorFloraZoneTreshold)
            {
                if(Noise.Get2DPerlin(new Vector2(pos.x, pos.z),0,biome.majorFloraPlacementScale) >biome.majorFloraPlacementTreshold)
                {
                    modifications.Enqueue(Structure.GenerateMajorFlora(biome.majorFloraIndex,pos, biome.minHeight, biome.maxHeight));
                }
            }
        }


        return voxelValue;
        
    }

    // Flag to check if the Chunk is in the World based on Coord (e.g. we need to create it)
    bool isChunkInWorld(ChunkCoord coord)
    {
        if(coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks -1 && coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks-1)
            return true;
        else
            return false;

    }

    // Flag if single Voxel based on his position is in the World
    bool isVoxelInWorld(Vector3 pos)
    {
        if(pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
            return true;
        else 
            return false;
    }

}

// Main class for Block Types that we can easily add to our game in Editor
[System.Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;
    public bool isTransparent;
    public Sprite icon;
    
    public float timeToMine;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;


    //Back, Front, Top, Bottom, Left, Right

    public int GetTextureID(int faceIndex)
    {
        switch(faceIndex)
        {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Error in GetTextureID, invalid face index");
                return 0;
        }
    }

}

// Simple class to store information about combination of position to ID of block
public class VoxelMod
{
    public Vector3 position;
    public byte id;

    public VoxelMod()
    {
        position = new Vector3();
        id = 0;
    }

    public VoxelMod(Vector3 _position, byte _id)
    {
        position = _position;
        id = _id;
    }
}

// Settings we save/load from file and get from Player
[System.Serializable]
public class Settings
{
    [Header("Game Data")]
    public string version = "0.1";

    [Header("Performance")]
    public int loadDistance = 32;
    public int viewDistance = 32;
    public bool enableThreading = true;

    [Header("Controls")]
    [Range(0.1f,10f)]
    public float mouseSensitivity = 2.0f;
}
