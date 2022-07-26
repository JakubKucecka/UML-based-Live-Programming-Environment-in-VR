using UnityEngine;
using Microsoft.Msagl.Miscellaneous;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Core.Routing;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Msagl.Core.DataStructures;
using Microsoft.CodeAnalysis;
using RoslynCSharp.Compiler;
using UnityEngine.UI.Extensions;
using System.IO;
using UnityEngine.UI;
using System;

public class Graph : MonoBehaviour
{
    public GameObject nodePrefab;
    public GameObject edgePrefab;
    public float factor = 0.2f;

    private GeometryGraph graph;
    private LayoutAlgorithmSettings settings;

    private Task router;
    private bool reroute = false;
    private bool redraw = true;

    private Transform units;

    public List<GNode> nodes = new List<GNode>();
    public List<GEdge> edges = new List<GEdge>();

    public int lastEdgeId = 0;
    public int lastNodeId = 0;

    public SyntaxTree[] syntaxTrees;

    public string teporaryProjectPath = "/TemporaryProject/";

    public List<string> NodeTypes = new List<string>();
    public List<string> EdgeTypes = new List<string>();

    OVRPassthroughLayer passthroughLayer;
    public float textureOpacity;

    private GameObject graphFromPrefab;

    // octobubbles - asociacia, inheritance, agregation, compositions, realisations
    private void Start()
    {
        GameObject ovrCameraRig = GameObject.Find("OVRCameraRig");
        if (ovrCameraRig != null) passthroughLayer = ovrCameraRig.GetComponent<OVRPassthroughLayer>();
        teporaryProjectPath = Application.persistentDataPath + teporaryProjectPath;

        if (!Directory.Exists(teporaryProjectPath))
        {
            Directory.CreateDirectory(teporaryProjectPath);
        }

        NodeType nt = new NodeType();
        NodeTypes.Add(nt.publicType);
        NodeTypes.Add(nt.privateType);
        NodeTypes.Add(nt.protectedType);
        NodeTypes.Add(nt.internalType);
        NodeTypes.Add(nt.interfaceType);

        EdgeType et = new EdgeType();
        EdgeTypes.Add(et.association);
        EdgeTypes.Add(et.dependency);
        EdgeTypes.Add(et.aggregation);
        EdgeTypes.Add(et.composition);
        EdgeTypes.Add(et.realisation);
        EdgeTypes.Add(et.generalization);

        graphFromPrefab = GameObject.FindGameObjectWithTag("Graph");
    }

    public void generateSyntaxTree(string[] files)
    {
        syntaxTrees = RoslynCSharpCompiler.ParseFiles(files, null);
    }

    public Transform Units
    {
        get
        {
            return units;
        }
    }

    public void Center()
    {
        graph.UpdateBoundingBox();
        units.localPosition = new Vector3(ToUnitySpace(graph.BoundingBox.Center.X), ToUnitySpace(graph.BoundingBox.Center.Y)) * -0.6f;
    }

    public void Layout()
    {
        if (graphFromPrefab != null)
        {
            graphFromPrefab.transform.localScale = new Vector3(0.003f, 0.003f, 1);
            graphFromPrefab.transform.localPosition = new Vector3(0, -1.8f, 0);
        }

        LayoutHelpers.CalculateLayout(graph, settings, null);

        PositionNodes();
        RedrawEdges();
        Center();
    }

    public GameObject AddNode()
    {
        var go = GameObject.Instantiate(nodePrefab, units);
        //go.transform.SetParent(transform);

        AddNode(go);

        return go;
    }

    public void AddNode(GameObject go)
    {
        Canvas.ForceUpdateCanvases();

        var unode = go.GetComponent<UNode>();
        double w = ToGraphSpace(unode.Size.width);
        double h = ToGraphSpace(unode.Size.height);

        Node node = new Node(CurveFactory.CreateRectangle(w, h, new Point()));
        node.UserData = go;
        unode.GraphNode = node;
        graph.Nodes.Add(node);
    }

