using UnityEngine;

// 这是一个接口，它不干活，只定义“必须干什么”
public interface IRingInteractable
{
    // 所有能被圆环影响的物体，都必须实现这个方法
    // ringCenter: 传入圆心位置，方便物体计算推力方向
    void OnRingHit(Vector3 ringCenter);
}
