using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushableBox : MonoBehaviour, IRingInteractable // 注意这里继承了接口
{
    public float pushForce = 10f;

    // 实现接口强制要求的方法
    public void OnRingHit(Vector3 ringCenter)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        
        // 计算推力方向：从圆心指向我
        Vector3 direction = (transform.position - ringCenter).normalized;
        direction.y = 0; // 保持水平推力

        rb.AddForce(direction * pushForce, ForceMode.Impulse);
        
        Debug.Log("箱子被推开了！");
    }
}
