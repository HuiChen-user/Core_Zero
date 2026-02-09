using UnityEngine;

public class BreakableObject : MonoBehaviour, IRingInteractable
{
    [Header("设置")]
    [Tooltip("破碎/消失时播放的粒子特效(可选)")]
    public GameObject deathVFX;

    [Tooltip("是否在销毁前延迟一小会儿(秒)")]
    public float destroyDelay = 0f;

    public void OnRingHit(Vector3 ringCenter)
    {
        // 1. 如果有特效，就在当前位置生成
        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position, transform.rotation);
        }

        // 2. 销毁物体
        // 这里的 gameObject 指的是挂载这个脚本的物体本身
        Destroy(gameObject, destroyDelay);

        // Debug信息
        Debug.Log($"{gameObject.name} 被冲击波击碎了！");
    }
}
