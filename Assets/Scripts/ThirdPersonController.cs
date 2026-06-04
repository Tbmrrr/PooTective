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
    private bool lastCameraLockState;
    private bool lastSettingsLockState;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
        if (playerCamera == null) playerCamera = Camera.main.transform;

        Vector3 offset = playerCamera.position - transform.position;
        horizontalDist = new Vector3(offset.x, 0, offset.z).magnitude;
        verticalHeight = offset.y;
        currentDistance = horizontalDist;
        yaw = playerCamera.eulerAngles.y;
        fixedPitch = playerCamera.eulerAngles.x;
    }

    void Update()
    {
        if (RebuildModeManager.Instance != null && RebuildModeManager.Instance.isRebuildModeActive)
        {
            return;
        }

        // 🛑【核心新增】拦截设置面板
        // 如果 UIManager 单例存在，且设置面板处于打开状态
        if (UIManager.Instance != null && UIManager.Instance.IsSettingsOpen)
        {
            if (!lastSettingsLockState)
            {
                Debug.Log("[CameraLock] Settings panel open. Camera updates paused.");
                lastSettingsLockState = true;
            }

            // 确保停止移动动画，防止角色卡在跑步姿态
            if (anim != null) anim.SetBool("isWalking", false);

            // 维持重力（防止万一在空中打开设置面板，关闭后坠落出 Bug，或者可以像下面 HandleMovement 一样处理）
            ApplyMenuStaticGravity();

            return; // 🛑 后面所有的 HandleCamera() 和 HandleMovement() 直接被跳过！
        }

        if (lastSettingsLockState)
        {
            Debug.Log("[CameraLock] Settings panel closed. Camera updates resumed.");
            lastSettingsLockState = false;
        }

        HandleCamera();
        HandleMovement();
    }

    // 💡【新增辅助方法】用于在打开设置菜单时，给角色提供基础的重力维持，防止穿地
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

        if (isDialogueActive || isNoteOpen || isChoosingOption)
        {
            if (!lastCameraLockState)
            {
                Debug.Log($"[CameraLock] Dialogue:{isDialogueActive} Note:{isNoteOpen} Choose:{isChoosingOption}. Camera updates paused.");
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

        Vector3 horizontalOffset = Quaternion.Euler(0, yaw, 0) * Vector3.back * horizontalDist;
        Vector3 idealCameraPos = transform.position + horizontalOffset;
        idealCameraPos.y = transform.position.y + verticalHeight;

        Vector3 cameraDirection = (idealCameraPos - transform.position).normalized;
        float targetDistance = horizontalDist;

        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * verticalHeight;
        if (Physics.SphereCast(rayStart, cameraCollisionRadius, cameraDirection, out hit, horizontalDist, collisionLayers))
        {
            targetDistance = Mathf.Max(hit.distance - cameraCollisionRadius, minDistance);
        }

        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * recoverySpeed);

        Vector3 finalOffset = Quaternion.Euler(0, yaw, 0) * Vector3.back * currentDistance;
        Vector3 finalCameraPos = transform.position + finalOffset;
        finalCameraPos.y = transform.position.y + verticalHeight;

        playerCamera.position = finalCameraPos;
        playerCamera.rotation = Quaternion.Euler(fixedPitch, yaw, 0);
    }

    void HandleMovement()
    {
        // ✅ 新增：对话中或笔记本打开时，锁定移动
        bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive;
        bool isNoteOpen = NoteManager.Instance != null && NoteManager.Instance.notePanel.activeSelf;

        if (isDialogueActive || isNoteOpen)
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
