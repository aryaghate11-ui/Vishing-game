using UnityEngine;

namespace ElmanGameDevTools.PlayerSystem
{
    /// <summary>
    /// Advanced player controller with movement, camera control, crouching, and head bobbing features
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        public CharacterController controller;
        public Transform playerCamera;

        [Header("Movement Settings")]
        public float speed = 6f;
        public float runSpeed = 9f;
        public float jumpHeight = 1f;
        public float gravity = -9.81f;
        public float sensitivity = 2f;

        [Header("Key Bindings")]
        public KeyCode defaultRunKey = KeyCode.LeftShift;
        public KeyCode defaultCrouchKey = KeyCode.LeftControl;

        [Header("Crouch Settings")]
        private float crouchHeight = 1.3f;
        public float crouchSmoothTime = 0.2f;

        [Header("Camera Look Limits")]
        public float maxLookUpAngle = 90f;
        public float maxLookDownAngle = -90f;

        [Header("Head Bobbing Settings")]
        public bool enableHeadBob = true;
        public float walkBobSpeed = 14f;
        public float walkBobAmount = 0.05f;
        public float runBobSpeed = 18f;
        public float runBobAmount = 0.03f;

        [Header("Standing Detection")]
        public GameObject standingHeightMarker;
        public float standingCheckRadius = 0.2f;
        public LayerMask obstacleLayerMask = ~0;
        public float minStandingClearance = 0.01f;
        public float standCheckCooldown = 0.1f;

        // Movement state variables
        private KeyCode currentRunKey;
        private KeyCode currentCrouchKey;
        private float defaultYPos = 0;
        private float timer = 0;
        private Vector3 velocity;
        private bool isGrounded;
        private bool isCrouching;
        private float originalHeight;
        private float targetHeight;
        private bool wantsToStand = false;
        private float currentHeightVelocity;
        private float cameraHeightVelocity;
        private float xRotation = 0f;
        private float markerHeightOffset;
        private bool markerInitialized = false;
        private float lastStandCheckTime = 0f;

        // Input and camera control variables
        private bool isCrouchKeyHeld = false;
        private float cameraBaseHeight;
        private Vector3 cameraOriginalLocalPos;

        /// <summary>
        /// Initializes the player controller and sets up initial state
        /// </summary>
        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            originalHeight = controller.height;
            targetHeight = originalHeight;
            defaultYPos = playerCamera.localPosition.y;
            cameraBaseHeight = defaultYPos;
            cameraOriginalLocalPos = playerCamera.localPosition;

            InitializeMarker();

