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
    private readonly List<(Vector2, Color)> _drawPoints = new List<(Vector2, Color)>();
    private readonly List<(Vector2, Vector2)> _drawBounceRays = new List<(Vector2, Vector2)>();
    private readonly List<(Vector2, Vector2)> _drawOutlines = new List<(Vector2, Vector2)>();
    [SerializeField] private float impactHighlightRadius;

    private Color _colorStandard = Color.green;
    private Color _colorAbsorb = Color.red;
    private Color _colorBounce = Color.yellowNice;
    private Color _colorOutline = Color.turquoise;
    private Mesh _arcGizmoMesh;
    private List<Ray2D> _rayList;

    void Awake()
    {
        _terrainLayerMask = LayerMask.GetMask("Terrain");
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

    void PopulateRays()
    {
        _rayList ??= new List<Ray2D>();
        _rayList.Clear();
        float angleGap = arcSize / (raycastCount + 1);
        var offset = (arcSize / 2) + arcAngleOffset;
        for (var i = 0; i < raycastCount; i++)
        {
            _rayList.Add(new Ray2D(transform.position,
                Quaternion.Euler(0f, 0f, (-angleGap * (i + 1) + offset)) * Vector2.up));
        }
    }

    void Sonar()
    {
        List<Ray2D> bounceRays = new List<Ray2D>();
        PopulateRays();
        _drawPoints?.Clear();
        _drawBounceRays?.Clear();
        _drawOutlines?.Clear();
        var hitList = FireRays(_rayList);

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
                    bounceRays.Add(new Ray2D(hit.point + (hit.normal * 0.01f),
                        Vector2.Reflect(_rayList[i].direction, hit.normal)));
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

        Debug.Log(raycastHits[0].collider);
        return raycastHits;
    }

    (Vector2, Vector2) GetHighlightLine(RaycastHit2D hit)
    {
        List<RaycastHit2D> probeHitArray = new List<RaycastHit2D>();
        var rayContactFilter = new ContactFilter2D { useLayerMask = true, layerMask = _terrainLayerMask };
        var tangent = Vector2.Perpendicular(hit.normal);
        Vector2 point1 = Probe(1);
        Vector2 point2 = Probe(-1);

        Vector2 Probe(int directionSign)
        {
            probeHitArray.Clear();
            var direction = tangent * directionSign;
            var origin = hit.point + (impactHighlightRadius * direction);
            Physics2D.Raycast(origin, -direction, rayContactFilter, probeHitArray, impactHighlightRadius);
            foreach (var probeHit in probeHitArray)
            {
                if (probeHit.collider.gameObject == hit.collider.gameObject)
                {
                    return probeHit.point;
                }
            }
            return origin;
        }

        return (point1, point2);
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

        if (_drawPoints != null)
        {
            foreach (var (point, color) in _drawPoints)

            {
                Gizmos.color = color;
                Gizmos.DrawSphere(point, 0.25f);
            }
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
        var halfArc = arcSize / 2;
        var arcVectorInitial = Vector2.up * 2;
        Vector2 arcPos1 = Quaternion.Euler(0, 0, arcAngleOffset + halfArc) * arcVectorInitial;
        Vector2 arcPos2 = Quaternion.Euler(0, 0, arcAngleOffset - halfArc) * arcVectorInitial;
        _arcGizmoMesh = new Mesh
        {
            vertices = new Vector3[] { Vector3.zero, arcPos1, arcPos2 },
            triangles = new[] { 0, 1, 2 }
        };
        _arcGizmoMesh.RecalculateNormals();

        PopulateRays();
    }
}