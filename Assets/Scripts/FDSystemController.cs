using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FDSystemController : MonoBehaviour
{
    public List<Transform> componentPositions;
    public List<Transform> tagPositions;
    //public Transform tagsParent;
    public Material lineMaterial;

    private List<LineRenderer> _connectingLines = new List<LineRenderer>();

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < componentPositions.Count; i++)
        {
            CreateConnectingLines(componentPositions[i], tagPositions[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < _connectingLines.Count; i++)
        {
            _connectingLines[i].SetPosition(0, componentPositions[i].position);
            _connectingLines[i].SetPosition(1, tagPositions[i].transform.position);
        }
    }


    private void CreateConnectingLines(Transform startPos, Transform endPos)
    {
        //For creating line renderer object
        LineRenderer lineRenderer;
        lineRenderer = new GameObject("Line").AddComponent<LineRenderer>();
        lineRenderer.material = lineMaterial;
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        //For drawing line in the world space, provide the x,y,z values
        lineRenderer.SetPosition(0, startPos.position); //x,y and z position of the starting point of the line
        lineRenderer.SetPosition(1, endPos.position); //x,y and z position of the end point of the line
        lineRenderer.widthMultiplier = 0.05f;
        lineRenderer.gameObject.transform.parent = endPos.parent;

        _connectingLines.Add(lineRenderer);
    }
}

