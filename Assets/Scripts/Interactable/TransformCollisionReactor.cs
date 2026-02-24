using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace StarterAssets
{
    public class TransformCollisionReactor : MonoBehaviour
    {
        [Header("Transform Settings")]
        [Tooltip("相对初始位置的偏移量")]
        public Vector3 positionOffset = new Vector3(0, 2, 0);
        [Tooltip("相对初始旋转的偏移量(欧拉角)")]
        public Vector3 rotationOffset = new Vector3(0, 180, 0);
        [Tooltip("变化过程持续的时间(秒)")]
        public float duration = 2f;
        
        [Header("Visualization")]
        [Tooltip("是否在场景中绘制目标位置的参考框")]
        public bool showTargetGizmo = true;
        [Tooltip("参考框的颜色")]
        public Color gizmoColor = new Color(0, 1, 0, 0.5f);
        
        [Header("Trigger Conditions")]
        [Tooltip("可选：指定必须碰撞的特定子物体(例如只放一个贴在某一面的BoxCollider)。如果不指定，则碰撞自身或任意子物体都会触发。")]
        public GameObject requiredTriggerObject;
        
        [Tooltip("可选：是否启用按碰撞面法线方向触发。")]
        public bool useNormalDetection = false;
        [Tooltip("必须碰撞的法线方向(世界坐标系)。例如(0,1,0)表示只能从正上方踩踏触发。")]
        public Vector3 validHitNormal = Vector3.up;
        [Tooltip("法线容差，1表示必须完全一致，0.5表示允许一定角度倾斜")]
        [Range(0f, 1f)]
        public float normalTolerance = 0.9f;

        private bool _hasTriggered = false;
        private Canvas _uiCanvas;
        private Image _progressBar;
        
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        
        private void Awake()
        {
            _startPosition = transform.position;
            _startRotation = transform.rotation;
            CreateProgressBar();
        }
        
        public void OnHitByPlayer(ControllerColliderHit hit)
        {
            // 如果已经触发过，则不再响应，保证只触发一次并停留在目标值
            if (_hasTriggered) return;

            // 1. 检查是否碰撞了指定的触发物体
            if (requiredTriggerObject != null && hit.gameObject != requiredTriggerObject)
            {
                return;
            }

            // 2. 检查法线方向是否符合要求
            if (useNormalDetection)
            {
                // hit.normal 是碰撞点表面的法线方向
                float dotProduct = Vector3.Dot(hit.normal.normalized, validHitNormal.normalized);
                if (dotProduct < normalTolerance)
                {
                    return; // 角度偏差过大，不触发
                }
            }

            StartCoroutine(AnimateTransform());
        }
        
        private IEnumerator AnimateTransform()
        {
            _hasTriggered = true;
            _uiCanvas.gameObject.SetActive(true);
            
            Vector3 endPos = _startPosition + positionOffset;
            Quaternion endRot = _startRotation * Quaternion.Euler(rotationOffset);
            
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                
                // 1. Transform组件的改变
                transform.position = Vector3.Lerp(_startPosition, endPos, progress);
                transform.rotation = Quaternion.Lerp(_startRotation, endRot, progress);
                
                // 2. 时间改变的可视化（UI进度条）
                if (_progressBar != null)
                {
                    _progressBar.fillAmount = progress;
                }
                
                yield return null;
            }
            
            // 确保最终状态准确，并保持不再改变
            transform.position = endPos;
            transform.rotation = endRot;
            
            // 动画结束后隐藏进度条
            _uiCanvas.gameObject.SetActive(false);
        }
        
        private void CreateProgressBar()
        {
            // 创建World Space Canvas
            GameObject canvasObj = new GameObject("ProgressCanvas");
            canvasObj.transform.SetParent(transform);
            // 将Canvas置于物体上方
            canvasObj.transform.localPosition = new Vector3(0, 1.5f, 0); 
            canvasObj.transform.localScale = Vector3.one * 0.01f;
            
            _uiCanvas = canvasObj.AddComponent<Canvas>();
            _uiCanvas.renderMode = RenderMode.WorldSpace;
            
            // 背景底框
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.5f);
            bgImage.rectTransform.sizeDelta = new Vector2(100, 20);
            
            // 前景进度条
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(canvasObj.transform, false);
            _progressBar = fillObj.AddComponent<Image>();
            _progressBar.color = Color.green;
            _progressBar.rectTransform.sizeDelta = new Vector2(100, 20);
            _progressBar.type = Image.Type.Filled;
            _progressBar.fillMethod = Image.FillMethod.Horizontal;
            _progressBar.fillAmount = 0;
            
            _uiCanvas.gameObject.SetActive(false); // 初始隐藏
            
            // 添加脚本让Canvas始终面向主相机
            canvasObj.AddComponent<FaceCamera>();
        }
        
        private void OnDrawGizmos()
        {
            if (!showTargetGizmo) return;
            
            // 在编辑器未运行时，使用当前位置作为起点；运行时使用记录的起点
            Vector3 startPos = Application.isPlaying ? _startPosition : transform.position;
            Quaternion startRot = Application.isPlaying ? _startRotation : transform.rotation;
            
            Vector3 targetPos = startPos + positionOffset;
            Quaternion targetRot = startRot * Quaternion.Euler(rotationOffset);
            
            Gizmos.color = gizmoColor;
            
            // 保存当前的Gizmo矩阵
            Matrix4x4 oldMatrix = Gizmos.matrix;
            
            // 设置Gizmo矩阵来到目标位置和旋转
            Gizmos.matrix = Matrix4x4.TRS(targetPos, targetRot, transform.lossyScale);
            
            // 尝试获取物体的MeshFilter来绘制同样的网格，如果没有则绘制一个默认的立方体
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Gizmos.DrawWireMesh(meshFilter.sharedMesh);
            }
            else
            {
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            }
            
            // 恢复Gizmo矩阵
            Gizmos.matrix = oldMatrix;
            
            // 画一条线连接起点和终点，方便查看路径
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            Gizmos.DrawLine(startPos, targetPos);
        }
    }
    
    /// <summary>
    /// 辅助组件：让UI始终朝向摄像机，方便观察
    /// </summary>
    public class FaceCamera : MonoBehaviour
    {
        private Camera _mainCamera;
        
        void Start()
        {
            _mainCamera = Camera.main;
        }
        
        void LateUpdate()
        {
            if (_mainCamera != null)
            {
                transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                    _mainCamera.transform.rotation * Vector3.up);
            }
        }
    }
}
