using System;
using System.Collections.Generic;
using UnityEngine;

public class SonarEmitter : MonoBehaviour
{
    [SerializeField] private int arcSize;
    [SerializeField] private int raycastCount;
    private int _terrainLayerMask;
    [SerializeField] private bool enableBounces;
    private HashSet<Vector2> _visiblePointsStandard;
    
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

    void Sonar()
    {
        _visiblePoints.Clear();
        List<Ray2D> bounceRays = new List<Ray2D>();
        List<Ray2D> rayList = new List<Ray2D>();
        float angleDeviation = (float)arcSize / raycastCount;

        for (var i = 0; i < raycastCount; i++)
        {
            rayList.Add(new Ray2D(transform.position,
                Quaternion.Euler(0f, 0f, (angleDeviation * i) - 90) * Vector2.up));
        }

        var hitList = FireRays(rayList);

        for (var i = 0; i < raycastCount; i++)
        {
            var hit = hitList[i];
            if (!hit.collider.CompareTag("SonarAbsorbs"))
            {
                _visiblePoints.Add(hit.point);
                if (hit.collider.CompareTag("SonarBounces"))
                {
                    bounceRays.Add(new Ray2D(hit.point, Vector2.Reflect(rayList[i].direction, hit.normal)));
                }
            }
        }

        if (enableBounces)
            foreach (var raycastHit2D in FireRays(bounceRays))
            {
                _visiblePointsBounces.Add(raycastHit2D.point);
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
        foreach (var point in _visiblePoints)
        {
            Gizmos.DrawSphere(point, 1);
        }
        
        
        foreach (var point in _visiblePointsBounces)
        {
            Gizmos.DrawSphere(point, 1);
        }
    }
}