using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(LineRenderer))]
public class ExpandingRing : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("圆环扩大的速度")]
    public float expansionSpeed = 5f;
    
    [Tooltip("最大半径")]
    public float maxRadius = 10f;
    
    [Tooltip("圆环的精细度（点越多越圆）")]
    public int segments = 50;

    [Tooltip("物体标签Layer")]
    public LayerMask targetLayer;
    
    [Header("Visuals")]
    [Tooltip("圆环初始颜色")]
    public Color normalColor = Color.white;
    [Tooltip("扩波后的圆环颜色")]
    public Color amplifiedColor = Color.yellow;

    private LineRenderer lineRenderer;
    private float currentRadius = 0f;
    private bool isAmplified = false;

    // 关键点：防止同一个物体每一帧都被触发一次
    private HashSet<GameObject> hitObjects = new HashSet<GameObject>();
    
    // 标记圆环是否已经消散
    private bool isDissipated = false;
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = segments + 1; // +1 是为了闭合圆环
        lineRenderer.useWorldSpace = false; // 使用本地坐标，跟随物体移动
        lineRenderer.loop = true;
        
        // 初始化颜色
        UpdateRingColor(normalColor);
    }
    
    private void UpdateRingColor(Color c)
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer.material == null) return;
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;
        lineRenderer.material.color = c;
    }

    /// <summary>
    /// 初始化圆环的属性（用于其他物体生成新波时的定制）
    /// </summary>
    public void InitializeRing(float newSpeed, float newRadius, Color newColor)
    {
        expansionSpeed = newSpeed;
        maxRadius = newRadius;
        UpdateRingColor(newColor);
    }

    void Update()
    {
        // 如果圆环已经没了，就不跑任何逻辑了
        if (isDissipated) return;
        
        // 1. 增加半径
        currentRadius += expansionSpeed * Time.deltaTime;

        // 2. 检测并触发交互（核心逻辑）
        DetectAndInteract();
        
        
        // 3. 绘制圆环
        DrawCircle();
        
        // 4. 销毁判定
        if (currentRadius >= maxRadius)
        {
            Destroy(gameObject);
        }
    }

    void DetectAndInteract()
    {
        // 获取当前半径内的所有碰撞体
        Collider[] hits = Physics.OverlapSphere(transform.position, currentRadius, targetLayer);

        var sortedHits = hits
            .Where(h => h != null)
            // 修复1：改用 ClosestPointOnBounds 替代 ClosestPoint，完美兼容 MeshCollider 从而避免报错中断
            .OrderBy(h => Vector3.Distance(transform.position, h.ClosestPointOnBounds(transform.position)))
            .ToArray();
        
        foreach (var hit in sortedHits)
        {
            if (isDissipated) break;
            
            // 关键：因为在前一个物体的交互（比如破碎逻辑里直接 Destroy(物体)）之后
            // 后面的碰撞体可能在这同一帧已经跟着父物体一起被销毁了！
            // 尝试访问被销毁的受害者会导致引擎底层的激烈报错（甚至影响 Editor UI绘制）
            if (hit == null || hit.gameObject == null) continue;

            GameObject target = hit.gameObject;

            // 关键修改：不要使用 transform.root，因为如果场景里把所有物体都放在一个 "Environment" 空物体下，
            // 就会导致一碰全碎（它会获取整个场景环境下的所有可破坏物体！）。
            // 默认情况下，自身就是唯一标识，如果它有带刚体的父节点，就以那个父节点为准。
            GameObject rootIdentity = target;
            Rigidbody rb = target.GetComponentInParent<Rigidbody>();
            if (rb != null)
            {
                rootIdentity = rb.gameObject; 
            }
            
            if (rootIdentity == null) continue;

            // 如果这个组合物体还没被处理过
            if (!hitObjects.Contains(rootIdentity))
            {
                // 1. 标记为已处理
                hitObjects.Add(rootIdentity);

                // 修复2：在获取交互接口时，直接从确定的唯一根节点（包含及其所有子节点）上拿所有接口
                // 防止波只撞到子Collider而那个Collider又没挂代码的情况
                IRingInteractable[] interactables = rootIdentity.GetComponentsInChildren<IRingInteractable>();
                
                foreach (var interactable in interactables)
                {
                    // 3. 触发它的反应
                    interactable.OnRingHit(this); 
    
                    // 如果圆环已经被某个护盾（阻碍器）销毁了，就没必要继续检测其他的了
                    if (this == null || isDissipated) return; 
                }
            }
        }
    }
    
    public void Dissipate()
    {
        if (isDissipated) return;

        isDissipated = true; // 标记死亡
        
        // 这里以后可以加“消散特效”，比如播放一个噗呲的声音或粒子
        Debug.Log("圆环撞到了阻碍物，消散了！");
        
        Destroy(gameObject);
    }
    void DrawCircle()
    {
        if (isDissipated) return;
        
        float angleStep = 360f / segments;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            
            // 计算 x 和 z (在水平面上画圆)
            float x = Mathf.Cos(angle) * currentRadius;
            float z = Mathf.Sin(angle) * currentRadius;

            lineRenderer.SetPosition(i, new Vector3(x, 0, z));
        }
    }

    /// <summary>
    /// 被扩波器放大
    /// </summary>
    public void Amplify(float speedMultiplier, float maxRadiusMultiplier)
    {
        if (isAmplified) return; // 防止被同一个/多个扩波器重复放大太多次
        isAmplified = true;

        expansionSpeed *= speedMultiplier;
        maxRadius *= maxRadiusMultiplier;
        
        // 视觉上表现出速度和范围被增强（换色）
        UpdateRingColor(amplifiedColor);
        
        Debug.Log($"圆环被扩波器增强！当前速度: {expansionSpeed}, 最大半径: {maxRadius}");
    }

    private void OnDrawGizmos()
    {
        // 可视化：在编辑器的Scene视图中画出这个波的最大传播范围
        Gizmos.color = isAmplified ? new Color(1f, 0.9f, 0f, 0.5f) : new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxRadius);
    }
}
