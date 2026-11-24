using System.Collections.Generic;
using UnityEngine;

public class SonarEmitter : MonoBehaviour
{
    [SerializeField] private int arcSize;
    [SerializeField] private int raycastCount;
    [SerializeField] private int arcAngleOffset;
    private int _terrainLayerMask;
    [SerializeField] private bool enableBounces;
    private List<(Vector2, Color)> _visiblePoints;
    private Color _pointColorStandard = Color.green;
    private Color _pointColorBouncePoint = Color.yellow;
    private Color _pointColorBounceEnd = Color.red;
    private Mesh _arcGizmoMesh;
    private Vector2[] _rayGizmos;

    void Awake()
    {
        _terrainLayerMask = LayerMask.NameToLayer("Terrain");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Sonar();
        }
    }
    
    List<Ray2D> PopulateRays()
    {
        List<Ray2D> rayList = new List<Ray2D>();
        float angleDeviation = (float)arcSize / raycastCount;
        for (var i = 0; i < raycastCount; i++)
        {
            rayList.Add(new Ray2D(transform.position,
                Quaternion.Euler(0f, 0f, (angleDeviation * i) + arcAngleOffset) * Vector2.up));
        }
        return rayList;
    }
    
    void Sonar()
    {
        List<Ray2D> bounceRays = new List<Ray2D>();
        List<Ray2D> rayList = PopulateRays();
        
        var hitList = FireRays(rayList);

        for (var i = 0; i < raycastCount; i++)
        {
            var hit = hitList[i];
            if (!hit.collider.CompareTag("SonarAbsorbs"))
            {
                var pointColor = _pointColorStandard;
                if (hit.collider.CompareTag("SonarBounces"))
                {
                    bounceRays.Add(new Ray2D(hit.point, Vector2.Reflect(rayList[i].direction, hit.normal)));
                    pointColor = _pointColorBouncePoint;
                }

                _visiblePoints.Add((hit.point, pointColor));
            }
        }

        if (enableBounces)
            foreach (var raycastHit2D in FireRays(bounceRays))
            {
                _visiblePoints.Add((raycastHit2D.point, _pointColorBounceEnd));
            }
    }

    List<RaycastHit2D> FireRays(List<Ray2D> rays)
    {
        List<RaycastHit2D> raycastHits = new List<RaycastHit2D>();
        foreach (var ray2D in rays)
        {
            raycastHits.Add(Physics2D.Raycast(ray2D.origin, ray2D.direction,
                100, _terrainLayerMask));
        }

        return raycastHits;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawMesh(_arcGizmoMesh, transform.position);

        // foreach (var (point, color) in _visiblePoints)
        // {
        //     Gizmos.color = color;
        //     Gizmos.DrawSphere(point, 1);
        // }
    }

    private void OnValidate()
    {
        int halfArc = arcSize/2;
        Vector2 arcVectorInitial = new Vector2(0.5f, 0.5f) * 5;
        Vector2 arcPos1 = Quaternion.Euler(0f, 0f, halfArc + arcAngleOffset) * arcVectorInitial;
        Vector2 arcPos2 = Quaternion.Euler(0f, 0f, -halfArc + arcAngleOffset) * arcVectorInitial;
        _arcGizmoMesh = new Mesh();
        _arcGizmoMesh.vertices = new Vector3[] { Vector3.zero, arcPos1, arcPos2 };
        _arcGizmoMesh.triangles = new [] { 0, 1, 2 };
        _arcGizmoMesh.RecalculateNormals();

        _rayGizmos = new Vector2[raycastCount];   


    }
}