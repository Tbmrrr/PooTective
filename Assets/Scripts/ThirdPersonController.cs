using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class LockedPitchThirdPersonController : MonoBehaviour
{
    [Header("组件引用")]
    public Transform playerCamera;
    private CharacterController controller;

    [Header("移动设置")]
    public float moveSpeed = 6.0f;
    public float rotationSpeed = 10.0f;
    public float gravity = 20.0f;

    [Header("相机旋转设置")]
    public float mouseSensitivity = 3.0f;

    // 内部记录变量
    private float yaw;              // 只有左右旋转
    private float fixedPitch;       // 锁死的上下角度
    private float horizontalDist;   // 锁死的水平距离
    private float verticalHeight;   // 锁死的垂直高度
    private float verticalVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null) playerCamera = Camera.main.transform;

        // 【核心：记录启动时的“黄金比例”】
        Vector3 offset = playerCamera.position - transform.position;

        // 1. 记录水平面上的距离 (忽略Y轴后的长度)
        horizontalDist = new Vector3(offset.x, 0, offset.z).magnitude;

        // 2. 记录垂直高度差
        verticalHeight = offset.y;

        // 3. 记录初始角度
        yaw = playerCamera.eulerAngles.y;
        fixedPitch = playerCamera.eulerAngles.x; // 记下这个角度，以后再也不动它
    }

    void Update()
    {
        HandleCamera();
        HandleMovement();
    }

    void HandleCamera()
    {
        // 只有按下右键才允许左右转
        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // --- 计算相机的最终位置 ---
        // 1. 根据 yaw 计算出一个方向向量，并拉伸到初始水平距离
        Vector3 horizontalOffset = Quaternion.Euler(0, yaw, 0) * Vector3.back * horizontalDist;

        // 2. 最终位置 = 角色位置 + 水平偏移 + 垂直高度
        Vector3 targetCameraPos = transform.position + horizontalOffset;
        targetCameraPos.y = transform.position.y + verticalHeight;

        playerCamera.position = targetCameraPos;

        // 3. 保持初始的俯仰角和当前的偏航角
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
            // 移动方向始终参考当前的水平旋转角度 (yaw)
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + yaw;

            // 角色平滑转向
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationSpeed, 0.1f);
            transform.rotation = Quaternion.Euler(0, angle, 0);

            moveDir = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
        }

        // --- 针对 Mesh Collider 的防掉落重力逻辑 ---
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // 限制最大坠落速度，防止瞬移穿过 Mesh
        verticalVelocity = Mathf.Max(verticalVelocity, -25f);

        Vector3 finalMove = moveDir * moveSpeed;
        finalMove.y = verticalVelocity;

        controller.Move(finalMove * Time.deltaTime);
    }
}