    public void RemoveNode(GameObject node)
    {
        var graphNode = node.GetComponent<UNode>().GraphNode;
        foreach (var edge in graphNode.Edges)
        {
            GameObject.Destroy((GameObject)edge.UserData);
            //in MSAGL edges are automatically removed, only UnityObjects have to be removed
        }
        graph.Nodes.Remove(graphNode);
        GameObject.Destroy(node);
    }

    public GameObject AddEdge(GameObject from, GameObject to)
    {
        var go = GameObject.Instantiate(edgePrefab, units);
        //go.transform.SetParent(transform);

        AddEdge(go, from, to);

        return go;
    }

    public void AddEdge(GameObject go, GameObject from, GameObject to)
    {
        var uEdge = go.GetComponent<UEdge>();

        Edge edge = new Edge(from.GetComponent<UNode>().GraphNode, to.GetComponent<UNode>().GraphNode);
        edge.LineWidth = ToGraphSpace(uEdge.Width);
        edge.UserData = go;
        uEdge.graphEdge = edge;
        uEdge.GetComponentsInChildren<Text>()[0].text = "1";
        uEdge.GetComponentsInChildren<Text>()[1].text = "2";
        graph.Edges.Add(edge);
    }

    public void RemoveEdge(GameObject edge)
    {
        graph.Edges.Remove(edge.GetComponent<UEdge>().graphEdge);
        GameObject.Destroy(edge);
    }

    public double ToGraphSpace(float x)
    {
        return x / factor;
    }

    public float ToUnitySpace(double x)
    {
        return (float)x * factor;
    }

    void Awake()
    {
        graph = new GeometryGraph();
        units = transform.Find("Units"); //extra object to center graph
        units.transform.SetParent(transform);
        settings = new SugiyamaLayoutSettings();
        settings.EdgeRoutingSettings.EdgeRoutingMode = EdgeRoutingMode.RectilinearToCenter;
    }

    void PositionNodes()
    {
        foreach (var node in graph.Nodes)
        {
            var go = (GameObject)node.UserData;
            go.transform.localPosition = new Vector3(ToUnitySpace(node.Center.X), ToUnitySpace(node.Center.Y), transform.localPosition.z);
        }
    }

    void UpdateNodes()
    {
        foreach (var node in graph.Nodes)
        {
            var go = (GameObject)node.UserData;
            node.Center = new Point(ToGraphSpace(go.transform.localPosition.x), ToGraphSpace(go.transform.localPosition.y));
            var unode = go.GetComponent<UNode>();
            node.BoundingBox = new Rectangle(new Size(ToGraphSpace(unode.Size.width), ToGraphSpace(unode.Size.height)), node.Center);
        }
    }

    public void RedrawEdges()
    {
        foreach (var edge in graph.Edges)
        {
            List<Vector2> pointList = new List<Vector2>();
            GameObject go = (GameObject)edge.UserData;

            Curve curve = edge.Curve as Curve;
            if (curve != null)
            {
                Point p = curve[curve.ParStart];
                pointList.Add(new Vector2(ToUnitySpace(p.X), ToUnitySpace(p.Y)));
                foreach (ICurve seg in curve.Segments)
                {
                    p = seg[seg.ParEnd];
                    pointList.Add(new Vector2(ToUnitySpace(p.X), ToUnitySpace(p.Y)));
                }
            }
            else
            {
                LineSegment ls = edge.Curve as LineSegment;
                if (ls != null)
                {
                    Point p = ls.Start;
                    pointList.Add(new Vector2(ToUnitySpace(p.X), ToUnitySpace(p.Y)));
                    p = ls.End;
                    pointList.Add(new Vector2(ToUnitySpace(p.X), ToUnitySpace(p.Y)));
                }
            }

            var lineRenderer = go.GetComponent<UILineRenderer>();
            lineRenderer.Points = pointList.ToArray();

            var texts = go.GetComponentsInChildren<Text>();
            if (texts.Length > 0)
            {
                texts[0].transform.localPosition = getMultyPosition(pointList[1], pointList[0], texts[0].transform.localPosition.z);
                texts[1].transform.localPosition = getMultyPosition(pointList[pointList.Count - 2], pointList[pointList.Count - 1], texts[1].transform.localPosition.z);
            }

            var image = go.GetComponentInChildren<Image>();
            if (image != null)
            {
                Tuple<Vector3, Quaternion> newTransofrm = getArrowTransform(pointList[pointList.Count - 2], pointList[pointList.Count - 1], image.transform.localPosition.z, image.transform.localRotation);
                image.transform.localPosition = newTransofrm.Item1;
                image.transform.localRotation = newTransofrm.Item2;
            }
        }
    }

