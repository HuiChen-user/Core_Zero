using UnityEngine;
using System.Collections;
using StarterAssets; // 需要引用 StarterAssets 命名空间

public class PlayerGrappleSystem : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("主摄像机，用于发射瞄准射线")]
    public Camera mainCamera;
    [Tooltip("引力石所在的图层")]
    public LayerMask grappleLayer;
    [Tooltip("最大瞄准距离")]
    public float maxDistance = 50f;
    [Tooltip("飞行速度")]
    public float grappleSpeed = 25f;
    [Tooltip("到达目标点后的偏移量（比如停在石头前方一点点）")]
    public Vector3 arrivalOffset = Vector3.up * 1.5f;
    [Tooltip("到达判定距离")]
    public float stoppingDistance = 1.5f;

    [Header("UI (可选)")]
    [Tooltip("瞄准时的准心UI(Image)，瞄准时显示，平时隐藏")]
    public GameObject crosshairUI;

    // 内部状态
    private bool isAiming = false;
    private bool isGrappling = false;
    private GravityStone currentTargetStone;

    // 组件引用
    private ThirdPersonController tpc;
    private CharacterController controller;

    void Start()
    {
        // 获取必要的组件
        tpc = GetComponent<ThirdPersonController>();
        controller = GetComponent<CharacterController>();

        if (mainCamera == null) mainCamera = Camera.main;
        if (crosshairUI != null) crosshairUI.SetActive(false);
    }

    void Update()
    {
        // 如果正在飞行中，禁止操作
        if (isGrappling) return;

        // --- 1. 输入处理 ---
        // 按住鼠标左键开始瞄准
        if (Input.GetMouseButtonDown(0))
        {
            StartAiming();
        }
        // 松开鼠标左键进行钩锁
        else if (Input.GetMouseButtonUp(0))
        {
            TryGrapple();
        }

        // --- 2. 瞄准逻辑 ---
        if (isAiming)
        {
            HandleAiming();
        }
    }

    void StartAiming()
    {
        isAiming = true;
        if (crosshairUI != null) crosshairUI.SetActive(true);
    }

    void StopAiming()
    {
        isAiming = false;
        if (crosshairUI != null) crosshairUI.SetActive(false);
        
        // 清除当前目标的高亮
        if (currentTargetStone != null)
        {
            currentTargetStone.SetHighlight(false);
            currentTargetStone = null;
        }
    }

    void HandleAiming()
    {
        // 从屏幕中心发射射线
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // 检测是否射中引力石图层
        if (Physics.Raycast(ray, out hit, maxDistance, grappleLayer))
        {
            GravityStone stone = hit.collider.GetComponent<GravityStone>();
            if (stone != null)
            {
                // 如果看向了新的石头
                if (currentTargetStone != stone)
                {
                    // 取消旧的高亮
                    if (currentTargetStone != null) currentTargetStone.SetHighlight(false);
                    // 设置新的目标并高亮
                    currentTargetStone = stone;
                    currentTargetStone.SetHighlight(true);
                }
                return; // 找到了目标，直接返回
            }
        }

        // 如果射线没有打中任何东西，或者打中的不是引力石，清除当前目标
        if (currentTargetStone != null)
        {
            currentTargetStone.SetHighlight(false);
            currentTargetStone = null;
        }
    }

    void TryGrapple()
    {
        // 如果有有效目标，开始飞行
        if (currentTargetStone != null)
        {
            StartCoroutine(GrappleMovementRoutine(currentTargetStone.transform.position + arrivalOffset));
        }
        
        // 无论是否成功钩锁，松开按键后都停止瞄准状态
        StopAiming();
    }
    
    /*void TryGrapple()
    {
        if (currentTargetStone != null)
        {
            // --- 自动计算表面停靠点 ---
            // 获取石头碰撞体
            Collider stoneCol = currentTargetStone.GetComponent<Collider>();
            // 找到石头表面离玩家最近的点
            Vector3 closestPoint = stoneCol.ClosestPoint(transform.position);
            // 往外挪一点点（比如1米），防止贴太死穿模
            Vector3 safeTarget = closestPoint + (transform.position - closestPoint).normalized * 1.0f;
            
            // 稍微抬高一点Y轴，防止脚卡进地里
            safeTarget.y += 0.5f;

            StartCoroutine(GrappleMovementRoutine(safeTarget));
        }
        
        StopAiming();
    }*/

    // 核心：飞向目标的协程
    IEnumerator GrappleMovementRoutine(Vector3 targetPosition)
    {
        isGrappling = true;

        // 1. 【关键】禁用 StarterAssets 的控制器
        // 这样我们才能手动控制 CharacterController，而不受重力和地面吸附的影响
        tpc.enabled = false;

        // 2. 循环移动角色，直到接近目标
        while (Vector3.Distance(transform.position, targetPosition) > stoppingDistance)
        {
            // 计算方向
            Vector3 direction = (targetPosition - transform.position).normalized;
            // 计算这一帧的移动向量
            Vector3 moveVector = direction * grappleSpeed * Time.deltaTime;
            
            // 使用 CharacterController 的 Move 方法进行物理移动（这比修改 transform.position 更安全）
            controller.Move(moveVector);

            // 可选：让角色面向飞行方向
            // transform.forward = Vector3.Lerp(transform.forward, direction, Time.deltaTime * 10f);

            yield return null; // 等待下一帧
        }

        // 3. 到达目标后的清理工作
        
        // 【关键】重置控制器的垂直速度，防止启用瞬间掉下去
        tpc.ResetVerticalVelocity();
        
        // 【关键】重新启用 StarterAssets 控制器
        tpc.enabled = true;
        
        isGrappling = false;
    }
}
