using System.Collections.Generic;
using UnityEngine;

public struct Edge
{
    public enum Direction : byte
    {
        W = 0,
        WN,
        N,
        NE,
        E,
        SE,
        S,
        SW
    }
    public int start;
    public int end;
    public Direction linkDirection;
    public bool isBiDirectional;

    public Edge(int start, int end, Direction linkDirection, bool isBiDir = true)
    {
        this.start = start;
        this.end = end;
        this.linkDirection = linkDirection;
        this.isBiDirectional = isBiDir;
    }
}
public struct Node
{
    private List<Edge> _edges;
    private List<int> neighbors;

    public Node(Edge[] edges)
    {
        _edges = new List<Edge>(edges);
        neighbors = new List<int>(edges.Length);

        foreach (var edge in edges)
            neighbors.Add(edge.end);
    }

    public Edge[] GetAvailableEdges()
    {
        int i = 0;
        if (_edges == null) 
            i = 0;

       return _edges.ToArray();
    }
    

    public bool hasNeighbor(int index) => neighbors.Contains(index);

    public Edge FindEdge(int nodeIndex)
    {
        Edge edge = new Edge();
        for (int i = 0; i < _edges.Count; i++)
            if (nodeIndex == _edges[i].end)
            {
                edge = _edges[i];
                break;
            }
        return edge;
    }
    public void AddEdge(int start, int end, bool isBiDir)
    {
        _edges.Add(new Edge(start, end, 0, isBiDir));
    }
}
public class Graph
{
    private Dictionary<int, Node> nodes;
    private Dictionary<int, (Vector3, Vector3)> mazeCells;

    public Vector3 GetCoordinates(int index)
    {
        if (!mazeCells.TryGetValue(index, out (Vector3, Vector3) a))
            return Vector3.zero;
       
        return RectangleCenter(a);
    }
    //create graph withing rectangle given
    public Graph(Vector3 topLeft, Vector3 bottomRight)
    {
        int offsetIndex = 0;
        CalculateGridWithinRectangle(new Vector3(1, 0, 1), topLeft, bottomRight, out nodes, out mazeCells, ref offsetIndex);
    }
    //TODO this ctr is bad because I put whole maze table here, not only floor coordinates
    public Graph(List<int> maze, List<(Vector3, Vector3)> cellCoord, int mazeRows, int mazeColumns)
    {
        nodes = new Dictionary<int, Node>(); //new List<Node>();
        List<Edge> candidates = new List<Edge>();
        mazeCells = new Dictionary<int, (Vector3, Vector3)>();

        for(int i = 0; i < mazeRows; i++)
        {
            for(int j = 0; j< mazeColumns; j++)
            {
                int currentIndex = i * mazeColumns + j;
                if (maze[currentIndex] == 0)
                {
                    mazeCells.Add(currentIndex, cellCoord[currentIndex]);
                    int lastInRowIndex = (i + 1) * mazeColumns - 2;
                    int firstInRowIndex = i * mazeColumns + 1;
                    
                    //check right 
                    if (lastInRowIndex != currentIndex && maze[currentIndex + 1] == 0)
                        candidates.Add(new Edge(currentIndex, currentIndex + 1, Edge.Direction.E, true));

                    //check bottom
                    if (i != mazeRows-1 && maze[(i+1) * mazeColumns + j] == 0)
                        candidates.Add(new Edge(currentIndex, (i + 1) * mazeColumns + j, Edge.Direction.S, true));

                    //check left
                    if (firstInRowIndex != currentIndex && maze[currentIndex - 1] == 0)
                        candidates.Add(new Edge(currentIndex, currentIndex - 1, Edge.Direction.W, true));

                    //check top
                    if (i != 0 && maze[(i - 1) * mazeColumns + j] == 0)
                        candidates.Add(new Edge(currentIndex, (i - 1) * mazeColumns + j, Edge.Direction.N, true));

                    nodes.Add(currentIndex, new Node(candidates.ToArray()));
                    candidates.Clear();
                }
            }
        }
    }
    public Graph()
    {
        nodes = new Dictionary<int, Node>();
        mazeCells = new Dictionary<int, (Vector3, Vector3)>();
    }
    public void AddNode(int index, Node node, Vector3 topLeft, Vector3 botRight)
    {
        nodes.Add(index, node);
        mazeCells.Add(index, (topLeft, botRight));
    }
    
