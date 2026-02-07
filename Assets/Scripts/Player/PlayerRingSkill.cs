using UnityEngine;

public class PlayerRingSkill : MonoBehaviour
{
    public GameObject ringPrefab; // 拖入上面做好的 Prefab
    
    [Header("预览设置")]
    public float previewMaxRadius = 10f; // 仅用于Gizmos显示

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)|| Input.GetKeyDown(KeyCode.Q))
        {
            Instantiate(ringPrefab, transform.position, Quaternion.identity);
        }
    }

    // 实现“编辑器可视化调整”的核心代码
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        // 画出最大半径的线框球，方便你在编辑器里看范围
        Gizmos.DrawWireSphere(transform.position, previewMaxRadius);
    }
}
