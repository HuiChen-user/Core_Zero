using UnityEngine;
using StarterAssets;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(LineRenderer))]
public class AnchorStone : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("The radius of the anchor zone.")]
    public float radius = 3.0f;
    [Tooltip("Key to hold for anchoring.")]
    public KeyCode interactKey = KeyCode.F;
    [Tooltip("Color of the ring when active.")]
    public Color activeColor = Color.cyan;
    [Tooltip("Color of the ring when inactive.")]
    public Color inactiveColor = Color.gray;

    [Header("Visuals")]
    public int ringSegments = 50;

    private SphereCollider _collider;
    private LineRenderer _lineRenderer;
    private ThirdPersonController _playerInside;
    private bool _isAnchoring = false;

    private void Awake()
    {
        _collider = GetComponent<SphereCollider>();
        _lineRenderer = GetComponent<LineRenderer>();

        // Setup Collider
        _collider.isTrigger = true;
        _collider.radius = radius;

        // Setup LineRenderer
        _lineRenderer.useWorldSpace = false;
        _lineRenderer.loop = true;
        _lineRenderer.positionCount = ringSegments + 1;
        _lineRenderer.startWidth = 0.1f;
        _lineRenderer.endWidth = 0.1f;
        
        UpdateRingVisuals(false);
        DrawRing();
    }

    private void Update()
    {
        if (_playerInside == null) return;

        // Check Input
        if (Input.GetKey(interactKey))
        {
            if (!_isAnchoring)
            {
                StartAnchoring();
            }
        }
        else
        {
            if (_isAnchoring)
            {
                StopAnchoring();
            }
        }
    }

    private void StartAnchoring()
    {
        _isAnchoring = true;
        _playerInside.IsAnchored = true;
        UpdateRingVisuals(true);
        Debug.Log("Player Anchored!");
    }

    private void StopAnchoring()
    {
        _isAnchoring = false;
        if (_playerInside != null)
        {
            _playerInside.IsAnchored = false;
        }
        UpdateRingVisuals(false);
        Debug.Log("Player Released!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _playerInside = other.GetComponent<ThirdPersonController>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // If player leaves while holding F, force release
            if (_isAnchoring)
            {
                StopAnchoring();
            }
            _playerInside = null;
        }
    }

    private void UpdateRingVisuals(bool isActive)
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = isActive ? activeColor : inactiveColor;
            _lineRenderer.endColor = isActive ? activeColor : inactiveColor;
        }
    }

    private void DrawRing()
    {
        float angleStep = 360f / ringSegments;
        for (int i = 0; i <= ringSegments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            _lineRenderer.SetPosition(i, new Vector3(x, 0.1f, z)); // Slightly above ground
        }
    }

    private void OnValidate()
    {
        // Update collider radius in editor when changing radius
        if (_collider == null) _collider = GetComponent<SphereCollider>();
        if (_collider != null) _collider.radius = radius;
    }
}