            currentRunKey = defaultRunKey;
            currentCrouchKey = defaultCrouchKey;
        }

        /// <summary>
        /// Initializes the standing height marker for crouch detection
        /// </summary>
        private void InitializeMarker()
        {
            if (standingHeightMarker != null)
            {
                markerHeightOffset = standingHeightMarker.transform.position.y - transform.position.y;
                markerInitialized = true;
                Debug.Log("Marker initialized with height offset: " + markerHeightOffset);
            }
            else
            {
                Debug.LogError("StandingHeightMarker not assigned! Please assign a marker object.");
            }
        }

        /// <summary>
        /// Main update loop handling player input and movement
        /// </summary>
        void Update()
        {
            HandleGroundCheck();
            HandleCrouching();
            HandleMovement();
            HandleControllerHeightAdjustment();
            HandleCameraControl();

            if (enableHeadBob)
            {
                HandleHeadBob();
            }
        }

        /// <summary>
        /// Checks if player is grounded and resets vertical velocity when landing
        /// </summary>
        private void HandleGroundCheck()
        {
            isGrounded = controller.isGrounded;

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
        }

        /// <summary>
        /// Handles player movement including walking, running and jumping
        /// </summary>
        private void HandleMovement()
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            UpdateMarkerPosition();

            Vector3 move = transform.right * moveX + transform.forward * moveZ;

            bool isRunning = Input.GetKey(currentRunKey) && !isCrouching && moveZ > 0.1f;
            float currentSpeed = isRunning ? runSpeed : speed;

            // Apply speed reduction when crouching and grounded
            if (isCrouching && isGrounded)
            {
                currentSpeed /= 2;
            }

            controller.Move(move * currentSpeed * Time.deltaTime);

            // Handle jumping
            if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        /// <summary>
        /// Updates the standing height marker position to follow the player
        /// </summary>
        private void UpdateMarkerPosition()
        {
            if (standingHeightMarker != null && markerInitialized)
            {
                Vector3 newMarkerPosition = new Vector3(
                    transform.position.x,
                    transform.position.y + markerHeightOffset,
                    transform.position.z
                );

                standingHeightMarker.transform.position = newMarkerPosition;
                standingHeightMarker.transform.rotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Handles crouching input and state management
        /// </summary>
        private void HandleCrouching()
        {
            // Handle crouch key press
            if (Input.GetKeyDown(currentCrouchKey))
            {
                isCrouchKeyHeld = true;

                if (!isCrouching)
                {
                    // Start crouching
                    isCrouching = true;
                    wantsToStand = false;
                    targetHeight = crouchHeight;
                }
            }

            // Handle crouch key release
            if (Input.GetKeyUp(currentCrouchKey))
            {
                isCrouchKeyHeld = false;

                // Try to stand up when crouch key is released
                if (isCrouching)
                {
                    wantsToStand = true;
                }
            }

            // Automatic standing up when possible
            if (wantsToStand && !isCrouchKeyHeld)
            {
                if (Time.time - lastStandCheckTime > standCheckCooldown)
                {
                    lastStandCheckTime = Time.time;

                    if (CanStandUp())
                    {
                        isCrouching = false;
                        targetHeight = originalHeight;
                        wantsToStand = false;
                    }
                }
            }

            // Prevent standing up when holding crouch key and obstacle above
            if (isCrouching && isCrouchKeyHeld)
            {
                wantsToStand = false;
            }
        }

        /// <summary>
        /// Smoothly adjusts the character controller height
        /// </summary>
        private void HandleControllerHeightAdjustment()
        {
            float previousHeight = controller.height;
            float newHeight = Mathf.SmoothDamp(controller.height, targetHeight, ref currentHeightVelocity, crouchSmoothTime);

            // Calculate height difference for proper positioning
            float heightDifference = newHeight - previousHeight;
            controller.height = newHeight;

            // Move controller up when height increases
            if (heightDifference > 0)
            {
                controller.Move(Vector3.up * heightDifference * 0.5f);
            }

            AdjustCameraPosition();
        }

        /// <summary>
        /// Adjusts camera position based on player height changes
        /// </summary>
        private void AdjustCameraPosition()
        {
            // Recalculate camera base height relative to current controller height
            float heightRatio = controller.height / originalHeight;
            float targetCameraHeight = cameraBaseHeight * heightRatio;

            float newCameraHeight = Mathf.SmoothDamp(
                playerCamera.localPosition.y,
                targetCameraHeight,
                ref cameraHeightVelocity,
                crouchSmoothTime
            );

            playerCamera.localPosition = new Vector3(
                playerCamera.localPosition.x,
                newCameraHeight,
                playerCamera.localPosition.z
            );
        }

        /// <summary>
        /// Handles camera rotation based on mouse input
        /// </summary>
        private void HandleCameraControl()
        {
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            transform.Rotate(0f, mouseX, 0f);

            float mouseY = Input.GetAxis("Mouse Y") * sensitivity;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, maxLookDownAngle, maxLookUpAngle);
            playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        /// <summary>
        /// Handles head bobbing effect during movement
        /// </summary>
        private void HandleHeadBob()
        {
            if (!isGrounded) return;

            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            // Recalculate base height for head bobbing
            float heightRatio = controller.height / originalHeight;
            float currentBaseHeight = cameraBaseHeight * heightRatio;

            if (Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveZ) > 0.1f)
            {
                bool isRunning = Input.GetKey(currentRunKey) && !isCrouching && moveZ > 0.1f;
                timer += Time.deltaTime * (isRunning ? runBobSpeed : walkBobSpeed);
                float bobAmount = isRunning ? runBobAmount : walkBobAmount;

                playerCamera.localPosition = new Vector3(
                    playerCamera.localPosition.x,
                    currentBaseHeight + Mathf.Sin(timer) * bobAmount,
                    playerCamera.localPosition.z
                );
            }
            else
            {
                timer = 0;
                playerCamera.localPosition = new Vector3(
                    playerCamera.localPosition.x,
                    Mathf.Lerp(playerCamera.localPosition.y, currentBaseHeight, Time.deltaTime * 8f),
                    playerCamera.localPosition.z
                );
            }
        }

        /// <summary>
        /// Checks if player can stand up by detecting obstacles above
        /// </summary>
        /// <returns>True if player can stand up, false otherwise</returns>
        private bool CanStandUp()
        {
            if (standingHeightMarker == null || !markerInitialized)
                return true; // Allow standing if marker not configured

            Collider[] hitColliders = Physics.OverlapSphere(standingHeightMarker.transform.position, standingCheckRadius, obstacleLayerMask);

            if (hitColliders.Length > 0)
            {
                foreach (Collider col in hitColliders)
                {
                    // Ignore player colliders
                    if (col.transform == transform || col.transform == standingHeightMarker.transform || col.transform.IsChildOf(transform))
                        continue;

                    Bounds bounds = col.bounds;
                    float obstacleBottom = bounds.min.y;

                    // Check if obstacle prevents standing
                    if (obstacleBottom < standingHeightMarker.transform.position.y + minStandingClearance)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Returns whether the player is currently crouching
        /// </summary>
        /// <returns>True if player is crouching</returns>
        public bool IsCrouching()
        {
            return isCrouching;
        }

        /// <summary>
        /// Draws debug gizmos in the editor for visualization
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (standingHeightMarker != null && markerInitialized)
            {
                Gizmos.color = CanStandUp() ? Color.green : Color.red;
                Gizmos.DrawWireSphere(standingHeightMarker.transform.position, standingCheckRadius);

                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, standingHeightMarker.transform.position);

                Gizmos.color = Color.yellow;
                Vector3 currentHeadPosition = transform.position + Vector3.up * controller.height;
                Gizmos.DrawWireSphere(currentHeadPosition, 0.05f);

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(standingHeightMarker.transform.position, 0.05f);
            }
        }
    }
}