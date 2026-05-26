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
    public float cameraCollisionRadius = 0.3f;  // 相机碰撞检测球体半径
    public LayerMask collisionLayers = -1;      // 哪些层会阻挡相机(默认全部)
    public float minDistance = 0.5f;            // 相机最近可以靠角色多近
    public float recoverySpeed = 5.0f;          // 相机恢复原位的速度

    // 内部记录变量
    private float yaw;
    private float fixedPitch;
    private float horizontalDist;
    private float verticalHeight;
    private float verticalVelocity;
    private float currentDistance;              // 当前实际距离

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();

        if (playerCamera == null) playerCamera = Camera.main.transform;

        Vector3 offset = playerCamera.position - transform.position;
        horizontalDist = new Vector3(offset.x, 0, offset.z).magnitude;
        verticalHeight = offset.y;
        currentDistance = horizontalDist;  // 初始化当前距离

        yaw = playerCamera.eulerAngles.y;
        fixedPitch = playerCamera.eulerAngles.x;
    }

    void Update()
    {
        if (RebuildModeManager.Instance != null && RebuildModeManager.Instance.isRebuildModeActive)
        {
            return;
        }
        HandleCamera();
        HandleMovement();
    }

    void HandleCamera()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;

        // --- 计算理想的相机位置 ---
        Vector3 horizontalOffset = Quaternion.Euler(0, yaw, 0) * Vector3.back * horizontalDist;
        Vector3 idealCameraPos = transform.position + horizontalOffset;
        idealCameraPos.y = transform.position.y + verticalHeight;

        // --- 相机碰撞检测 ---
        Vector3 cameraDirection = (idealCameraPos - transform.position).normalized;
        float targetDistance = horizontalDist;

        // 从角色位置向相机理想位置发射射线
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * verticalHeight;  // 从角色头部高度开始

        if (Physics.SphereCast(rayStart, cameraCollisionRadius, cameraDirection, out hit, horizontalDist, collisionLayers))
        {
            // 检测到障碍物,计算安全距离
            targetDistance = Mathf.Max(hit.distance - cameraCollisionRadius, minDistance);
        }

        // 平滑过渡当前距离
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * recoverySpeed);

        // --- 使用调整后的距离计算最终位置 ---
        Vector3 finalOffset = Quaternion.Euler(0, yaw, 0) * Vector3.back * currentDistance;
        Vector3 finalCameraPos = transform.position + finalOffset;
        finalCameraPos.y = transform.position.y + verticalHeight;

        playerCamera.position = finalCameraPos;
        playerCamera.rotation = Quaternion.Euler(fixedPitch, yaw, 0);
    }

    void HandleMovement()
    {
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

        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

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