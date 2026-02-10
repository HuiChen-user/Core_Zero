using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class DirectionalFallingPillar : MonoBehaviour, IRingInteractable
{
    [Header("Configuration")]
    [Tooltip("The specific point around which the pillar will rotate. If null, uses bottom center.")]
    public Transform pivotPoint;

    [Tooltip("The local direction the pillar is allowed to fall (e.g., Forward = 0,0,1).")]
    public Vector3 allowedFallDirection = Vector3.forward;

    [Tooltip("The force magnitude applied to push the pillar over.")]
    public float pushForce = 50f;

    [Tooltip("Max angle deviation allowed for the push to trigger the fall.")]
    [Range(0f, 90f)]
    public float hitToleranceAngle = 45f;

    [Header("Physics Settings")]
    [Tooltip("Mass of the pillar (should be heavy).")]
    public float mass = 50f;
    [Tooltip("Drag to prevent wobbling.")]
    public float drag = 0.5f;
    [Tooltip("Angular Drag to prevent wobbling.")]
    public float angularDrag = 0.5f;

    private Rigidbody _rb;
    private bool _hasFallen = false;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        SetupPhysics();
        SetupHingeJoint();
    }

    private void SetupPhysics()
    {
        _rb.mass = mass;
        _rb.drag = drag;
        _rb.angularDrag = angularDrag;
        _rb.isKinematic = true; // Start frozen
        _rb.useGravity = true;
    }

    private void SetupHingeJoint()
    {
        // 1. Determine Pivot Position
        Vector3 pivotPos = pivotPoint != null ? pivotPoint.position : GetBottomCenter();
        
        // 2. Add Hinge Joint
        HingeJoint hinge = gameObject.AddComponent<HingeJoint>();
        
        // 3. Configure Anchor (Local Space relative to Body)
        hinge.anchor = transform.InverseTransformPoint(pivotPos);

        // 4. Configure Axis (Local Space)
        // Axis must be perpendicular to the fall direction and Up vector
        // e.g. If FallDir is Forward (Z+), Axis is Right (X+)
        Vector3 rotationAxis = Vector3.Cross(Vector3.up, allowedFallDirection).normalized;
        hinge.axis = rotationAxis;

        // 5. Configure Limits
        // Prevent falling backwards: Min = -5 (slight wiggle), Max = 100 (fall flat)
        JointLimits limits = new JointLimits();
        limits.min = -5f;
        limits.max = 120f;
        hinge.limits = limits;
        hinge.useLimits = true;

        // 6. Stability
        hinge.enableCollision = true; // Allow collision with other objects? No, usually self-collision is false.
        // Actually, enableCollision usually refers to collision with the connected body. Since connected is null (world), this doesn't matter much.
    }

    private Vector3 GetBottomCenter()
    {
        Collider col = GetComponent<Collider>();
        return col.bounds.center - new Vector3(0, col.bounds.extents.y, 0);
    }

    public void OnRingHit(ExpandingRing ring)
    {
        if (_hasFallen) return;

        // 1. Check Direction
        Vector3 pushDir = (transform.position - ring.transform.position).normalized;
        // Flatten geometry for logic
        pushDir.y = 0; pushDir.Normalize();

        Vector3 myFallDirWorld = transform.TransformDirection(allowedFallDirection).normalized;
        myFallDirWorld.y = 0; myFallDirWorld.Normalize();

        float angle = Vector3.Angle(pushDir, myFallDirWorld);

        if (angle <= hitToleranceAngle)
        {
            TriggerFall(myFallDirWorld);
        }
    }

    private void TriggerFall(Vector3 pushDir)
    {
        _hasFallen = true;

        // Unfreeze
        _rb.isKinematic = false;
        _rb.WakeUp();

        // Push
        // Apply trigger force at the top center
        Collider col = GetComponent<Collider>();
        Vector3 pushPoint = col.bounds.center + new Vector3(0, col.bounds.extents.y * 0.8f, 0);
        
        _rb.AddForceAtPosition(pushDir * pushForce, pushPoint, ForceMode.Impulse);
    }

    private void OnDrawGizmos()
    {
        // Visualize Pivot
        Vector3 pPos = pivotPoint != null ? pivotPoint.position : (GetComponent<Collider>() ? GetBottomCenter() : transform.position);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pPos, 0.2f);

        // Visualize Fall Direction
        Vector3 dir = transform.TransformDirection(allowedFallDirection).normalized;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(pPos, pPos + dir * 3f);
        
        // Visualize Axis
        Vector3 axis = transform.TransformDirection(Vector3.Cross(Vector3.up, allowedFallDirection).normalized);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(pPos - axis, pPos + axis);
    }
}
