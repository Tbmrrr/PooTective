using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class LockedPitchThirdPersonController : MonoBehaviour
{
    [Header("组件引用")]
    public Transform playerCamera;
    private CharacterController controller;
    private Animator anim;

    [Header("移动设置")]
    public float moveSpeed = 6.0f;
    public float rotationSpeed = 10.0f;
    public float gravity = 20.0f;

    [Header("相机旋转设置")]
    public float mouseSensitivity = 3.0f;

    [Header("相机碰撞设置")]
    public float cameraCollisionRadius = 0.3f;
    public LayerMask collisionLayers = -1;
    public float minDistance = 0.5f;
    public float recoverySpeed = 5.0f;

    private float yaw;
    private float fixedPitch;
    private float horizontalDist;
    private float verticalHeight;
    private float verticalVelocity;
    private float currentDistance;
    private float targetCameraDistance;
    private bool lastCameraLockState;
    private bool lastSettingsLockState;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();

        // 用代码强制关闭动画自带的位移（Root Motion）
        if (anim != null)
        {
            anim.applyRootMotion = false;
        }

        if (playerCamera == null) playerCamera = Camera.main.transform;

        Vector3 offset = playerCamera.position - transform.position;
        horizontalDist = new Vector3(offset.x, 0, offset.z).magnitude;
        verticalHeight = offset.y;
        currentDistance = horizontalDist;
        targetCameraDistance = horizontalDist;
        yaw = playerCamera.eulerAngles.y;
        fixedPitch = playerCamera.eulerAngles.x;
    }

    void Update()
    {
        if (RebuildModeManager.Instance != null && RebuildModeManager.Instance.isRebuildModeActive)
        {
            return;
        }

        // 拦截设置面板
        if (UIManager.Instance != null && UIManager.Instance.IsSettingsOpen)
        {
            if (!lastSettingsLockState)
            {
                Debug.Log("[CameraLock] Settings panel open. Camera updates paused.");
                lastSettingsLockState = true;
            }

            if (anim != null) anim.SetBool("isWalking", false);
            ApplyMenuStaticGravity();
            return;
        }

        if (lastSettingsLockState)
        {
            Debug.Log("[CameraLock] Settings panel closed. Camera updates resumed.");
            lastSettingsLockState = false;
        }

        HandleCamera();
        HandleMovement();
    }

    private void ApplyMenuStaticGravity()
    {
        if (!controller.isGrounded) verticalVelocity -= gravity * Time.deltaTime;
        else verticalVelocity = -2f;
        verticalVelocity = Mathf.Max(verticalVelocity, -25f);
        controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
    }

    void HandleCamera()
    {
        bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive;
        bool isNoteOpen = NoteManager.Instance != null
            && NoteManager.Instance.notePanel != null
            && NoteManager.Instance.notePanel.activeSelf;
        bool isChoosingOption = NPCInteractable.isChoosingOption;

        bool isSearchOpen = SearchPanelManager.Instance != null
            && SearchPanelManager.Instance.searchPanel != null
            && SearchPanelManager.Instance.searchPanel.activeSelf;

        if (isDialogueActive || isNoteOpen || isChoosingOption || isSearchOpen)
        {
            if (!lastCameraLockState)
            {
                Debug.Log("[CameraLock] UI Active - Camera updates paused.");
                lastCameraLockState = true;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (lastCameraLockState)
        {
            Debug.Log("[CameraLock] Camera updates resumed.");
            lastCameraLockState = false;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;

        Quaternion camRotation = Quaternion.Euler(0, yaw, 0);

        Vector3 rayStart =
            transform.position + Vector3.up * Mathf.Max(1.6f, verticalHeight);

        Vector3 desiredCameraPos =
            transform.position
            + camRotation * Vector3.back * horizontalDist;

        desiredCameraPos.y =
            transform.position.y + verticalHeight;

        Vector3 direction =
            (desiredCameraPos - rayStart).normalized;

        float desiredDistance = horizontalDist;

        RaycastHit hit;

        if (Physics.SphereCast(
            rayStart,
            cameraCollisionRadius,
            direction,
            out hit,
            horizontalDist,
            collisionLayers,
            QueryTriggerInteraction.Ignore))
        {
            desiredDistance =
                Mathf.Max(
                    hit.distance - cameraCollisionRadius,
                    minDistance);
        }

        // 撞墙立即收缩
        if (desiredDistance < targetCameraDistance)
        {
            targetCameraDistance = desiredDistance;
        }
        // 离墙慢慢恢复
        else
        {
            targetCameraDistance = Mathf.Lerp(
                targetCameraDistance,
                desiredDistance,
                Time.deltaTime * 2f);
        }

        // 最终平滑
        currentDistance = Mathf.Lerp(
            currentDistance,
            targetCameraDistance,
            Time.deltaTime * 12f);

        Vector3 finalCameraPos =
            transform.position
            + camRotation * Vector3.back * currentDistance;

        finalCameraPos.y =
            transform.position.y + verticalHeight;

        playerCamera.position = finalCameraPos;
        playerCamera.rotation = Quaternion.Euler(
            fixedPitch,
            yaw,
            0);
    }

    void HandleMovement()
    {
        bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive;
        bool isNoteOpen = NoteManager.Instance != null && NoteManager.Instance.notePanel.activeSelf;
        bool isSearchOpen = SearchPanelManager.Instance != null
            && SearchPanelManager.Instance.searchPanel != null
            && SearchPanelManager.Instance.searchPanel.activeSelf;

        if (isDialogueActive || isNoteOpen || isSearchOpen)
        {
            if (!controller.isGrounded) verticalVelocity -= gravity * Time.deltaTime;
            else verticalVelocity = -2f;
            verticalVelocity = Mathf.Max(verticalVelocity, -25f);
            controller.Move(new Vector3(0, verticalVelocity, 0) * Time.deltaTime);
            if (anim != null) anim.SetBool("isWalking", false);
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;
        Vector3 moveDir = Vector3.zero;

        if (inputDir.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + yaw;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationSpeed, 0.1f);
            transform.rotation = Quaternion.Euler(0, angle, 0);
            moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
        }

        if (controller.isGrounded) verticalVelocity = -2f;
        else verticalVelocity -= gravity * Time.deltaTime;
        verticalVelocity = Mathf.Max(verticalVelocity, -25f);

        Vector3 finalMove = moveDir * moveSpeed;
        finalMove.y = verticalVelocity;
        controller.Move(finalMove * Time.deltaTime);

        if (anim != null)
        {
            anim.SetBool("isWalking", inputDir.magnitude >= 0.1f);
        }
    }
}