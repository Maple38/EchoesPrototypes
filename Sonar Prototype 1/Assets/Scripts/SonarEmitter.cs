using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SonarEmitter : MonoBehaviour
{
    [Header("Debug Toggles")]
    [SerializeField] private bool showArc;
    [SerializeField] private bool showRays;
    [SerializeField] private bool showBounceLines;
    [SerializeField] private bool showFailedBounces;
    [SerializeField] private bool showHits;
    [SerializeField] private float impactHighlightRadius;
    [SerializeField] private bool hideOtherObjects;

    [Header("Sonar Parameters")]
    [SerializeField] private float arcSize;
    [SerializeField] private int arcAngleOffset;
    [SerializeField] private int raycastCount;
    [SerializeField] private float rayDistance;
    [SerializeField] private bool enableBounces;

    [Header("Colors")]
    [SerializeField] private Color colorStandard = Color.green;
    [SerializeField] private Color colorAbsorb = Color.red;
    [SerializeField] private Color colorBounce = Color.yellowNice;
    [SerializeField] private Color colorOutline = Color.turquoise;
    
    private readonly List<(Vector2, Vector2)> _drawBounceRays = new();
    private readonly List<(Vector2, Vector2)> _drawOutlines = new();
    private readonly List<(Vector2, Color)> _drawPoints = new();
    private Mesh _arcGizmoMesh;
    private Mesh _arcGizmoMeshOld;
    private List<Ray2D> _rayList;
    private int _terrainLayerMask;

    private void Awake()
    {
        _terrainLayerMask = LayerMask.GetMask("Terrain");
        var terrainLayer = LayerMask.NameToLayer("Terrain");
        if (hideOtherObjects)
        {
            foreach (var obj in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (obj.layer == terrainLayer)
                {
                    obj.GetComponent<SpriteRenderer>().enabled = false;
                }
            }
        }
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Sonar();
        }

        if (_drawOutlines != null)
        {
            foreach (var (start, end) in _drawOutlines)
            {
                Debug.DrawLine(start, end, colorOutline);
            }
        }

        if (showBounceLines && _drawBounceRays != null)
        {
            foreach (var (start, end) in _drawBounceRays)
            {
                Debug.DrawLine(start, end, colorBounce);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.darkGreen - new Color(0f, 0f, 0f, 0.8f);

        if (showArc)
        {
            Gizmos.DrawMesh(_arcGizmoMesh, transform.position);
        }

        Gizmos.color = Color.deepPink;
        if (showRays)
        {
            foreach (var ray in _rayList)
            {
                Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * rayDistance);
            }
        }

        if (showHits && _drawPoints != null)
        {
            foreach (var (point, color) in _drawPoints)

            {
                Gizmos.color = color;
                Gizmos.DrawSphere(point, 0.05f);
            }
        }
    }

    private void OnValidate()
    {
        const int arcVertexGapTarget = 10;
        var arcVertexCount = Mathf.CeilToInt(arcSize / arcVertexGapTarget);
        var arcVertexGap = arcSize / (arcVertexCount - 1);

        var arcVertices = new Vector3[arcVertexCount + 1];
        arcVertices[0] = Vector3.zero;
        for (var i = 0; i < arcVertexCount; i++)
        {
            arcVertices[i + 1] = Quaternion.Euler(0, 0, ArcVertexRotation(i)) * Vector2.up * 2;
        }

        var arcTriangles = new int[(arcVertexCount + 1) * 3];
        for (var i = 0; i < arcVertexCount - 1; i++)
        {
            arcTriangles[i * 3] = 0;
            arcTriangles[i * 3 + 1] = i + 1;
            arcTriangles[i * 3 + 2] = i + 2;
        }

        _arcGizmoMesh = new Mesh { vertices = arcVertices, triangles = arcTriangles };
        _arcGizmoMesh.RecalculateNormals();

        PopulateRays();
        return;

        float ArcVertexRotation(int vertex) => arcAngleOffset + arcSize / 2 - arcVertexGap * vertex;
    }

    private void PopulateRays()
    {
        _rayList ??= new List<Ray2D>();
        _rayList.Clear();
        var angleGap = arcSize / (raycastCount + 1);
        var offset = arcSize / 2 + arcAngleOffset;
        for (var i = 0; i < raycastCount; i++)
        {
            _rayList.Add(new Ray2D(transform.position,
                Quaternion.Euler(0f, 0f, -angleGap * (i + 1) + offset) * Vector2.up));
        }
    }

    private bool ParseHit(RaycastHit2D hit)
    {
        if (hit)
        {
            if (!hit.collider.CompareTag("SonarAbsorbs"))
            {
                _drawOutlines.Add(GetHighlightLine(hit));
                return true;
            }

            _drawPoints.Add((hit.point, colorAbsorb));
        }

        return false;
    }

    private void Sonar()
    {
        var bounceRays = new List<Ray2D>();
        PopulateRays();
        _drawPoints.Clear();
        _drawBounceRays.Clear();
        _drawOutlines.Clear();
        var hitList = FireRays(_rayList);

        for (var i = 0; i < raycastCount; i++)
        {
            var hit = hitList[i];
            if (ParseHit(hit))
            {
                _drawPoints.Add((hit.point, colorStandard));
                if (hit.collider.CompareTag("SonarBounces"))
                {
                    bounceRays.Add(new Ray2D(hit.point + hit.normal * 0.01f,
                        Vector2.Reflect(_rayList[i].direction, hit.normal)));
                }
            }
        }

        if (enableBounces)
        {
            var bounceHits = FireRays(bounceRays);
            for (var i = 0; i < bounceHits.Count; i++)
            {
                if (ParseHit(bounceHits[i]))
                {
                    _drawBounceRays.Add((bounceRays[i].origin, bounceHits[i].point));
                    _drawPoints.Add((bounceHits[i].point, colorBounce));
                }
                else if (showFailedBounces)
                {
                    _drawBounceRays.Add((bounceRays[i].origin, bounceRays[i].direction * rayDistance));
                }
            }
        }
    }


    private List<RaycastHit2D> FireRays(List<Ray2D> rays)
    {
        var raycastHits = new List<RaycastHit2D>();
        foreach (var ray2D in rays)
        {
            raycastHits.Add(Physics2D.Raycast(ray2D.origin, ray2D.direction,
                rayDistance, _terrainLayerMask));
        }

        return raycastHits;
    }

    private (Vector2, Vector2) GetHighlightLine(RaycastHit2D hit)
    {
        var probeHitArray = new List<RaycastHit2D>();
        var rayContactFilter = new ContactFilter2D { useLayerMask = true, layerMask = _terrainLayerMask };
        var tangent = Vector2.Perpendicular(hit.normal);
        var point1 = Probe(1);
        var point2 = Probe(-1);

        Vector2 Probe(int directionSign)
        {
            probeHitArray.Clear();
            var direction = tangent * directionSign;
            var origin = hit.point + impactHighlightRadius * direction;
            Physics2D.Raycast(origin, -direction, rayContactFilter, probeHitArray, impactHighlightRadius);
            // _drawPoints.Add((origin, Color.mediumPurple));
            foreach (var probeHit in probeHitArray)
            {
                // _drawPoints.Add((probeHit.point, Color.black));
                if (probeHit.collider.gameObject == hit.collider.gameObject)
                {
                    return probeHit.point;
                }
            }

            return origin;
        }

        return (point1, point2);
    }
}