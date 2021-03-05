using UnityEngine;

public class AIManager : MonoBehaviour
{
    private GameObject _aiGrid;
    private Graph _graph;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void CreateAIGrid(float aiGridWidth, float aiGridLength)
    {
        _aiGrid = new GameObject("AIGrid");
        _aiGrid.transform.SetParent(this.transform);
        _aiGrid.transform.localScale = new Vector3(aiGridLength, 0.1f, aiGridWidth);
        var collider = _aiGrid.AddComponent<BoxCollider>(); collider.isTrigger = true;
        _aiGrid.transform.tag = "AIGrid";

        var bounds = _aiGrid.GetComponent<BoxCollider>().bounds;
        _graph = new Graph(_aiGrid.transform.position - bounds.extents, _aiGrid.transform.position + bounds.extents);
    }

    public Graph GetAIGridGraph() => _graph;

    // Update is called once per frame
    void Update()
    {
        
    }
}
