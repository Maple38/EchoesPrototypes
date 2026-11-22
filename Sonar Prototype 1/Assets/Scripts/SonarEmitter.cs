using UnityEngine;

public class SonarEmitter : MonoBehaviour
{
    [SerializeField] private int arcSize;
    [SerializeField] private int raycastCount;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Sonar();
        }
    }

    void Sonar()
    {
        float angleDeviation = (float)arcSize / raycastCount;
        for (var i = 0; i < raycastCount; i++)
        {
            RaycastHit2D hit =
                Physics2D.Raycast(transform.position, Quaternion.Euler(0f, 0f, (angleDeviation * i) - 90) * Vector2.up);
        }
    }
}