    //go only where center is
    private int[] AStar(int start, int end)
    {
        Dictionary<int, float> open = new Dictionary<int, float>(); // <index, f>
        List<int> closed = new List<int>();
        List<float> path = new List<float>();
        List<float[]> avWays = new List<float[]>();
        int currentIndex = start, closedNeighbourIndex, lowestNodeIndex;
        float f = 0, h = 0, g = 0, potF = 0;
        Edge[] neighbors;

        int findClosedNeighbour(int index)
        {
            int value = -1;
            nodes.TryGetValue(index, out Node node);
            var avNeighbours = node.GetAvailableEdges();
            if (avNeighbours.Length == 0)
                return 1;
                foreach (var item in avNeighbours)
                if (closed.Contains(item.end))
                {
                    value = item.end;
                    break;
                }
            return value;
        }

        mazeCells.TryGetValue(end, out (Vector3, Vector3) endPos);
        //first element of path is F of this path
        path.Add(0); path.Add(start); avWays.Add(path.ToArray());
        closed.Add(start);

        while(currentIndex != end)
        {
            nodes.TryGetValue(System.Convert.ToInt32(path[path.Count - 1]), out Node node);
            neighbors = node.GetAvailableEdges();
            if (neighbors.Length == 0) 
                return new int[] { };
            
            foreach (var edge in neighbors)
            {
                if (!closed.Contains(edge.end) && !open.ContainsKey(edge.end))
                {
                    mazeCells.TryGetValue(edge.end, out (Vector3, Vector3) pos);
                    open.Add(edge.end, Vector3.Distance(RectangleCenter(endPos), RectangleCenter(pos)));
                }
            }

            lowestNodeIndex = GetLowestFNode(out potF, in open);

            closedNeighbourIndex = findClosedNeighbour(lowestNodeIndex);

            for (int i = 0; i < avWays.Count; i++)
            {
                if (avWays[i][avWays[i].Length - 1] == closedNeighbourIndex)
                {
                    path = new List<float>(avWays[i]);
                    break;
                }
            }

            closed.Add(lowestNodeIndex);
            open.Remove(lowestNodeIndex);
            path.Add(lowestNodeIndex);
            avWays.Add(path.ToArray());

            path[0] += potF;
            currentIndex = lowestNodeIndex;
        }

        int[] goodPath = new int[path.Count - 1];
        for (int i = 1; i < path.Count; i++)
            goodPath[i - 1] = System.Convert.ToInt32(path[i]);
        return goodPath;
    }
    //more precisive algorithm that uses AStart(int, int)
    public Vector3[] AStar(float modelWidth, float modelLength, Vector3 startPoint, Vector3 endPoint)
    {
        Graph graph = new Graph();
        List<int> index;
        List<int> previousIndexesLink = new List<int>();
        List<int> currentIndexesLink = new List<int>();
        Edge.Direction currentDirection = 0;
        int offsetIndex = 0;
        int start=-1, end=-1;

        foreach (KeyValuePair<int, (Vector3, Vector3)> item in mazeCells)
        {
            if (start >= 0 && end >= 0) break;
            if (start == -1 && IsPointInideRectangle(startPoint, item.Value.Item1, item.Value.Item2))
                start = item.Key;
            else if (end == -1 && IsPointInideRectangle(endPoint, item.Value.Item1, item.Value.Item2))
                end = item.Key;
        }

        int[] bigWay = AStar(start, end);

        for (int i = 0; i < bigWay.Length; i++)
        {
            nodes.TryGetValue(bigWay[i], out Node bigNode);
            mazeCells.TryGetValue(bigWay[i], out (Vector3, Vector3) cell);

            CalculateGridWithinRectangle(new Vector3(modelLength, 0, modelWidth), cell.Item1, cell.Item2, out Dictionary<int, Node> createdNodes, out Dictionary<int, (Vector3, Vector3)> nodesCoords, ref offsetIndex);

            int xCount = Mathf.RoundToInt((cell.Item1.x - cell.Item2.x) / modelLength);
            xCount = xCount < 0 ? xCount * (-1) : xCount;

            foreach (KeyValuePair<int, Node> pair in createdNodes)
            {
                nodesCoords.TryGetValue(pair.Key, out (Vector3, Vector3) pos);
                graph.AddNode(pair.Key, pair.Value, pos.Item1, pos.Item2);

                if (IsPointInideRectangle(startPoint, pos.Item1, pos.Item2))
                    start = pair.Key;
                if (IsPointInideRectangle(endPoint, pos.Item1, pos.Item2))
                    end = pair.Key;
            }

            index = new List<int>(createdNodes.Keys);
            if (previousIndexesLink.Count > 0)
            {
                //currentIndexesLink = FindIndexesToLink(currentIndexesArray, currentDirection);
                currentIndexesLink = FindIndexesToLink(index, xCount, currentDirection);
                for (int k = 0; k < previousIndexesLink.Count && currentIndexesLink.Count > 0; k++)
                {
                    if (k - 1 >= 0 && k - 1 <= currentIndexesLink.Count - 1)
                        graph.LinkNodes(previousIndexesLink[k], currentIndexesLink[k - 1]);
                    if (k <= currentIndexesLink.Count - 1)
                        graph.LinkNodes(previousIndexesLink[k], currentIndexesLink[k]);
                    if (k + 1 <= currentIndexesLink.Count - 1)
                        graph.LinkNodes(previousIndexesLink[k], currentIndexesLink[k + 1]);
                }
            }

            //find linkage indexes
            if (i + 1 < bigWay.Length)
            {
                previousIndexesLink = FindIndexesToLink(index, xCount, currentDirection = bigNode.FindEdge(bigWay[i + 1]).linkDirection);

                if (currentDirection == Edge.Direction.N)
                    currentDirection = Edge.Direction.S;
                else if (currentDirection == Edge.Direction.S)
                    currentDirection = Edge.Direction.N;
                else if (currentDirection == Edge.Direction.W)
                    currentDirection = Edge.Direction.E;
                else if (currentDirection == Edge.Direction.E)
                    currentDirection = Edge.Direction.W;
                else if (currentDirection == Edge.Direction.NE)
                    currentDirection = Edge.Direction.SW;
                else if (currentDirection == Edge.Direction.SE)
                    currentDirection = Edge.Direction.WN;
                else if (currentDirection == Edge.Direction.SW)
                    currentDirection = Edge.Direction.NE;
                else if (currentDirection == Edge.Direction.WN)
                    currentDirection = Edge.Direction.SE; 
            }
        }

        List<Vector3> retValue = new List<Vector3>();
        int[] asd = graph.AStar(start, end);
        for(int i =0; i<asd.Length; i++)
        {
            retValue.Add(graph.GetCoordinates(asd[i]));
        }

        return retValue.ToArray();
    }
    private void LinkNodes(int indexNode1, int indexNode2, bool? linkNode1ToNode2Only = null)
    {
        nodes.TryGetValue(indexNode1, out Node node1);
        nodes.TryGetValue(indexNode2, out Node node2);

        node1.AddEdge(indexNode1, indexNode2, linkNode1ToNode2Only.HasValue ? linkNode1ToNode2Only.Value : false);
        node2.AddEdge(indexNode2, indexNode1, linkNode1ToNode2Only.HasValue ? linkNode1ToNode2Only.Value : false);

    }
    private List<int> FindIndexesToLink(List<int> index, int rows, Edge.Direction direction)
    {
        int cols = index.Count / rows;
        List<int> retList = new List<int>();

        switch (direction)
        {
            case Edge.Direction.N:
                for (int j = 0; j < cols; j++)
                    retList.Add(index[j]);
                break;

            case Edge.Direction.E:
                for (int i = 0; i < rows; i++)
                    retList.Add(index[cols * (i + 1) - 1]);
                break;

            case Edge.Direction.S:
                for (int j = 0; j < cols; j++)
                    retList.Add(index[cols * (rows - 1) + j]);
                break;

            case Edge.Direction.W:
                for (int i = 0; i < rows; i++)
                    retList.Add(index[i * cols]);
                break;

            case Edge.Direction.NE:
                retList.Add(index[cols - 1]);
                break;

            case Edge.Direction.SE:
                retList.Add(index[cols * rows - 1]);
                break;

            case Edge.Direction.SW:
                retList.Add(index[cols * (rows - 1)]);
                break;

            case Edge.Direction.WN:
                retList.Add(index[0]);
                break;
        }
        return retList;
    }
    private Vector3 RectangleCenter((Vector3, Vector3) coords)
    {
        return new Vector3((coords.Item1.x + coords.Item2.x) / 2, (coords.Item1.y + coords.Item2.y) / 2, (coords.Item1.z + coords.Item2.z) / 2);
    }
    private (Vector3, Vector3) RectangleTopLeftBotRightCorners(Vector3 center, Vector3 modelDimensions)
    {
        return (new Vector3(center.x - modelDimensions.x / 2, 0, center.z - modelDimensions.z / 2), new Vector3(center.x + modelDimensions.x / 2, 0, center.z + modelDimensions.z / 2));
    }
    private int GetLowestFNode(out float minF, in Dictionary<int, float> open)
    {
        int index = -1;
        minF = -1f;

        foreach (var item in open)
        {
            minF = item.Value;
            index = item.Key;
            break;
        }

        //check all open vertices for minF value
        foreach (KeyValuePair<int, float> item in open)
            if (item.Value < minF)
            {
                minF = item.Value;
                index = item.Key;
            }
        return index;
    }
    private void CalculateGridWithinRectangle(Vector3 modelDimensions, Vector3 topLeft, Vector3 bottomRight, out Dictionary<int, Node> nodes, out Dictionary<int, (Vector3, Vector3)> mazeCells, ref int offsetIndex)
    {
        (Vector3, Vector3) vector;
        List<Edge> neighbors = new List<Edge>();
        Edge edge;
        Node node;
        int currentIndex = offsetIndex;

        int zCount = Mathf.RoundToInt((topLeft.z - bottomRight.z) / modelDimensions.z);
        int xCount = Mathf.RoundToInt((topLeft.x - bottomRight.x) / modelDimensions.x);

        nodes = new Dictionary<int, Node>();
        mazeCells = new Dictionary<int, (Vector3, Vector3)>();

        zCount = zCount < 0 ? zCount * (-1) : zCount;
        xCount = xCount < 0 ? xCount * (-1) : xCount;

        for (int x = 0; x < xCount; x++)
            for (int z = 0; z < zCount; z++, currentIndex++)
            {
                vector = (new Vector3(topLeft.x + modelDimensions.x * x, 0, topLeft.z + modelDimensions.z * z), new Vector3(topLeft.x + modelDimensions.x * (x + 1), 0, topLeft.z + modelDimensions.z * (z + 1)));

                //west
                if (z - 1 >= 0)
                {
                    edge = new Edge(currentIndex, (x * zCount) + z - 1 + offsetIndex, Edge.Direction.W, true);
                    neighbors.Add(edge);
                }
                //north-west
                if (x - 1 >= 0 && z - 1 >= 0)
                {
                    edge = new Edge(currentIndex, (x - 1) * zCount + z - 1 + offsetIndex, Edge.Direction.WN, true);
                    neighbors.Add(edge);
                }

                //north
                if (x - 1 >= 0)
                {
                    edge = new Edge(currentIndex, (x - 1) * zCount + z + offsetIndex, Edge.Direction.N, true);
                    neighbors.Add(edge);
                }

                //north-east
                if (x - 1 >= 0 && z + 1 < zCount)
                {
                    edge = new Edge(currentIndex, (x - 1) * zCount + z + 1 + offsetIndex, Edge.Direction.NE, true);
                    neighbors.Add(edge);
                }

                //east
                if (z + 1 < zCount)
                {
                    edge = new Edge(currentIndex, (x * zCount) + z + 1 + offsetIndex, Edge.Direction.E, true);
                    neighbors.Add(edge);
                }

                //south-east
                if (z + 1 < zCount && x + 1 < xCount)
                {
                    edge = new Edge(currentIndex, (x + 1) * zCount + z + 1 + offsetIndex, Edge.Direction.SE, true);
                    neighbors.Add(edge);
                }

                //south
                if (x + 1 < xCount)
                {
                    edge = new Edge(currentIndex, (x + 1) * zCount + z + offsetIndex, Edge.Direction.S, true);
                    neighbors.Add(edge);
                }

                //south-west
                if (x + 1 < xCount && z - 1 >= 0)
                {
                    edge = new Edge(currentIndex, (x + 1) * zCount + z - 1 + offsetIndex, Edge.Direction.SW, true);
                    neighbors.Add(edge);
                }

                node = new Node(neighbors.ToArray());
                nodes.Add(currentIndex, node);
                mazeCells.Add(currentIndex, vector);
                neighbors.Clear();
            }

        offsetIndex += xCount * zCount;
    }
    private bool IsPointInideRectangle(Vector3 point, Vector3 topLeft, Vector3 botRight)
    {
        bool value = false;

        if (point.x >= topLeft.x - 0.03f  && point.x <= botRight.x + 0.03f &&
            point.z >= topLeft.z - 0.03f && point.z <= botRight.z + 0.03f)
            value = true;

        return value;
    }

