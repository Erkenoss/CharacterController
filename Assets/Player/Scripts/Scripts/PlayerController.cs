using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class PlayerController : MonoBehaviour
{
    [HideInInspector] public CharacterController controller;
    [HideInInspector] public PlayerInputManager input;
    [HideInInspector] public Animator animator;

    [Header("Cam, Cursor and Sensitivity")]
    [SerializeField] private GameObject mainCamera;
    [SerializeField] private LayerMask aimColliderLayerMask = new LayerMask();
    [SerializeField] private Transform cameraFollowTarget;
    [SerializeField] float normalSensitivity;
    [SerializeField] float aimSensitivity;
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    float sensitivity = 1f;
    float xRotation;
    float yRotation;
    bool isMouseEnabled = false;
    Vector3 aimRotation = Vector3.zero;
    bool rotateOnMove;

    [Header("Movement")]
    public float moveSpeed = 3.0f;
    public float sprintSpeed = 8.0f;
    public float speedChange = 10.0f;
    public float RotationSmoothTime = 0.12f;
    public float speedPlayer;
    public float animationLocomotor;
    public float targetRotation;
    public float rotationVelocity;
    private float verticalVelocity;

    [Header("Grounded Check")]
    public bool Grounded;
    public LayerMask GroundLayer;
    public float GroundedRadius = 0.28f;
    public float GroundOffset = 0.2f;

    [Header("Jump and Gravity")]
    private float fallTimeoutDelta;
    public float FallTimeout = 0.10f;
    public float JumpTimeoutDelta;
    public float JumpHeight = 1.2f;
    public float Gravity = -15f;
    public float JumpTimeout = 0.5f;
    private float terminalVelocity = 53.0f;

    [Header("Crouch")]
    [SerializeField] private GameObject headPos;
    public bool crouch;
    public bool canStand;


    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        input = GetComponent<PlayerInputManager>();
    }

    private void Update()
    {
        HandleCursor();
        aimRotation = MouseTarget();
        AimHandle();
        JumpAndGravity();
        GroundCheck();
        Move();
        Crouching();
    }

    // Call After the Update fonction
    private void LateUpdate()
    {
        CameraRotation();
    }


    //Handle camera movement
    private void CameraRotation()
    {
        if (isMouseEnabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            xRotation += input.look.y * sensitivity;
            yRotation += input.look.x * sensitivity;
            xRotation = Mathf.Clamp(xRotation, -30, 70);

            Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0);
            cameraFollowTarget.rotation = rotation;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    //Enabled or disabled cursor and camera
    private void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            isMouseEnabled = !isMouseEnabled;
        }
    }

    //ray on the center of the screen and rotate player when aiming
    public Vector3 MouseTarget()
    {
        Vector3 mouseWorldPosition = Vector3.zero;
        Vector2 centerPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(centerPoint);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, aimColliderLayerMask))
        {
            mouseWorldPosition = raycastHit.point;
        }
        return mouseWorldPosition;
    }
        
    //Aim cam active or not
    private void AimHandle()
    {
        if (input.aim)
        {
            aimVirtualCamera.gameObject.SetActive(true);
            SetSensitivity(aimSensitivity);
            SetRotationOnMove(false);

            Vector3 worldAimTarget = aimRotation;
            worldAimTarget.y = transform.position.y;
            Vector3 aimDirection = (worldAimTarget - transform.position).normalized;

            transform.forward = Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 20f);
        }
        else
        {
            aimVirtualCamera.gameObject.SetActive(false);
            SetSensitivity(normalSensitivity);
            SetRotationOnMove(true);
        }
    }

    //Change the sensitivity when aim cam
    public void SetSensitivity(float newSensitivity)
    {
        sensitivity = newSensitivity;
    }

    private void JumpAndGravity()
    {
        if (!crouch)
        {
            if (Grounded)
            {
                fallTimeoutDelta = FallTimeout;

                //initialize animations to false
                animator.SetBool("Jump", false);
                animator.SetBool("Falling", false);

                if (verticalVelocity < 0.0f)
                {
                    verticalVelocity = -2f;
                }

                if (input.jump && JumpTimeoutDelta <= 0.0f)
                {
                    verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    //jump true
                    animator.SetBool("Jump", true);
                }

                if (JumpTimeoutDelta >= 0.0f)
                {
                    JumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                JumpTimeoutDelta = JumpTimeout;

                if (fallTimeoutDelta >= 0.0f)
                {
                    fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    //Animation falling true
                    animator.SetBool("Falling", true);
                }

                input.jump = false;
            }
            if (verticalVelocity < terminalVelocity)
            {
                verticalVelocity += Gravity * Time.deltaTime;
            }
        }
    }

    //Check if the player is on the ground
    private void GroundCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayer, QueryTriggerInteraction.Ignore);

        animator.SetBool("Grounded", Grounded);
    }
    //Movement of the player
    private void Move()
    {        
        float targetSpeed;

        if (input.sprint)
        {
            targetSpeed = sprintSpeed;
        }
        else
        {
            targetSpeed = moveSpeed;
        }
        if (input.move == Vector2.zero)
        {
            targetSpeed = 0.0f;
        }

        float currentHorizontalSpeed = new Vector3(controller.velocity.x, 0.0f, controller.velocity.z).magnitude;
        float speedOffSet = 0.1f;
        float inputMagnitude = input.move.magnitude;

        //Define speed changes
        if (currentHorizontalSpeed < targetSpeed - speedOffSet || currentHorizontalSpeed > targetSpeed + speedOffSet)
        {
            speedPlayer = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * speedChange);
            speedPlayer = Mathf.Round(speedPlayer * 1000f) / 1000f;
        }
        else 
        {
            speedPlayer = targetSpeed;
        }

        //Set speed for animation
        animationLocomotor = Mathf.Lerp(animationLocomotor, targetSpeed, Time.deltaTime * speedChange);
        if (animationLocomotor < 0.01f)
        {
            animationLocomotor = 0f;
        }

        Vector3 inputDir = new Vector3(input.move.x, 0.0f, input.move.y).normalized;

        //Check input > or < to zero 
        if (input.move != Vector2.zero)
        {
            targetRotation = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, RotationSmoothTime);

            if(rotateOnMove)
            {
                //Rotate to the face input based on the camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }
        }

        Vector3 targetDir = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;

        //Finally move the player
        controller.Move(targetDir.normalized * (speedPlayer * Time.deltaTime) + new Vector3(0.0f, verticalVelocity, 0.0f) * Time.deltaTime);

        animator.SetFloat("Speed", animationLocomotor);
        animator.SetFloat("MotionSpeed", inputMagnitude);
    }

    public void SetRotationOnMove(bool newRotateOnMove)
    {
        rotateOnMove = newRotateOnMove;
    }

    private void Crouching()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (Physics.Raycast(headPos.transform.position, Vector3.up, 0.5f))
            {
                canStand = false;
                Debug.DrawRay(headPos.transform.position, Vector3.up, Color.green);
            }
            else
            {
                canStand = true;
            }
            if (crouch && canStand)
            {
                crouch = false;
                animator.SetBool("Crouching", false);
                controller.height = 1.6f;
                controller.center = new Vector3(0f, 0.78f, 0f);
            }
            else
            {
                crouch = true;
                animator.SetBool("Crouching", true);
                controller.height = 1f;
                controller.center = new Vector3(0f, 0.52f, 0f);
            }
        }
    }
}
