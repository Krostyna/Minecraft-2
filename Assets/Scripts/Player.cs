using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Player : MonoBehaviour
{
    public bool isGrounded;
    public bool isSprinting;

    private Transform cam;
    private World world;

    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float jumpForce = 5f;
    public float gravity = -9.8f;

    public float playerWidth = 0.15f;

    private float horizontal;
    private float vertical;
    private float mouseHorizontal;
    private float mouseVertical;
    private Vector3 velocity;
    private float verticalMomentum = 0;
    private bool jumpRequest;

    private void Start()
    {
        cam = GameObject.Find("Main Camera").transform;
        world = GameObject.Find("World").GetComponent<World>();
    }

    private void Update()
    {
        GetPlayerInputs();

        velocity = ((transform.forward * vertical) + (transform.right*horizontal)) * Time.deltaTime * walkSpeed;
        velocity += Vector3.up * gravity * Time.deltaTime;

        velocity.y = checkDownSpeed(velocity.y);

        transform.Rotate(Vector3.up * mouseHorizontal);
        cam.Rotate(Vector3.right * -mouseVertical);
        transform.Translate(velocity, Space.World);

    }

    private void GetPlayerInputs()
    {
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

    }

    private float checkDownSpeed(float downSpeed)
    {
        if(
            world.CheckForVoxel(transform.position.x - playerWidth, transform.position.y+downSpeed, transform.position.z-playerWidth)||
            world.CheckForVoxel(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth) ||
            world.CheckForVoxel(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth) ||
            world.CheckForVoxel(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)
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

    private float checkUpSpeed(float upSpeed)
    {
        if (
            world.CheckForVoxel(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth) ||
            world.CheckForVoxel(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth) ||
            world.CheckForVoxel(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth) ||
            world.CheckForVoxel(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth)
            )
        {
            return 0;
        }
        else
        {
            return upSpeed;
        }
    }

    public bool front
    {
        get
        {
            if (
                world.CheckForVoxel(transform.position.x, transform.position.y, transform.position.z + playerWidth) ||
                world.CheckForVoxel(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth)
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
                world.CheckForVoxel(transform.position.x, transform.position.y, transform.position.z + playerWidth) ||
                world.CheckForVoxel(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth)
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
                world.CheckForVoxel(transform.position.x, transform.position.y, transform.position.z + playerWidth) ||
                world.CheckForVoxel(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth)
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
                world.CheckForVoxel(transform.position.x, transform.position.y, transform.position.z + playerWidth) ||
                world.CheckForVoxel(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth)
                )
                return true;
            else
                return false;
        }
    }
}
