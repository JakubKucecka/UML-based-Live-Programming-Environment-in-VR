using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynCSharp.Compiler;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class HomeController : MonoBehaviour
{
    public Canvas tabletCanvas;
    private Canvas graphCanvas;

    public GameObject player;
    private Graph graph;
    public Shader shader;

    private EdgeType et = new EdgeType();

    private Dictionary<string, List<string[]>> tmpEdges = new Dictionary<string, List<string[]>>();
    public Transform graphFromPrefabTransform;
    private GameObject graphFromPrefab;

    public GameObject errorObject;
    public GameObject VRTablet;

    private void Start()
    {
        graphCanvas = tabletCanvas.GetComponent<TabletHandler>().graphCanvas;
        graph = graphCanvas.GetComponentInChildren<Graph>();
        graphFromPrefab = GameObject.FindGameObjectWithTag("Graph");
    }

    private void Update()
    {
        if (graph == null || graphFromPrefab == null)
        {
            Debug.LogWarning("Missing graph");
            graph = graphCanvas.GetComponentInChildren<Graph>();
            graphFromPrefab = GameObject.FindGameObjectWithTag("Graph");
        }

        if (OVRInput.GetDown(OVRInput.RawButton.B) || Input.GetKeyDown(KeyCode.F3))
        {
            showVRTablet();
        }
    }

    public void ZoomCanvas()
    {
        graphFromPrefab.transform.localScale += new Vector3(0.0002f, 0.0002f, 0);
        //if (graphCanvas.transform.position.z > -10)
        //    graphCanvas.transform.Translate(0, 0, -0.5f);
    }

    public void UnZoomCanvas()
    {
        graphFromPrefab.transform.localScale -= new Vector3(0.0002f, 0.0002f, 0);
        //if (graphCanvas.transform.position.z < 10)
        //    graphCanvas.transform.Translate(0, 0, 0.5f);
    }

    public void UpCanvas()
    {
        //if (graphCanvas.transform.position.y > -30)
        //    graphCanvas.transform.Translate(0, -2f, 0);
        graphFromPrefab.transform.localPosition -= new Vector3(0, 0.2f, 0);
    }

    public void DownCanvas()
    {
        //if (graphCanvas.transform.position.y < 30)
        //    graphCanvas.transform.Translate(0, 2f, 0);
        graphFromPrefab.transform.localPosition += new Vector3(0, 0.2f, 0);
    }

    public void LeftCanvas()
    {
        //if (player.transform.position.x > -60)
        //{
        //    player.transform.Rotate(0, -0.5f, 0);
        //    player.transform.Translate(-5f, 0, 0);
        //}
        graphFromPrefab.transform.localPosition += new Vector3(0.2f, 0, 0);
    }

    public void RightCanvas()
    {
        //if (player.transform.position.x < 60)
        //{
        //    player.transform.Rotate(0, 0.5f, 0);
        //    player.transform.Translate(5f, 0, 0);
        //}
        graphFromPrefab.transform.localPosition -= new Vector3(0.2f, 0, 0);
    }

    public void SaveGraph()
    {
        var jsonData = new JsonData();
        var nodeList = new List<JsonNode>();
        var edgeList = new List<JsonEdge>();
        //var graphCanvasPosition = graphCanvas.transform.position;

        foreach (var node in graph.nodes)
        {
            var position = node.go.transform.position;

            var aList = new List<JsonAtribute>();
            foreach (var a in node.attributes)
            {
                aList.Add(new JsonAtribute(a[0], a[1]));
            }

            var oList = new List<JsonAtribute>();
            foreach (var o in node.opperation)
            {
                oList.Add(new JsonAtribute(o[0], o[1]));
            }

            nodeList.Add(new JsonNode(position.x, position.y, position.z, node.ID, node.Nname, node.type, node.namespaceName, aList.ToArray(), oList.ToArray(), node.path));
        }

        foreach (var edge in graph.edges)
        {
            edgeList.Add(new JsonEdge(edge.ID, edge.type, edge.from.ID, edge.to.ID, edge.fromMulti, edge.toMulti));
        }

        jsonData.nodes = nodeList.ToArray();
        jsonData.edges = edgeList.ToArray();

        string json = JsonUtility.ToJson(jsonData);
        File.WriteAllText(graph.teporaryProjectPath + "graph.json", json);
    }

    public string[] getAllCSFiles(string dirName)
    {
        List<string> files = new List<string>();
        if (!Directory.Exists(dirName))
        {
            return new string[] { };
        }

        string extension = ".cs";

        void addToFilePaths(string dirName)
        {
            string[] filePaths = Directory.GetFiles(dirName);

            foreach (string path in filePaths)
            {
                if (File.Exists(path) && path.Substring(path.Length - extension.Length) == extension)
                {
                    files.Add(path);
                }
            }

            string[] dirPaths = Directory.GetDirectories(dirName);
            foreach (string path in dirPaths)
            {
                if (Directory.Exists(path))
                {
                    addToFilePaths(path);
                }
            }
        }

        addToFilePaths(dirName);

        return files.ToArray();
    }

    public bool isInTmpEdges(string fromName)
    {
        return tmpEdges.ContainsKey(fromName);
    }

    public void insertIntoTmpEdges(string from, string to, string type)
    {
        if (!tmpEdges.ContainsKey(from))
        {
            tmpEdges[from] = new List<string[]>();
        }
        tmpEdges[from].Add(new string[] { to, type });
    }

    public List<string[]> addToRelationships(List<string[]> relationships, string[] newRel)
    {
        if (newRel.Length == 2 && newRel[0].Length > 0 && newRel[1].Length > 0)
        {
            if (!relationships.Contains(newRel))
            {
                relationships.Add(newRel);
            }
        }

        return relationships;
    }

    public string parseClassOrInterfaceDeclaration(TypeDeclarationSyntax classDeclaration, string filePath, GNode gNode, string namespaceName)
    {
        graph.lastNodeId += 1;

        JsonNode jn = new JsonNode();

        if (gNode != null)
        {
            jn.name = gNode.Nname;
            jn.namespaceName = gNode.namespaceName;
            jn.path = gNode.path;
            jn.ID = gNode.ID;
        }
        else
        {
            jn.name = classDeclaration.Identifier.ToString();
            jn.path = filePath;
            jn.ID = graph.lastNodeId;
            jn.namespaceName = namespaceName;
        }

        jn.type = classDeclaration.IsKind(SyntaxKind.InterfaceDeclaration) ? "interface" : classDeclaration.Modifiers[0].ToString();

        var startIndex = classDeclaration.ToFullString().IndexOf('{');
        if (startIndex < 0) startIndex = classDeclaration.ToFullString().Length;
        var fullDeclaration = classDeclaration.ToFullString().Remove(startIndex);
        var declarationArray = fullDeclaration.Split(':');
        if (declarationArray.Length > 1)
        {
            var elementArray = declarationArray[1].Replace("\n", "").Replace("\r", "").Replace(" ", "").Split(',');
            foreach (var i in elementArray)
            {
                insertIntoTmpEdges(jn.name, i, "ROG");
            }
        }

        Tuple<List<string[]>, List<JsonAtribute>, List<JsonAtribute>> tuple = getClassMembers(classDeclaration, jn.name, jn.namespaceName);
        var relationships = tuple.Item1;
        var aList = tuple.Item2;
        var oList = tuple.Item3;

        foreach (var rel in relationships)
        {
            insertIntoTmpEdges(jn.name, rel[0], rel[1]);
        }

        jn.attributes = aList.ToArray();
        jn.opperation = oList.ToArray();
        addNodeToGraph(jn, graph, gNode == null ? null : gNode);

        return jn.name;
    }

    public Tuple<List<string[]>, List<JsonAtribute>, List<JsonAtribute>> getClassMembers(TypeDeclarationSyntax classDeclaration, string jnName, string jnNamespace)
    {
        var aList = new List<JsonAtribute>();
        var oList = new List<JsonAtribute>();
        // Dictionary <name, Tuple <type, isAggregation> >
        var aggregation = new Dictionary<string, Tuple<string, bool>>();
        //var composition = new Dictionary<string, string>();
        var relationships = new List<string[]>();
        string[] newRel;

        foreach (var e in classDeclaration.Members)
        {
            switch (e.Kind().ToString())
            {
                case "MethodDeclaration":
                    var mElem = (MethodDeclarationSyntax)e;
                    oList.Add(new JsonAtribute(mElem.Identifier.ToString() + "()", mElem.ReturnType.ToString()));

                    newRel = new string[] { mElem.ReturnType.ToString(), et.dependency };
                    relationships = addToRelationships(relationships, newRel);

                    foreach (var param in mElem.ParameterList.Parameters)
                    {
                        newRel = new string[] { param.Type.ToString(), et.dependency };
                        relationships = addToRelationships(relationships, newRel);
                    }

                    string fullMetod = mElem.ToString();
                    MatchCollection matchesDep = Regex.Matches(fullMetod, @"\s*(\b[a-z]+\b\s+)?((\b[a-zA-Z0-9_\[\]()<>]+)\s+)?(\b[a-zA-Z0-9_]*\b)\s*(=\s*.+)?;\s*");

                    foreach (Match matchDep in matchesDep)
                    {
                        // group 1 - name
                        MatchCollection matchesAggr = Regex.Matches(matchDep.ToString(), @"^\s*(\b[a-zA-Z0-9_]*)\b\s*=\s*((?!\bnew\b).)+\s*;\s*$");
                        string methodName = mElem.Identifier.ToString();
                        if (matchesAggr.Count > 0 && (methodName == "Start" || methodName == "Awake" || methodName == jnName))
                        {
                            foreach (Match matchAggr in matchesAggr)
                            {
                                //var propType = matchAggr.Groups[2].Value;
                                var propName = matchAggr.Groups[1].Value;

                                if (aggregation.ContainsKey(propName) && aggregation[propName].Item1 != "-in_method-" && !aggregation[propName].Item2)
                                {
                                    newRel = new string[] { aggregation[propName].Item1, et.aggregation };
                                    relationships = addToRelationships(relationships, newRel);
                                    aggregation[propName] = new Tuple<string, bool>(aggregation[propName].Item1, true);
                                }
                                else if (!aggregation.ContainsKey(propName))
                                {
                                    aggregation.Add(propName, new Tuple<string, bool>("-in_method-", false));
                                }
                            }
                        }
                        else
                        {
                            //// group 2 - type, group 3 - name
                            //MatchCollection matchesComp = Regex.Matches(matchDep.ToString(), @"\s*((\b[a-zA-Z0-9_\[\]()<>]+)\s+)?(\b[a-zA-Z0-9_]*\b)\s*=\s*\bnew\b.+;\s*");
                            //if (matchesComp.Count > 0)
                            //{
                            //    foreach (Match matchComp in matchesComp)
                            //    {
                            //        var propType = matchComp.Groups[2].Value;
                            //        var propName = matchComp.Groups[3].Value;

                            //        if ((composition.ContainsKey(propName) && composition[propName] != "-in_method-") || propType.Length > 0)
                            //        {
                            //            newRel = new string[] { (composition.ContainsKey(propName) && composition[propName] != "-in_method-") ? composition[propName] : propType, et.composition };
                            //            relationships = addToRelationships(relationships, newRel);
                            //            if (!composition.ContainsKey(propName))
                            //            {
                            //                composition.Add(propName, propType);
                            //            }
                            //        }
                            //        else if (!composition.ContainsKey(propName))
                            //        {
                            //            composition.Add(propName, "-in_method-");
                            //        }
                            //    }
                            //}
                            //else
                            //{
                            newRel = new string[] { matchDep.Groups[3].Value, et.dependency };
                            relationships = addToRelationships(relationships, newRel);
                            //}
                        }
                    }

                    break;

                case "FieldDeclaration":
                    var fElem = (FieldDeclarationSyntax)e;
                    aList.Add(new JsonAtribute(fElem.Declaration.Variables[0].Identifier.ToString(), fElem.Declaration.Type.ToString()));

                    newRel = new string[] { fElem.Declaration.Type.ToString(), et.dependency };
                    relationships = addToRelationships(relationships, newRel);

                    string fullField = fElem.ToString();
                    // group 1 - privacy, group 3 - type, group 4 - name
                    MatchCollection matchesFieldDep = Regex.Matches(fullField, @"\s*(\b[a-z]+\b\s+)?((\b[a-zA-Z0-9_\[\]()<>]+)\s+)?(\b[a-zA-Z0-9_]*\b)\s*(=\s*.+)?;\s*");

                    foreach (Match matchFieldDep in matchesFieldDep)
                    {
                        // group 1 - type, group 2 - name
                        MatchCollection matchesAggr = Regex.Matches(matchFieldDep.ToString(), @"\s*\b[a-z]+\b\s+(\b[a-zA-Z0-9_\[\]()<>]+)\s+(\b[a-zA-Z0-9_]+\b)\s*;\s*");
                        if (matchesAggr.Count > 0)
                        {
                            foreach (Match matchAggr in matchesAggr)
                            {
                                var propType = matchAggr.Groups[1].Value;
                                var propName = matchAggr.Groups[2].Value;

                                if (aggregation.ContainsKey(propName) && aggregation[propName].Item1 == "-in_method-" && propType.Length > 0)
                                {
                                    newRel = new string[] { propType, et.aggregation };
                                    relationships = addToRelationships(relationships, newRel);
                                    aggregation[propName] = new Tuple<string, bool>(propType, true);
                                }
                                else if (!aggregation.ContainsKey(propName))
                                {
                                    aggregation.Add(propName, new Tuple<string, bool>(propType, false));
                                }

                                //// TODO: tymto si nie som isty ci je kompozicia
                                //if (composition.ContainsKey(propName) && composition[propName] == "-in_method-" && propType.Length > 0)
                                //{
                                //    newRel = new string[] { propType, et.composition };
                                //    relationships = addToRelationships(relationships, newRel);
                                //    composition[propName] = propType;
                                //}
                                //else if (!composition.ContainsKey(propName))
                                //{
                                //    composition.Add(propName, propType);
                                //}
                            }
                        }
                        else
                        {
                            // group 1 - type, group 2 - name
                            MatchCollection matchesFieldComp = Regex.Matches(matchFieldDep.ToString(), @"\s*\b[a-z]+\b\s+(\b[a-zA-Z0-9_\[\]()<>]+)\s+(\b[a-zA-Z0-9_]*\b)\s*=\s*\bnew\b.+;\s*");
                            if (matchesFieldComp.Count > 0)
                            {
                                foreach (Match matchComp in matchesFieldComp)
                                {
                                    var propType = matchComp.Groups[1].Value;
                                    var propName = matchComp.Groups[2].Value;

                                    newRel = new string[] { propType, et.composition };
                                    relationships = addToRelationships(relationships, newRel);
                                }
                            }
                            else
                            {
                                newRel = new string[] { matchFieldDep.Groups[3].Value, et.association };
                                relationships = addToRelationships(relationships, newRel);
                            }
                        }
                    }
                    break;

                case "ClassDeclaration":
                    var cElem = (ClassDeclarationSyntax)e;
                    string nestedClassName = parseClassOrInterfaceDeclaration(cElem, "nested", null, jnNamespace);
                    insertIntoTmpEdges(jnName, nestedClassName, et.composition);
                    break;

                default:
                    Debug.LogWarning($"\t{e.Kind()} in ClassDeclaration");
                    break;
            }
        }

        foreach (Tuple<string, bool> a in aggregation.Values)
        {
            if (!a.Item2)
            {
                newRel = new string[] { a.Item1, et.dependency };
                relationships = addToRelationships(relationships, newRel);
            }
        }

        return new Tuple<List<string[]>, List<JsonAtribute>, List<JsonAtribute>>(relationships, aList, oList);
    }

    public void addNodeToGraph(JsonNode jn, Graph graph, GNode gNode)
    {
        GameObject n;
        if (gNode != null)
        {
            n = gNode.go;
        }
        else
        {
            n = graph.AddNode();
        }

        if (jn.x != 0 || jn.y != 0 || jn.z != 0)
        {
            n.transform.position = new Vector3(jn.x, jn.y, jn.z);
        }

        var buttons = n.GetComponentsInChildren<Button>();

        foreach (Button b in buttons)
        {
            if (jn.path == "nested" && b.name != "MoveButton")
            {
                b.gameObject.SetActive(false);
            }
            else
            {
                b.gameObject.SetActive(true);
                if (b.name == "EditButton")
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(delegate { tabletCanvas.GetComponent<UpdateController>().showUpdateNode(n); });
                    continue;
                }

                if (b.name == "CodeButton")
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(delegate { tabletCanvas.GetComponent<UpdateController>().showCode(n); });
                    continue;
                }
            }
        }

        if (jn.ID > graph.lastNodeId) graph.lastNodeId = jn.ID;

        var attrStr = getStringFromArray(jn.attributes, "- ");
        var opperStr = getStringFromArray(jn.opperation, "+ ");

        if (gNode == null)
        {
            graph.nodes.Add(new GNode(jn.ID, jn.name, jn.type, jn.namespaceName, attrStr, opperStr, jn.path, n));
        }
        else
        {
            gNode.ID = jn.ID;
            gNode.Nname = jn.name;
            gNode.type = jn.type;
            gNode.namespaceName = jn.namespaceName;
            gNode.attributes = gNode.getListFromString(attrStr);
            gNode.opperation = gNode.getListFromString(opperStr);
            gNode.path = jn.path;
            gNode.go = n;
        }

        var texts = n.GetComponentsInChildren<Text>();
        foreach (Text t in texts)
        {
            if (t.name == "Header")
            {
                t.text = jn.type + " " + jn.name;
                continue;
            }

            if (t.name == "Attributes")
            {
                t.text = attrStr;
                //t.text = "...";
                continue;
            }

            if (t.name == "Opperations")
            {
                t.text = opperStr;
                //t.text = "...";
                continue;
            }
        }
    }

    public void updateEges(Graph graph)
    {
        foreach (var tmpEdge in tmpEdges)
        {
            foreach (var v in tmpEdge.Value)
            {
                List<string> variables = new List<string>();
                MatchCollection matches = Regex.Matches(v[0].Replace("/r", "").Replace("/n", "").Replace(" ", ""), @"((Dictionary|List|Array)<(.*)>|\[\])");

                void GetAllVariables(string declaration)
                {
                    declaration = declaration.Replace("/r", "").Replace("/n", "").Replace(" ", "");
                    MatchCollection listMatches = Regex.Matches(declaration, @"(Dictionary|List|Array)<(.*)>");

                    foreach (Match match in listMatches)
                    {
                        var newDeclarations = match.Groups[2].Value.Split(',');

                        foreach (string newDeclaration in newDeclarations) GetAllVariables(newDeclaration);
                    }

                    if (listMatches.Count == 0)
                    {
                        MatchCollection arrayMatches = Regex.Matches(declaration, @"(\b[a-zA-Z0-9_]*\b)\[\]");

                        if (arrayMatches.Count > 0)
                        {
                            if (!variables.Contains(arrayMatches[0].Groups[1].Value)) variables.Add(arrayMatches[0].Groups[1].Value);
                        }
                        else
                        {
                            if (!variables.Contains(declaration)) variables.Add(declaration);
                        }
                    }
                }

                bool isMulty;
                if (matches.Count > 0)
                {
                    isMulty = true;
                    GetAllVariables(v[0]);
                }
                else
                {
                    isMulty = false;
                    variables.Add(v[0]);
                }

                foreach (string variable in variables)
                {
                    GNode fromNode = graph.getNodeByName(tmpEdge.Key);
                    GNode toNode = graph.getNodeByName(variable);

                    string type;
                    if (v[1] == "ROG" && toNode != null)
                    {
                        if (toNode.type == "interface")
                        {
                            type = et.realisation;
                        }
                        else
                        {
                            type = et.generalization;
                        }
                    }
                    else
                    {
                        type = v[1];
                    }

                    if (fromNode != null && toNode != null)
                    {
                        bool exist = false;
                        foreach (var edge in graph.edges)
                        {
                            if (((edge.from == fromNode && edge.to == toNode && edge.type != et.generalization)
                                || (edge.from == toNode && edge.to == fromNode && edge.type == et.generalization))
                                && (edge.type == type))
                            {
                                if (isMulty && edge.toMulti != "*")
                                {
                                    edge.toMulti = "*";
                                }

                                exist = true;
                                break;
                            }
                        }

                        if (!exist && notIn(fromNode.ID, toNode.ID, type) == 2)
                        {

                            if ((type == et.realisation && toNode.type == "interface" && fromNode.type != "interface")
                                || (type != et.realisation && toNode.type != "interface" && fromNode.type != "interface"))
                            {
                                var e = graph.AddEdge(fromNode.go, toNode.go);
                                e.GetComponent<UILineRenderer>().material = getMaterial(type);
                                e.GetComponentInChildren<Image>().sprite = getArrowSprite(type);
                                e.GetComponentsInChildren<Text>()[0].text = "1";

                                graph.lastEdgeId += 1;
                                if (type == et.generalization)
                                {
                                    graph.edges.Add(new GEdge(graph.lastEdgeId, type, toNode, fromNode, "1", "1", e));
                                    e.GetComponentsInChildren<Text>()[1].text = "1";
                                }
                                else
                                {
                                    string toMulty = (isMulty && type != et.realisation) ? "*" : "1";
                                    graph.edges.Add(new GEdge(graph.lastEdgeId, type, fromNode, toNode, "1", toMulty, e));
                                    e.GetComponentsInChildren<Text>()[1].text = toMulty;
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"Bad relationship, type is: {type}, to node is: {toNode.type} and from node is: {fromNode.type}!");
                                tabletCanvas.GetComponent<HomeController>().showError($"Bad relationship, type is: {type}, to node is: {toNode.type} and from node is: {fromNode.type}!");
                            }
                        }
                    }
                }
            }
        }
        tmpEdges.Clear();
    }

    public Material getMaterial(string type)
    {
        Material newMat = new Material(shader);
        byte alpha = 100;
        switch (type)
        {
            case "association":
                newMat.SetColor("_Color", new Color32(255, 255, 255, alpha));
                break;
            case "aggregation":
                newMat.SetColor("_Color", new Color32(255, 0, 0, alpha));
                break;
            case "composition":
                newMat.SetColor("_Color", new Color32(0, 0, 0, alpha));
                break;
            case "dependency":
                newMat.SetColor("_Color", new Color32(255, 234, 4, alpha));
                break;
            case "generalization":
                newMat.SetColor("_Color", new Color32(0, 255, 0, alpha));
                break;
            case "realisation":
                newMat.SetColor("_Color", new Color32(0, 0, 255, alpha));
                break;
            default:
                Debug.LogWarning($"type: {type} is unknown!");
                newMat.SetColor("_Color", new Color32(0, 255, 255, alpha));
                break;
        }
        return newMat;
    }

    public Sprite getArrowSprite(string type)
    {
        Sprite newTex;
        switch (type)
        {
            case "association":
                newTex = Resources.Load<Sprite>("Images/emptyArrow");
                break;
            case "aggregation":
                newTex = Resources.Load<Sprite>("Images/emptyDiamond");
                break;
            case "composition":
                newTex = Resources.Load<Sprite>("Images/fullDiamond");
                break;
            case "dependency":
                newTex = Resources.Load<Sprite>("Images/emptyArrow");
                break;
            case "generalization":
                newTex = Resources.Load<Sprite>("Images/fullArrow");
                break;
            case "realisation":
                newTex = Resources.Load<Sprite>("Images/fullArrow");
                break;
            default:
                Debug.LogWarning($"type: {type} is unknown!");
                newTex = Resources.Load<Sprite>("Images/emptyArrow");
                break;
        }

        return newTex;
    }

    // 0-uprav parametre, 1-uprav kod, 2-uprav aj kod aj hranu 
    public int notIn(int fromID, int toID, string type)
    {
        List<int> removeEdges = new List<int>();
        foreach (var e in graph.edges)
        {
            if (e.from.ID == fromID && e.to.ID == toID)
            {
                if (e.type == type)
                {
                    return 0;
                }
                if (type == et.association && (e.type == et.dependency || e.type == et.aggregation || e.type == et.composition))
                {
                    return 1;
                }
                if (type == et.dependency && (e.type == et.aggregation || e.type == et.composition))
                {
                    return 1;
                }
                if (type == et.aggregation && e.type == et.composition)
                {
                    return 1;
                }
                if (type == et.composition && (e.type == et.association || e.type == et.dependency || e.type == et.aggregation))
                {
                    removeEdges.Add(e.ID);
                }
                if (type == et.aggregation && (e.type == et.association || e.type == et.dependency))
                {
                    removeEdges.Add(e.ID);
                }
                if (type == et.dependency && e.type == et.association)
                {
                    removeEdges.Add(e.ID);
                }
            }
        }

        foreach (var e in removeEdges)
        {
            GEdge gEdge = graph.getEdgeById(e);
            tabletCanvas.GetComponent<UpdateController>().removeEdge(gEdge.go, gEdge.type, gEdge.to, gEdge.from, true);
        }

        return 2;
    }

    public bool checkIfAssociationIsInFile(string relationship, GNode fromNode, GNode toNode)
    {
        var code = File.ReadAllText(fromNode.path);
        var syntaxTrees = RoslynCSharpCompiler.ParseSource(code, null)[0];
        CompilationUnitSyntax root = syntaxTrees.GetCompilationUnitRoot();

        if (relationship == et.generalization || relationship == et.realisation)
        {
            var match = Regex.Match(code, @"(class|interface)\s*" + fromNode.Nname + @"\s*.*" + toNode.Nname + @".*\s*{");
            return match.Success ? true : false;
        }

        ClassDeclarationSyntax cElem = null;
        foreach (var c in root.Members)
        {
            if (c.IsKind(SyntaxKind.ClassDeclaration))
            {
                cElem = (ClassDeclarationSyntax)c;
                break;
            }
        }

        foreach (UsingDirectiveSyntax usage in root.Usings)
        {
            var usageName = usage.Name.ToString().Substring(usage.Name.ToString().LastIndexOf('.') + 1).Replace(";", "");
            if (usageName == toNode.Nname)
            {
                return true;
            }
        }

        if (cElem != null)
        {
            var tuple = getClassMembers(cElem, fromNode.Nname, fromNode.namespaceName);
            foreach (var t in tuple.Item1)
            {
                if (t[0] == toNode.Nname && t[1] == relationship)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void loadExamaple()
    {
        StartCoroutine(loadExampleRoutine());
    }

    public IEnumerator loadExampleRoutine()
    {
        tmpEdges.Clear();

        graph.lastEdgeId = 0;
        graph.lastNodeId = 0;
        //var graphCanvasPosition = graphCanvas.transform.position;

        //graphCanvas.transform.position = new Vector3(graphCanvasPosition.x, graphCanvasPosition.y, 0);

        var nodeList = new List<GameObject>();
        foreach (var n in graph.nodes)
        {
            nodeList.Add(n.go);
        }

        var updateController = GetComponent<UpdateController>();
        foreach (var n in nodeList)
        {
            updateController.removeNode(n);
        }

        string[] fileNames = getAllCSFiles(graph.teporaryProjectPath);
        graph.generateSyntaxTree(fileNames);

        foreach (var f in graph.syntaxTrees)
        {
            string namespaceName = "default";
            CompilationUnitSyntax root = f.GetCompilationUnitRoot();
            foreach (var element in root.Members)
            {
                yield return null;
                if (element.IsKind(SyntaxKind.ClassDeclaration))
                {
                    var cElem = (ClassDeclarationSyntax)element;
                    string className = parseClassOrInterfaceDeclaration(cElem, f.FilePath, null, namespaceName);

                    foreach (UsingDirectiveSyntax usage in root.Usings)
                    {
                        insertIntoTmpEdges(className, usage.Name.ToString().Substring(usage.Name.ToString().LastIndexOf('.') + 1).Replace(";", ""), et.association);
                    }
                }
                else if (element.IsKind(SyntaxKind.NamespaceDeclaration))
                {
                    var nElem = (NamespaceDeclarationSyntax)element;
                    namespaceName = nElem.Name.ToString();
                    foreach (var e in nElem.Members)
                    {
                        if (e.IsKind(SyntaxKind.ClassDeclaration))
                        {
                            var cElem = (ClassDeclarationSyntax)e;
                            parseClassOrInterfaceDeclaration(cElem, f.FilePath, null, namespaceName);
                        }
                    }
                }
                else if (element.IsKind(SyntaxKind.InterfaceDeclaration))
                {
                    var iElem = (InterfaceDeclarationSyntax)element;
                    parseClassOrInterfaceDeclaration(iElem, f.FilePath, null, namespaceName);
                }
                else
                {
                    Debug.LogWarning($"\t{element.Kind()} in CompilationUnit");
                }
            }
        }

        //graphCanvas.transform.position = graphCanvasPosition;
        updateEges(graph);

        yield return new WaitForSecondsRealtime(0);
        graph.Layout();
    }

    public void LoadGraph()
    {
        StartCoroutine(LoadGraphRoutine());
    }

    public IEnumerator LoadGraphRoutine()
    {
        graph.lastEdgeId = 0;
        graph.lastNodeId = 0;
        var updateController = GetComponent<UpdateController>();
        var graphCanvasPosition = graphCanvas.transform.position;

        var nodeList = new List<GameObject>();
        foreach (var n in graph.nodes)
        {
            nodeList.Add(n.go);
        }

        foreach (var n in nodeList)
        {
            updateController.removeNode(n);
        }

        string json = File.ReadAllText(graph.teporaryProjectPath + "graph.json");
        JsonData jsonData = JsonUtility.FromJson<JsonData>(json);

        foreach (JsonNode jn in jsonData.nodes)
        {
            yield return null;
            addNodeToGraph(jn, graph, null);
        }

        foreach (JsonEdge je in jsonData.edges)
        {
            GNode fromNode = graph.getNodeById(je.fromID);
            GNode toNode = graph.getNodeById(je.toID);

            var e = je.type == et.generalization ? graph.AddEdge(toNode.go, fromNode.go) : graph.AddEdge(fromNode.go, toNode.go);
            e.GetComponentsInChildren<Text>()[0].text = je.type == et.generalization ? je.toMulti : je.fromMulti;
            e.GetComponentsInChildren<Text>()[1].text = je.type == et.generalization ? je.fromMulti : je.toMulti;
            e.GetComponent<UILineRenderer>().material = getMaterial(je.type);
            e.GetComponentInChildren<Image>().sprite = getArrowSprite(je.type);

            if (je.ID > graph.lastEdgeId) graph.lastEdgeId = je.ID;
            graph.edges.Add(new GEdge(je.ID, je.type, fromNode, toNode, je.fromMulti, je.toMulti, e));
        }

        graphCanvas.transform.position = graphCanvasPosition;
    }

    private string getStringFromArray(JsonAtribute[] json, string start)
    {
        string s = "";
        int i = 0;
        foreach (JsonAtribute a in json)
        {
            s += start + a.name + ": " + a.type;
            if (i < json.Length - 1)
            {
                s += "\n";
            }
            i += 1;
        }

        return s;
    }

    public static void DumpToConsole(string info, object obj)
    {
        var output = JsonUtility.ToJson(obj, true);
        Debug.Log(info + ": " + output);
    }

    public static void DumpToConsole(object obj)
    {
        var output = JsonUtility.ToJson(obj, true);
        Debug.Log(output);
    }

    public void showError(string message)
    {
        errorObject.SetActive(true);
        Text errorText = errorObject.GetComponentInChildren<Text>();
        errorText.text = message;
    }

    public void showVRTablet()
    {
        VRTablet.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 1.2f;
        VRTablet.transform.LookAt(Camera.main.transform.position);
        VRTablet.transform.Rotate(new Vector3(0, 180, 0));
    }

    public void closeError()
    {
        errorObject.SetActive(false);
    }

    [Serializable]
    public class JsonAtribute
    {
        public string name;
        public string type;

        public JsonAtribute(string n, string t)
        {
            name = n;
            type = t;
        }
    }

    [Serializable]
    public class JsonNode
    {
        public float x;
        public float y;
        public float z;
        public int ID;
        public string name;
        public string type;
        public string namespaceName;
        public JsonAtribute[] attributes;
        public JsonAtribute[] opperation;
        public string path;

        public JsonNode(float a, float b, float c, int i, string n, string t, string nn, JsonAtribute[] attr, JsonAtribute[] opper, string p)
        {
            x = a;
            y = b;
            z = c;
            ID = i;
            name = n;
            type = t;
            namespaceName = nn;
            attributes = attr;
            opperation = opper;
            path = p;
        }

        public JsonNode()
        {
            x = 0;
            y = 0;
            z = 0;
            ID = 0;
            name = null;
            type = "";
            attributes = null;
            opperation = null;
            path = null;
        }
    }

    [Serializable]
    public class JsonEdge
    {
        public int ID;
        public string type;
        public int fromID;
        public int toID;
        public string fromMulti;
        public string toMulti;

        public JsonEdge(int i, string t, int fID, int tID, string fM, string tM)
        {
            ID = i;
            type = t;
            fromID = fID;
            toID = tID;
            fromMulti = fM;
            toMulti = tM;
        }
    }

    [Serializable]
    public class JsonData
    {
        public JsonEdge[] edges = new JsonEdge[] { };
        public JsonNode[] nodes = new JsonNode[] { };
    }
}
