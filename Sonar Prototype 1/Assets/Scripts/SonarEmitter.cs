using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SonarEmitter : MonoBehaviour
{
    [SerializeField] private float arcSize;
    [SerializeField] private int raycastCount;
    [SerializeField] private int arcAngleOffset;
    [SerializeField] private float rayDistance;

    private int _terrainLayerMask;
    [SerializeField] private bool enableBounces;
    private List<(Vector2, Color)> _drawPoints;
    private List<(Vector2, Vector2)> _drawBounceRays;
    private List<(Vector2, Vector2)> _drawOutlines;
    private Color _colorStandard;
    private Color _colorAbsorb;
    private Color _colorBounce;
    private Color _colorOutline;
    private Mesh _arcGizmoMesh;
    private Ray2D[] _rayGizmos;
    private List<Ray2D> _rayList;

    void Awake()
    {
        _terrainLayerMask = LayerMask.NameToLayer("Terrain");
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Sonar();
        }

        if (_drawOutlines != null)
        {
            foreach (var (start, end) in _drawOutlines)
            {
                Debug.DrawLine(start, end, _colorOutline);
            }
        }
    }

    List<Ray2D> PopulateRays()
    {
        _rayList ??= new List<Ray2D>();
        _rayList.Clear();
        float angleDeviation = arcSize / raycastCount;
        float rayOffset = arcAngleOffset + (angleDeviation / 2);
        for (var i = 0; i < raycastCount; i++)
        {
            _rayList.Add(new Ray2D(transform.position,
                Quaternion.Euler(0f, 0f, (angleDeviation * i) + rayOffset) * Vector2.right));
        }

        return _rayList;
    }

    void Sonar()
    {
        List<Ray2D> bounceRays = new List<Ray2D>();
        List<Ray2D> rayList = PopulateRays();
        _drawPoints?.Clear();
        _drawBounceRays?.Clear();
        _drawOutlines?.Clear();
        var hitList = FireRays(rayList);

        for (var i = 0; i < raycastCount; i++)
        {
            var hit = hitList[i];
            if (hit)
            {
                Color pointColor = hit.collider.CompareTag("SonarAbsorbs")
                    ? _colorAbsorb
                    : _colorStandard;
                if (hit.collider.CompareTag("SonarBounces"))
                {
                    bounceRays.Add(new Ray2D(hit.point, Vector2.Reflect(rayList[i].direction, hit.normal)));
                }

                _drawPoints.Add((hit.point, pointColor));
            }
        }

        if (enableBounces)
        {
            var bounceHits = FireRays(bounceRays);
            for (int i = 0; i < bounceHits.Count; i++)
            {
                if (bounceHits[i])
                {
                    _drawBounceRays.Add((bounceHits[i].point, bounceRays[i].origin));
                }
            }
        }
    }

    
    List<RaycastHit2D> FireRays(List<Ray2D> rays)
    {
        List<RaycastHit2D> raycastHits = new List<RaycastHit2D>();
        foreach (var ray2D in rays)
        {
            raycastHits.Add(Physics2D.Raycast(ray2D.origin, ray2D.direction,
                rayDistance, _terrainLayerMask));
        }

        return raycastHits;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.darkGreen - new Color(0f, 0f, 0f, 0.8f);
        Gizmos.DrawMesh(_arcGizmoMesh, transform.position);

        Gizmos.color = Color.deepPink;
        foreach (var ray in _rayList)
        {
            Gizmos.DrawLine(ray.origin, ray.origin + (ray.direction * rayDistance));
        }

        if (_drawPoints != null) foreach (var (point, color) in _drawPoints)
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(point, 0.25f);
        }
        if (_drawBounceRays != null)
        {
            Gizmos.color = _colorBounce;
            foreach (var (start, end) in _drawBounceRays)
            {
                Gizmos.DrawLine(start, end);
            }
        }
    }

    private void OnValidate()
    {
        float halfArc = arcSize / 2;
        Vector2 arcVectorInitial = new Vector2(0.5f, 0.5f) * 5;
        Vector2 arcPos1 = Quaternion.Euler(0f, 0f, halfArc + arcAngleOffset) * arcVectorInitial;
        Vector2 arcPos2 = Quaternion.Euler(0f, 0f, -halfArc + arcAngleOffset) * arcVectorInitial;
        _arcGizmoMesh = new Mesh();
        _arcGizmoMesh.vertices = new Vector3[] { Vector3.zero, arcPos1, arcPos2 };
        _arcGizmoMesh.triangles = new[] { 0, 1, 2 };
        _arcGizmoMesh.RecalculateNormals();

        _rayList = PopulateRays();
    }
}