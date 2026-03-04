using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class WaveDrivenMover : MonoBehaviour, IRingInteractable
{
    [Header("位移设置")]
    [Tooltip("位移的方向和距离（局部坐标系下）")]
    public Vector3 localMoveOffset = new Vector3(0, 0, 5f);
    
    [Tooltip("位移速度（单位/秒）")]
    public float moveSpeed = 5f;

    [Header("可视化与平滑")]
    [Tooltip("是否在游戏开始前允许波触发复位？")]
    public bool isOneShot = true;
    [Tooltip("到达终点目标颜色的可视化线框")]
    public Color gizmoTargetColor = Color.green;
    [Tooltip("表示速度的路径指示线颜色")]
    public Color gizmoPathColor = Color.cyan;

    private bool _hasTriggered = false;
    private Vector3 _startPos;
    private Vector3 _targetPos;

    private void Start()
    {
        _startPos = transform.position;
        _targetPos = transform.TransformPoint(localMoveOffset);
    }

    public void OnRingHit(ExpandingRing ring)
    {
        if (_hasTriggered && isOneShot) return;

        // 如果在移动中重复触发，可能会覆盖目标点。确保再次触发时重新计算位置或者只允许一次位移
        _hasTriggered = true;
        _startPos = transform.position;
        _targetPos = transform.TransformPoint(localMoveOffset);

        StopAllCoroutines();
        StartCoroutine(MoveToTargetCoroutine());

        Debug.Log(gameObject.name + " 受到波的冲击，开始平滑位移！");
    }

    private IEnumerator MoveToTargetCoroutine()
    {
        while (Vector3.Distance(transform.position, _targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, _targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = _targetPos; // 确保精准到达
    }

    private void OnDrawGizmosSelected()
    {
        // 1. 确定最终的世界坐标目标点
        Vector3 currentPos = Application.isPlaying && _hasTriggered ? _startPos : transform.position;
        Vector3 targetPos = Application.isPlaying && _hasTriggered ? _targetPos : transform.TransformPoint(localMoveOffset);

        // 2. 绘制目标的模型轮廓线框，让作者“能知道位置在哪儿”
        Gizmos.color = gizmoTargetColor;
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            // 通过当前物体的旋转和缩放来绘制目标位置处一致的网格线框
            Gizmos.DrawWireMesh(meshFilter.sharedMesh, targetPos, transform.rotation, transform.localScale);
        }
        else
        {
            // 如果没有Mesh，用Collider的Bounds代替
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.DrawWireCube(targetPos, col.bounds.size);
            }
            else
            {
                Gizmos.DrawWireSphere(targetPos, 0.5f);
            }
        }

        // 3. 绘制路径上的“速度可视化”带方向箭头或点
        Gizmos.color = gizmoPathColor;
        Gizmos.DrawLine(currentPos, targetPos);

        DrawSpeedMarkers(currentPos, targetPos);
    }

    private void DrawSpeedMarkers(Vector3 start, Vector3 end)
    {
        float distance = Vector3.Distance(start, end);
        if (distance <= 0.01f) return;

        Vector3 direction = (end - start).normalized;
        
        // 速度越大，点/箭头的间隔可能越长或越密，这里约定：箭头的间距 = 1 / moveSpeed （确保在合理范围内）
        // 速度快 = 间隔稀疏（表示跨越步子大） 或者 速度快 = 间隔密集（视觉效果更强），用户通常认为密集或快速移动的箭头代表快。
        // 这里设计：根据移动速度画刻度，每个刻度表示 "每 0.5 秒钟移动一步" 的距离
        
        float timeInterval = 0.5f; // 每 0.5 秒一个标记
        float distanceInterval = moveSpeed * timeInterval;
        
        if (distanceInterval <= 0.1f) distanceInterval = 0.1f; // 防死循环

        int numMarkers = Mathf.FloorToInt(distance / distanceInterval);
        
        for (int i = 1; i <= numMarkers; i++)
        {
            Vector3 markerPos = start + direction * (i * distanceInterval);
            DrawArrowGizmo(markerPos, direction);
        }
    }

    private void DrawArrowGizmo(Vector3 pos, Vector3 direction)
    {
        // 画一个小箭头
        float arrowHeadLength = 0.4f;
        float arrowHeadAngle = 20.0f;

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);

        Gizmos.DrawRay(pos, right * arrowHeadLength);
        Gizmos.DrawRay(pos, left * arrowHeadLength);
        Gizmos.DrawRay(pos, direction * (arrowHeadLength * 0.5f)); // 中心短线
    }
}