    Vector3 getMultyPosition(Vector2 firstPoint, Vector2 secondPoint, float zPosition)
    {
        float margin = 30;
        Vector3 newPosition = new Vector3(secondPoint.x, secondPoint.y, zPosition);

        if (firstPoint.x < secondPoint.x)
        {
            newPosition.x -= margin / 2;
            newPosition.y += margin;
        }

        if (firstPoint.x > secondPoint.x)
        {
            newPosition.x += margin / 2;
            newPosition.y += margin;
        }

        if (firstPoint.y < secondPoint.y)
        {
            newPosition.x += margin / 2;
            newPosition.y -= margin;
        }

        if (firstPoint.y > secondPoint.y)
        {
            newPosition.x += margin / 2;
            newPosition.y += margin;
        }

        return newPosition;
    }

    Tuple<Vector3, Quaternion> getArrowTransform(Vector2 firstPoint, Vector2 secondPoint, float zPosition, Quaternion newRotation)
    {
        float margin = 10;
        Vector3 newPosition = new Vector3(secondPoint.x, secondPoint.y, zPosition);

        if (firstPoint.y < secondPoint.y)
        {
            newPosition.y -= margin;
            newRotation = Quaternion.Euler(0, 0, 0);
        }

        if (firstPoint.y > secondPoint.y)
        {
            newPosition.y += margin;
            newRotation = Quaternion.Euler(0, 0, 180);
        }

        if (firstPoint.x > secondPoint.x)
        {
            newPosition.x += margin;
            newRotation = Quaternion.Euler(0, 0, 90);
        }

        if (firstPoint.x < secondPoint.x)
        {
            newPosition.x -= margin;
            newRotation = Quaternion.Euler(0, 0, 270);
        }

        return new Tuple<Vector3, Quaternion>(newPosition, newRotation);
    }

    private async void ForgetRouter(Task t)
    {
        await t;
        router = null;
        redraw = true;
    }

    public void UpdateGraph()
    {
        reroute = true;
    }

    public void Update()
    {
        if (passthroughLayer != null) passthroughLayer.textureOpacity = textureOpacity;

        if (reroute)
        {
            if (router == null && graph.Edges.Count != 0)
            {
                UpdateNodes();
                router = Task.Run(() => LayoutHelpers.RouteAndLabelEdges(graph, settings, graph.Edges));
                ForgetRouter(router);
                reroute = false;
            }
        }
        if (redraw)
        {
            RedrawEdges();
            redraw = false;
        }
    }

    public GNode getNodeById(int i)
    {
        foreach (GNode n in nodes)
        {
            if (n.ID == i) return n;
        }
        return null;
    }

    public GEdge getEdgeById(int i)
    {
        foreach (GEdge e in edges)
        {
            if (e.ID == i) return e;
        }
        return null;
    }

    public GNode getNodeByName(string i)
    {
        foreach (GNode n in nodes)
        {
            if (n.Nname == i) return n;
        }
        return null;
    }
}
