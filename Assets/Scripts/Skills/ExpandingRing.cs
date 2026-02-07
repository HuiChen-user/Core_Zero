using UnityEngine;

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

    private LineRenderer lineRenderer;
    private float currentRadius = 0f;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = segments + 1; // +1 是为了闭合圆环
        lineRenderer.useWorldSpace = false; // 使用本地坐标，跟随物体移动
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
