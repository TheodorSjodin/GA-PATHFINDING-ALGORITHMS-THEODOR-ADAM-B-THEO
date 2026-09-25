using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BeanBagTrail : MonoBehaviour
{
    public float trailHeight = 0.08f;
    public float pointDistance = 0.05f;
    public float trailWidth = 0.08f;

    private LineRenderer lineRenderer;

    private List<Vector3> points =
        new List<Vector3>();

    void Start()
    {
        lineRenderer =
            GetComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;

        lineRenderer.startWidth =
            trailWidth;

        lineRenderer.endWidth =
            trailWidth;

        lineRenderer.positionCount = 0;

        Material material =
            new Material(
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                )
            );

        material.color = Color.red;

        lineRenderer.material = material;

        AddPoint();
    }

    void Update()
    {
        Vector3 currentPosition =
            transform.position;

        currentPosition.y =
            trailHeight;

        if (points.Count == 0)
        {
            AddPoint();
            return;
        }

        float distance =
            Vector3.Distance(
                points[points.Count - 1],
                currentPosition
            );

        if (distance >= pointDistance)
        {
            AddPoint();
        }
    }

    void AddPoint()
    {
        Vector3 position =
            transform.position;

        position.y =
            trailHeight;

        points.Add(position);

        lineRenderer.positionCount =
            points.Count;

        lineRenderer.SetPosition(
            points.Count - 1,
            position
        );
    }
}