    #region AStarFullAfterOptimization
    //public float[] AStarFull(int start, int end)
    //{
    //    Dictionary<int, float> open = new Dictionary<int, float>(); // <index, f>
    //    List<int> closed = new List<int>();
    //    List<float> path = new List<float>();
    //    List<float[]> avWays = new List<float[]>();
    //    int currentIndex = start, closedNeighbourIndex = -1, lowestNodeIndex = 0;
    //    float f = 0, h = 0, g = 0, potF = 0;
    //    Edge[] neighbors;
    //
    //    int findClosedNeighbour(int index)
    //    {
    //        int value = -1;
    //        var avNeighbours = nodes.Find(node => node.GetNodeNumber().Equals(index)).GetAvailableEdges();
    //        foreach (var item in avNeighbours)
    //            if (closed.Contains(item.end))
    //            {
    //                value = item.end;
    //                break;
    //            }
    //        return value;
    //    }
    //
    //    mazeCells.TryGetValue(end, out (Vector3, Vector3) endPos);
    //    //first element of path is F of this path
    //    path.Add(0); path.Add(start); avWays.Add(path.ToArray());
    //    closed.Add(start);
    //
    //    neighbors = nodes.Find(w => w.GetNodeNumber().Equals(path[path.Count - 1])).GetAvailableEdges();
    //
    //    foreach (var edge in neighbors)
    //    {
    //        if (!closed.Contains(edge.end) && !open.ContainsKey(edge.end))
    //        {
    //            mazeCells.TryGetValue(edge.end, out (Vector3, Vector3) pos);
    //            open.Add(edge.end, Vector3.Distance(RectangleCenter(endPos), RectangleCenter(pos)));
    //        }
    //    }
    //
    //    do
    //    {
    //        //find node with open neighbors
    //        if (currentIndex == end)
    //        {
    //            foreach (int item in open.Keys)
    //            {
    //                currentIndex = findClosedNeighbour(item);
    //                break;
    //
    //            }
    //        }
    //
    //
    //        //neighbors = nodes.Find(w => w.GetNodeNumber().Equals(path[path.Count - 1])).GetAvailableEdges();
    //        ////if (neighbors.Length == 0) return avWays[0];
    //        //
    //        //foreach (var edge in neighbors)
    //        //{
    //        //    if (!closed.Contains(edge.end) && !open.ContainsKey(edge.end))
    //        //    {
    //        //        mazeCells.TryGetValue(edge.end, out Vector3 pos);
    //        //        open.Add(edge.end, Vector3.Distance(endPos, pos));
    //        //    }
    //        //}
    //
    //        lowestNodeIndex = GetLowestFNode(out potF, in open);
    //
    //        closedNeighbourIndex = findClosedNeighbour(lowestNodeIndex);
    //
    //        for (int i = 0; i < avWays.Count; i++)
    //        {
    //            if (avWays[i][avWays[i].Length - 1] == closedNeighbourIndex)
    //            {
    //                path = new List<float>(avWays[i]);
    //                break;
    //            }
    //
    //        }
    //
    //        closed.Add(lowestNodeIndex);
    //        open.Remove(lowestNodeIndex);
    //        path.Add(lowestNodeIndex);
    //        avWays.Add(path.ToArray());
    //
    //        path[0] += potF;
    //        currentIndex = lowestNodeIndex;
    //        closedNeighbourIndex = -1;
    //
    //        neighbors = nodes.Find(w => w.GetNodeNumber().Equals(path[path.Count - 1])).GetAvailableEdges();
    //        //if (neighbors.Length == 0) return avWays[0];
    //
    //        foreach (var edge in neighbors)
    //        {
    //            if (!closed.Contains(edge.end) && !open.ContainsKey(edge.end))
    //            {
    //                mazeCells.TryGetValue(edge.end, out (Vector3, Vector3) pos);
    //                open.Add(edge.end, Vector3.Distance(RectangleCenter(endPos), RectangleCenter(pos)));
    //            }
    //        }
    //
    //    } while (open.Count != 0);
    //
    //    int indexMin = 0;
    //    int lengthMin = start * start;
    //    for (int i = 0; i < avWays.Count; i++)
    //        if (avWays[i].Length < lengthMin && avWays[i][avWays[i].Length - 1] == end)
    //        {
    //            lengthMin = avWays[i].Length;
    //            indexMin = i;
    //        }
    //
    //    return avWays[indexMin];
    //}
    #endregion
}
