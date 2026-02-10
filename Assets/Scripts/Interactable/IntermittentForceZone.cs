using UnityEngine;
using StarterAssets;

[RequireComponent(typeof(BoxCollider))]
public class IntermittentForceZone : MonoBehaviour
{
    [Header("Force Settings")]
    [Tooltip("The direction of the force (in world space).")]
    public Vector3 forceDirection = Vector3.forward;

    [Tooltip("The magnitude of the force.")]
    public float forceMagnitude = 20f;

    [Tooltip("Time interval safely between force applications (seconds).")]
    public float interval = 2.0f;

    [Tooltip("Starting delay before the first force application.")]
    public float startDelay = 0.5f;

    [Header("Visualization")]
    public Color gizmoColor = new Color(1f, 0f, 0f, 0.3f);
    public float arrowLength = 2.0f;

    private float _timer;
    private bool _isPlayerInside;
    private ThirdPersonController _targetController;

    private void Start()
    {
        // Ensure the collider is a trigger
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _targetController = other.GetComponent<ThirdPersonController>();
            if (_targetController != null)
            {
                _isPlayerInside = true;
                _timer = interval - startDelay; // Apply first force after startDelay
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInside = false;
            _targetController = null;
        }
    }

    private void Update()
    {
        if (!_isPlayerInside || _targetController == null) return;

        _timer += Time.deltaTime;

        if (_timer >= interval)
        {
            ApplyForce();
            _timer = 0f;
        }
    }

    private void ApplyForce()
    {
        if (_targetController != null)
        {
            // Normalize direction but keep separate for clarity in inspector
            // We use transform.TransformDirection if we want local space, 
            // but requirements said "fixed direction", so world space forceDirection is appropriate.
            // If user wants local, they should rotate the object and we'd use transform.forward.
            // Let's assume World Space based on "fixed direction" request, but keep it flexible.
            
            // Actually, "fixed direction" usually implies World Space (e.g. wind blowing East).
            // However, often level designers want to rotate the zone to rotate the force.
            // Let's use Local Space logic if forceDirection is not normalized, or just use vector math.
            
            // To be safe and intuitive: let's use the local rotation of the zone to determine direction
            // if forceDirection is (0,0,1), it pushes along the zone's Forward.
            
            Vector3 worldDir = transform.TransformDirection(forceDirection).normalized;
            
            _targetController.AddImpact(worldDir, forceMagnitude);
        }
    }

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        // Draw Zone Box
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(box.center, box.size);

        // Draw Force Direction Arrow
        // Center of the box in world space
        Vector3 center = transform.TransformPoint(box.center);
        Vector3 direction = transform.TransformDirection(forceDirection).normalized;
        Vector3 endPoint = center + direction * arrowLength;

        Gizmos.matrix = Matrix4x4.identity; // Reset matrix for arrow
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(center, endPoint);
        
        // Draw Arrowhead
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 150, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -150, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(endPoint, right * 0.5f);
        Gizmos.DrawRay(endPoint, left * 0.5f);
    }
}
