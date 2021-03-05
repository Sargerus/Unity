using System;
using System.Diagnostics;
using UnityEngine;

public class GraphPlayer : MonoBehaviour
{
    private Graph _graph;
    private Vector3[] _currentPathCoords;
    private int _currentPathIndex;
    private Vector3 _targetCoordinates;
    private float _speed;
    public GameObject _waypoint;
    private AIManager _aiManager;

    // Start is called before the first frame update
    void Start()
    {
        _speed = 1.0f;
        _aiManager = GameObject.Find("AIManager").GetComponent<AIManager>();
        _graph = _aiManager.GetAIGridGraph();
    }

    // Update is called once per frame
    void Update()
    {
        //MOVE
       // if (_graph == null) _graph = MazeTester.graph;
        if (Input.GetMouseButtonDown(1))
        {
            Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitinfo);
            if (hitinfo.collider.CompareTag("AIGrid"))
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                _currentPathCoords = _graph.AStar(0.2f, 0.2f, transform.localPosition, hitinfo.point);
                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds);
                UnityEngine.Debug.Log("RunTime " + elapsedTime);

                _currentPathIndex = 0;
                _targetCoordinates = _currentPathCoords[_currentPathIndex];
            }
        }
        //MOVE
        //MOVE
        if (_currentPathCoords != null)
            SetNextDirection();

        if (_targetCoordinates != Vector3.zero && transform.localPosition != _targetCoordinates)
            transform.position = Vector3.MoveTowards(transform.localPosition, _targetCoordinates, _speed * Time.deltaTime);
        //MOVE
    }

    private void SetNextDirection()
    {
        if (_currentPathCoords != null && transform.localPosition == _targetCoordinates)
        {
            if (_currentPathIndex == _currentPathCoords.Length - 1)
            {
                _currentPathCoords = null;
                _currentPathIndex = 0;
            }
            else
            {
                _targetCoordinates = _currentPathCoords[++_currentPathIndex];
            }
        }
    }
}
