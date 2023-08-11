using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterControllerScript : MonoBehaviour
{
    CharacterController charCon;
    float gravity = 9.87f;
    float verticalSpeed;
    public float moveSpeed = 10f;
    public float jumpForce = 10f;
    public Transform cameraHolder;
    public float mouseSensitivity = 2f;
    public float upLimit = -50;
    public float downLimit = 50;
    Vector3 playerVelocity;

    void Start()
    {
        charCon = GetComponent<CharacterController>();
    }

    void Update()
    {
        Move();
        Rotate();
        Jumping();
    }

    private void Rotate()
    {
        float horizRot = Input.GetAxis("Mouse X");
        float vertRot = Input.GetAxis("Mouse Y");

        transform.Rotate(0, horizRot * mouseSensitivity, 0);
        cameraHolder.Rotate(-vertRot * mouseSensitivity, 0, 0);

        Vector3 currentRot = cameraHolder.localEulerAngles;
        if(currentRot.x > 180)
        {
            currentRot.x -= 360;
        }
        currentRot.x = Mathf.Clamp(currentRot.x, upLimit, downLimit);
        cameraHolder.localRotation = Quaternion.Euler(currentRot);
    }

    private void Move()
    {
        float horizontalMove = Input.GetAxis("Horizontal");
        float verticalMove = Input.GetAxis("Vertical");

        if(charCon.isGrounded)
        {
            verticalSpeed = 0;
        }
        else if(!charCon.isGrounded && !Input.GetKeyDown(KeyCode.Space))
        {
            verticalSpeed -= gravity * Time.deltaTime;
        }

        Vector3 gravityMove = new Vector3(0, verticalSpeed, 0);
        Vector3 move = transform.forward * verticalMove + transform.right * horizontalMove;
        charCon.Move(moveSpeed * move * Time.deltaTime + gravityMove * Time.deltaTime + playerVelocity * Time.deltaTime);
    }

    void Jumping()
    {
        //playerVelocity.y += gravity * Time.deltaTime;

            if (charCon.isGrounded)
            {
                playerVelocity.y = 0f;
                if (Input.GetButtonDown("Jump"))
                {
                    playerVelocity.y += Mathf.Sqrt(jumpForce * gravity);
                }
            }
    }
}
