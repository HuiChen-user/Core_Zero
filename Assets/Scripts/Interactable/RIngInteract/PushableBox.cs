using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushableBox : MonoBehaviour, IRingInteractable // 注意这里继承了接口
{
    public float pushForce = 10f;

    // 实现接口强制要求的方法
    public void OnRingHit(ExpandingRing ring)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        Collider col = GetComponent<Collider>(); // 获取自身的碰撞器

        Vector3 ringCenter = ring.transform.position;
        
        // 1. 找到箱子表面离圆心最近的点（这才是真正的“受力点”）
        Vector3 closestPoint = col.ClosestPoint(ringCenter);

        // 2. 拍扁高度（只取水平面）
        Vector3 flatHitPoint = new Vector3(closestPoint.x, 0, closestPoint.z);
        Vector3 flatRingPos = new Vector3(ringCenter.x, 0, ringCenter.z);

        // 3. 计算从“圆心”指向“受力点”的方向
        Vector3 direction = (flatHitPoint - flatRingPos).normalized;

        // 保护机制：如果圆心就在物体内部，direction可能会变成0，这时退回到用中心点
        if (direction == Vector3.zero)
        {
            Vector3 flatBoxCenter = new Vector3(transform.position.x, 0, transform.position.z);
            direction = (flatBoxCenter - flatRingPos).normalized;
        }

        rb.AddForce(direction * pushForce, ForceMode.Impulse);
        
        // (可选) 为了调试，画出受力方向看看
        Debug.DrawLine(flatRingPos, flatHitPoint, Color.red, 2f);
    }
}
