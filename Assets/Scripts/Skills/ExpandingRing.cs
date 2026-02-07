using UnityEngine;
using System.Collections.Generic;

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
    
    private LineRenderer lineRenderer;
    private float currentRadius = 0f;

    // 关键点：防止同一个物体每一帧都被触发一次
    private HashSet<GameObject> hitObjects = new HashSet<GameObject>();
    
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = segments + 1; // +1 是为了闭合圆环
        lineRenderer.useWorldSpace = false; // 使用本地坐标，跟随物体移动
        lineRenderer.loop = true;
    }

    void Update()
    {
        // 1. 增加半径
        currentRadius += expansionSpeed * Time.deltaTime;

        // 2. 检查是否达到最大半径
        if (currentRadius >= maxRadius)
        {
            Destroy(gameObject); // 销毁自身
            return;
        }

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

        foreach (var hit in hits)
        {
            GameObject target = hit.gameObject;

            // 如果这个物体还没被处理过
            if (!hitObjects.Contains(target))
            {
                // 1. 标记为已处理
                hitObjects.Add(target);

                // 2. 尝试获取接口（问它：你会对圆环有反应吗？）
                // 注意：这里查找的是接口，不是具体的类
                IRingInteractable interactable = target.GetComponent<IRingInteractable>();
                
                if (interactable != null)
                {
                    // 3. 触发它的反应
                    interactable.OnRingHit(transform.position);
                }
            }
        }
    }
    
    void DrawCircle()
    {
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
}
