using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    // Flags for our movenent detection
    public bool isGrounded;
    public bool isSprinting;

    // References for our main and only camera and our World 
    private Transform cam;
    private World world;

    // Default setting we use for movement and phycisc of player
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float jumpForce = 5f;
    public float gravity = -9.8f;

    // Settings we can adjust for our player avatar - can change based on morphing etc.
    public float playerWidth = 0.25f;
    public float playerHeight = 2f;

    // Basic inputs for our player
    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;
    private Vector3 velocity;
    private float verticalMomentum = 0;
    private bool jumpRequest;

    // Helping values for our custom raycast
    public Transform highlightBlock;
    public Transform placeBlock;
    public float checkIncrement = 0.1f;
    public float reach = 8f;

    // Toolbar UI we see on bottom of creen in game
    public Toolbar toolbar;

    // If we started mining how long it will take and if we mined blocked
    private float startedMiningTime = 0f;
    private byte minedVoxel;
    private bool miningDone;

    // Loading bar for mining progress
    public Slider miningSlider;

    private void Start()
    {
        cam = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<World>();

        world.inUI = false;
    }

    // Fixed Update only compute movement and jumping, Camera is in normal update for Higher FPS monitors etc.
    private void FixedUpdate()
    {
        if (!world.inUI)
        {
            CalculateVelocity();
            if (jumpRequest)
                Jump();

            transform.Translate(velocity, Space.World);
        }
    }

    private void Update()
    {
        // Opening Creative Inventory or closing it
        if(Input.GetKeyDown(KeyCode.I))
        {
            world.inUI = !world.inUI;
        }
        
        // If we are not in Creative Inventory we can move and interact with world
        if (!world.inUI)
        {
            GetPlayerInputs();
            placeCursorBlocks();

            transform.Rotate(Vector3.up * mouseHorizontal * world.settings.mouseSensitivity);
            cam.Rotate(Vector3.right * -mouseVertical * world.settings.mouseSensitivity);
        }
        else
        {
            // We are in UI (creative inventory) and also pressed escape
            if(Input.GetKeyDown(KeyCode.Escape))
            {
                world.inUI = !world.inUI;
            }
        }
    }

    // Function to request jump - adding force to upwards movement
    void Jump()
    {
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }

    // Calculating movement of player based on his inputs
    private void CalculateVelocity()
    {
        // Affect vertical momentum with gravity
        if (verticalMomentum > gravity)
            verticalMomentum += Time.fixedDeltaTime * gravity;

        // if we're sprinting, use the sprint multiplier
        if (isSprinting)
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)).normalized * Time.fixedDeltaTime * sprintSpeed;
        else
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)).normalized * Time.fixedDeltaTime * walkSpeed;

        // Apply vertical momentum (falling, jumping)
        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        if ((velocity.z > 0 && front) || (velocity.z < 0 && back))
            velocity.z = 0;
        if ((velocity.x > 0 && right) || (velocity.x < 0 && left))
            velocity.x = 0;

        if (velocity.y < 0)
            velocity.y = checkDownSpeed(velocity.y);
        else if(velocity.y >0)
            velocity.y = checkUpSpeed(velocity.y);

    }

    // Main function for In Game inputs to control characters actions
    private void GetPlayerInputs()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SaveSystem.SaveWorld(World.Instance.worldData);

            //If we are running in a standalone build of the game
            #if UNITY_STANDALONE
            //Quit the application
            Application.Quit();
            #endif

            //If we are running in the editor
            #if UNITY_EDITOR
            //Stop playing the scene
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        // Mouse inputs from Input Manager settings
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        mouseHorizontal = Input.GetAxis("Mouse X");
        mouseVertical = Input.GetAxis("Mouse Y");

        if (Input.GetButtonDown("Sprint"))
            isSprinting = true;
        if(Input.GetButtonUp("Sprint"))
            isSprinting = false;

        if (isGrounded && Input.GetButtonDown("Jump"))
            jumpRequest = true;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (highlightBlock.gameObject.activeSelf)
        {
            // Mine block
            if (Input.GetMouseButtonDown(0))
            {
                startedMiningTime = Time.time;
                minedVoxel = world.GetChunkFromVector3(highlightBlock.position).GetVoxelFromGlobalVector3(highlightBlock.position);
                miningDone = false;
                miningSlider.gameObject.SetActive(true);
                miningSlider.value = 0;

            }
            // Mining block or multiple block in chain
            if (Input.GetMouseButton(0))
            {
                // We started mining different type of block without releasing mouse button
                if(minedVoxel != world.GetChunkFromVector3(highlightBlock.position).GetVoxelFromGlobalVector3(highlightBlock.position))
                {
                    minedVoxel = world.GetChunkFromVector3(highlightBlock.position).GetVoxelFromGlobalVector3(highlightBlock.position);
                    startedMiningTime = Time.time;
                }

                if (miningDone)
                {
                    startedMiningTime = Time.time;
                    minedVoxel = world.GetChunkFromVector3(highlightBlock.position).GetVoxelFromGlobalVector3(highlightBlock.position);
                    miningDone = false;
                    miningSlider.gameObject.SetActive(true);
                    miningSlider.value = 0;
                }

                // If the mined Voxel is not "Bedrock" - can be added to properties of block, but we only have one
                if (minedVoxel != 1)
                {
                    if (startedMiningTime + world.blockTypes[minedVoxel].timeToMine <= Time.time && !miningDone)
                    {
                        world.GetChunkFromVector3(highlightBlock.position).EditVoxel(highlightBlock.position, 0);
                        miningDone = true;
                        miningSlider.gameObject.SetActive(false);

                        bool added = false;
                        UIItemSlot emptySlot = null;
                        // Check Toolbar if we have item alreadz to stack it
                        foreach (UIItemSlot slot in toolbar.slots)
                        {
                            if (slot.itemSlot != null && slot.itemSlot.stack != null)
                            {
                                // We found same item in toolbar and add 1 to it
                                if (slot.itemSlot.stack.id == minedVoxel)
                                {
                                    slot.itemSlot.Get(1);
                                    added = true;
                                    break;
                                }
                            }
                            else if (emptySlot == null)
                            {
                                // We save the first emptz slot in case we didn't found the item stack
                                emptySlot = slot;
                            }

                        }

                        // The item is not on the toolbar or it is full
                        if (added == false)
                        {
                            // We have some empty slot
                            if (emptySlot != null)
                            {
                                ItemStack stack = new ItemStack(minedVoxel, 1);
                                ItemSlot slot = new ItemSlot(emptySlot, stack);
                            }
                        }
                    }
                    else
                    {
                        miningSlider.value = (Time.time - startedMiningTime) / world.blockTypes[minedVoxel].timeToMine;
                    }
                }
            }

            // We stopped mining
            if (Input.GetMouseButtonUp(0))
            {
                miningDone = true;
                miningSlider.gameObject.SetActive(false);
            }

            // Place block
            if (Input.GetMouseButtonDown(1))
            {
                if (transform.position.y < VoxelData.ChunkHeight)
                {
                    if (toolbar.slots[toolbar.slotIndex].HasItem)
                    {
                        world.GetChunkFromVector3(placeBlock.position).EditVoxel(placeBlock.position, toolbar.slots[toolbar.slotIndex].itemSlot.stack.id);
                        toolbar.slots[toolbar.slotIndex].itemSlot.Take(1);
                    }
                }
            }
        }
    }

    // Computation of "raycast" to know what block are we looking at to edit - build onto or destroy
    private void placeCursorBlocks()
    {
        float step = checkIncrement;
        Vector3 lastPos = new Vector3();

        // Custom Raycast - Unity raycast don't work with our custom Mesh
        while(step < reach)
        {
            Vector3 pos = cam.position+ (cam.forward*step);

            if(world.CheckForVoxel(pos))
            {
                highlightBlock.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                placeBlock.position = lastPos;

                highlightBlock.gameObject.SetActive(true);
                placeBlock.gameObject.SetActive(true);

                return;
            }

            lastPos = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
         
            step += checkIncrement;
        }

        highlightBlock.gameObject.SetActive(false);
        placeBlock.gameObject.SetActive(false);

    }

    // When we are checking if we hit ground after fall/jump to enable jumping again
    private float checkDownSpeed(float downSpeed)
    {
        if(
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth))
            )
        {
            isGrounded = true;
            return 0;
        }
        else
        {
            isGrounded= false;
            return downSpeed;
        }
    }

    // For our Jumping function to compute with gravitation and collisions
    private float checkUpSpeed(float upSpeed)
    {
        if (
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + playerHeight + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + playerHeight + upSpeed, transform.position.z - playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + playerHeight + upSpeed, transform.position.z + playerWidth)) ||
            world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + playerHeight + upSpeed, transform.position.z + playerWidth))
            )
        {
            return 0;
        }
        else
        {
            return upSpeed;
        }
    }
    

    // Flags for every side of player for movement function - based on his height and width
    public bool front
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth))
                )
                return true;
            else
                return false;
        }
    }
    public bool back
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
                world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth))
                )
                return true;
            else
                return false;
        }
    }
    public bool left
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z )) ||
                world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z))
                )
                return true;
            else
                return false;
        }
    }
    public bool right
    {
        get
        {
            if (
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z))
                )
                return true;
            else
                return false;
        }
    }
}
