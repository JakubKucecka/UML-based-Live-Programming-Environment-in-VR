using InGameCodeEditor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class UpdateController : MonoBehaviour
{
    public Canvas tabletCanvas;
    private Canvas graphCanvas;
    private Graph graph;
    private TabletHandler tabletHandler;

    private EdgeType et = new EdgeType();
    private void Start()
    {
        graphCanvas = tabletCanvas.GetComponent<TabletHandler>().graphCanvas;
        graph = graphCanvas.GetComponentInChildren<Graph>();
        tabletHandler = tabletCanvas.GetComponent<TabletHandler>();
    }

    private void Update()
    {
        if (graph == null)
        {
            Debug.LogWarning("Missing graph");
            graph = graphCanvas.GetComponentInChildren<Graph>();
        }
    }

    private GNode getGNode(GameObject go)
    {
        foreach (GNode n in graph.nodes)
        {
            if (n.go == go)
            {
                return n;
            }
        }

        return null;
    }

    private GEdge getGEdge(GameObject go)
    {
        foreach (GEdge e in graph.edges)
        {
            if (e.go == go)
            {
                return e;
            }
        }

        return null;
    }

    public void showCode(GameObject node)
    {
        GNode gNode = getGNode(node);

        if (!File.Exists(gNode.path))
        {
            tabletHandler.GetComponent<AddController>().generateCode(gNode);
        }

        var textCode = File.ReadAllText(gNode.path);

        tabletHandler.showCanvas(6);

        foreach (Transform child in tabletHandler.canvases[6].transform)
        {
            if (child.name == "InGameCodeEditor")
            {
                child.GetComponent<CodeEditor>().Text = textCode;
            }
        }

        var buttons = tabletHandler.canvases[6].GetComponentsInChildren<Button>();
        buttons[buttons.Length - 1].onClick.RemoveAllListeners();
        buttons[buttons.Length - 1].onClick.AddListener(delegate { updateCode(node); });
    }

    public void updateCode(GameObject node)
    {
        GNode gNode = getGNode(node);

        var textCode = "";
        foreach (Transform child in tabletHandler.canvases[6].transform)
        {
            if (child.name == "InGameCodeEditor")
            {
                textCode = child.GetComponent<CodeEditor>().Text;
            }
        }

        var oldCode = File.ReadAllText(gNode.path);
        SyntaxTree oldTree = CSharpSyntaxTree.ParseText(oldCode);
        SyntaxTree newTree = CSharpSyntaxTree.ParseText(textCode);

        // ak nie su zmeny, nerob nic
        List<TextChange> changes = (List<TextChange>)newTree.GetChanges(oldTree);
        if (changes.Count == 0) return;
        File.WriteAllText(gNode.path, textCode);

        var homeController = tabletCanvas.GetComponent<HomeController>();
        string namespaceName = "default";

        ChcekForUpdate(textCode, homeController, gNode, namespaceName);
    }

    void ChcekForUpdate(string textCode, HomeController homeController, GNode gNode, string namespaceName)
    {
        SyntaxTree newTree = CSharpSyntaxTree.ParseText(textCode);

        CompilationUnitSyntax root = newTree.GetCompilationUnitRoot();
        foreach (var element in root.Members)
        {
            if (element.IsKind(SyntaxKind.ClassDeclaration))
            {
                var cElem = (ClassDeclarationSyntax)element;
                string className = homeController.parseClassOrInterfaceDeclaration(cElem, gNode.path, gNode, namespaceName);

                foreach (UsingDirectiveSyntax usage in root.Usings)
                {
                    homeController.insertIntoTmpEdges(className, usage.Name.ToString().Substring(usage.Name.ToString().LastIndexOf('.') + 1).Replace(";", ""), et.association);
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
                        homeController.parseClassOrInterfaceDeclaration(cElem, gNode.path, gNode, namespaceName);
                    }
                }
            }
            else if (element.IsKind(SyntaxKind.InterfaceDeclaration))
            {
                var iElem = (InterfaceDeclarationSyntax)element;
                homeController.parseClassOrInterfaceDeclaration(iElem, gNode.path, gNode, namespaceName);
            }
            else
            {
                Debug.LogWarning($"\t{element.Kind()} in CompilationUnit");
            }
        }

        List<int> removeEdges = new List<int>();

        foreach (var edge in graph.edges)
            if (
                (edge.from.ID == gNode.ID && edge.type != et.generalization)
                || (edge.to.ID == gNode.ID && edge.type == et.generalization))
                removeEdges.Add(edge.ID);

        foreach (var i in removeEdges)
        {
            GEdge gEdge = graph.getEdgeById(i);
            removeEdge(gEdge.go, gEdge.type, gEdge.to, gEdge.from, true);
        }

        homeController.updateEges(graph);
        graph.UpdateGraph();
    }

    public void showUpdateNode(GameObject node)
    {
        GNode gNode = getGNode(node);

        tabletCanvas.GetComponent<TabletHandler>().showCanvas(4);

        var dropdown = tabletHandler.canvases[4].GetComponentInChildren<TMP_Dropdown>();
        var texts = tabletHandler.canvases[4].GetComponentsInChildren<Text>();
        int typeIndex = dropdown.options.FindIndex((i) => { return i.text.ToLower().Equals(gNode.type); });
        var inputfields = tabletHandler.canvases[4].GetComponentsInChildren<MyTMPInputField>();

        texts[1].text = gNode.Nname;
        dropdown.value = typeIndex;

        string text = "";
        foreach (string[] line in gNode.attributes)
        {
            text += "- " + line[0] + ": " + line[1];
            if (gNode.attributes.IndexOf(line) < gNode.attributes.Count - 1)
            {
                text += "\n";
            }
        }
        if (text.Length > 0) inputfields[0].text = text;

        text = "";
        foreach (string[] line in gNode.opperation)
        {
            text += "+ " + line[0] + ": " + line[1];
            if (gNode.opperation.IndexOf(line) < gNode.opperation.Count - 1)
            {
                text += "\n";
            }
        }
        if (text.Length > 0) inputfields[1].text = text;

        var buttons = tabletHandler.canvases[4].GetComponentsInChildren<Button>();
        buttons[buttons.Length - 2].onClick.RemoveAllListeners();
        buttons[buttons.Length - 1].onClick.RemoveAllListeners();
        buttons[buttons.Length - 2].onClick.AddListener(delegate { updateNode(node); });
        buttons[buttons.Length - 1].onClick.AddListener(delegate { removeNode(node, true); });
    }

    public void updateNode(GameObject node)
    {
        GNode gNode = getGNode(node);

        var dropdown = tabletHandler.canvases[4].GetComponentInChildren<TMP_Dropdown>();
        var inputfields = tabletHandler.canvases[4].GetComponentsInChildren<MyTMPInputField>();

        var oldClassType = gNode.type;
        gNode.type = dropdown.options[dropdown.value].text.ToLower();
        var attrStr = inputfields[0].text.Replace("+", "").Replace("-", "");
        var opperStr = inputfields[1].text.Replace("+", "").Replace("-", "");

        string[] textLines = attrStr.Split(new[] { "\n" }, StringSplitOptions.None);
        attrStr = "";
        foreach (string line in textLines)
        {
            if (line.Length > 0) attrStr += "- " + line + "\r\n";
        }
        if (attrStr.Length > 1) attrStr = attrStr.Remove(attrStr.Length - 2);

        textLines = opperStr.Split(new[] { "\n" }, StringSplitOptions.None);
        opperStr = "";
        foreach (string line in textLines)
        {
            if (line.Length > 0) opperStr += "+ " + line + "\r\n";
        }
        if (opperStr.Length > 1) opperStr = opperStr.Remove(opperStr.Length - 2);

        var texts = node.GetComponentsInChildren<Text>();

        foreach (Text t in texts)
        {
            if (t.name == "Header")
            {
                t.text = gNode.type + " " + gNode.Nname;
                continue;
            }

            if (t.name == "Attributes")
            {
                t.text = attrStr;
                continue;
            }

            if (t.name == "Opperations")
            {
                t.text = opperStr;
                continue;
            }
        }

        var oldAttributes = gNode.attributes;
        var oldOpperation = gNode.opperation;

        var newAttributes = gNode.getListFromString(attrStr);
        var newOpperation = gNode.getListFromString(opperStr);

        var oldAttrNotInNew = oldAttributes.Where(i => !newAttributes.Exists(e => e[0] == i[0] && e[1] == i[1])).ToList();
        var newAttrNotInOld = newAttributes.Where(i => !oldAttributes.Exists(e => e[0] == i[0] && e[1] == i[1])).ToList();
        var oldOpperNotInNew = oldOpperation.Where(i => !newOpperation.Exists(e => e[0] == i[0] && e[1] == i[1])).ToList();
        var newOpperNotInOld = newOpperation.Where(i => !oldOpperation.Exists(e => e[0] == i[0] && e[1] == i[1])).ToList();

        string oldCode = File.ReadAllText(gNode.path);
        SyntaxTree oldTree = CSharpSyntaxTree.ParseText(oldCode);
        CompilationUnitSyntax root = oldTree.GetCompilationUnitRoot();

        string newCode = oldCode;

        foreach (var j in root.Members)
        {
            if (j.IsKind(SyntaxKind.ClassDeclaration) || j.IsKind(SyntaxKind.InterfaceDeclaration))
            {
                newCode = RemoveMethodOrFileld(newCode, j, oldOpperNotInNew, oldAttrNotInNew);
                break;
            }

            if (j.IsKind(SyntaxKind.NamespaceDeclaration))
            {
                var oldNamespace = (NamespaceDeclarationSyntax)j;
                foreach (var on in oldNamespace.Members)
                {
                    if (on.IsKind(SyntaxKind.ClassDeclaration) || on.IsKind(SyntaxKind.InterfaceDeclaration))
                    {
                        newCode = RemoveMethodOrFileld(newCode, on, oldOpperNotInNew, oldAttrNotInNew);
                        break;
                    }
                }
            }
        }

        root = CSharpSyntaxTree.ParseText(newCode).GetCompilationUnitRoot();

        foreach (var j in root.Members)
        {
            if (j.IsKind(SyntaxKind.ClassDeclaration) || j.IsKind(SyntaxKind.InterfaceDeclaration))
            {
                newCode = AddMethodOrFileld(newCode, j, root, newAttrNotInOld, newOpperNotInOld);
                break;
            }

            if (j.IsKind(SyntaxKind.NamespaceDeclaration))
            {
                var oldNamespace = (NamespaceDeclarationSyntax)j;
                foreach (var on in oldNamespace.Members)
                {
                    if (on.IsKind(SyntaxKind.ClassDeclaration) || on.IsKind(SyntaxKind.InterfaceDeclaration))
                    {
                        newCode = AddMethodOrFileld(newCode, on, root, newAttrNotInOld, newOpperNotInOld);
                        break;
                    }
                }
            }
        }

        // update code type
        newCode = updateType(oldClassType, gNode.type, gNode.Nname, newCode);

        File.WriteAllText(gNode.path, newCode);

        gNode.attributes = newAttributes;
        gNode.opperation = newOpperation;

        ChcekForUpdate(newCode, tabletCanvas.GetComponent<HomeController>(), gNode, gNode.namespaceName);

        tabletCanvas.GetComponent<TabletHandler>().showCanvas(4);
    }

    string RemoveMethodOrFileld(string newCode, MemberDeclarationSyntax j, List<string[]> oldOpperNotInNew, List<string[]> oldAttrNotInNew)
    {
        var tElem = (TypeDeclarationSyntax)j;
        foreach (var dec in tElem.Members)
        {
            switch (dec.Kind().ToString())
            {
                case "MethodDeclaration":
                    var mElem = (MethodDeclarationSyntax)dec;
                    if (oldOpperNotInNew.Exists(e => e[0].Remove(e[0].IndexOf("(")) == mElem.Identifier.ToString() && e[1] == mElem.ReturnType.ToString()))
                    {
                        newCode = newCode.Remove(newCode.IndexOf(mElem.ToFullString()), mElem.ToFullString().Length);
                    }
                    break;

                case "FieldDeclaration":
                    var fElem = (FieldDeclarationSyntax)dec;
                    if (oldAttrNotInNew.Exists(e => e[0] == fElem.Declaration.Variables[0].Identifier.ToString() && e[1] == fElem.Declaration.Type.ToString()))
                    {
                        newCode = newCode.Remove(newCode.IndexOf(fElem.ToFullString()), fElem.ToFullString().Length);
                    }
                    break;
            }
        }

        return newCode;
    }

    string AddMethodOrFileld(string newCode, MemberDeclarationSyntax j, CompilationUnitSyntax root, List<string[]> newAttrNotInOld, List<string[]> newOpperNotInOld)
    {
        var oldClass = (ClassDeclarationSyntax)j;
        var members = new List<MemberDeclarationSyntax>();
        foreach (var i in newAttrNotInOld)
        {
            var variableDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(i[1])).AddVariables(SyntaxFactory.VariableDeclarator(i[0]));
            members.Add(SyntaxFactory.FieldDeclaration(variableDeclaration));
        }
        foreach (var i in newOpperNotInOld)
        {
            int indexBracket = i[0].IndexOf("(");
            string methodName = indexBracket > 0 ? i[0].Remove(indexBracket) : i[0];
            var methodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(i[1]), methodName);
            members.Add(methodDeclaration.WithBody((BlockSyntax)SyntaxFactory.ParseStatement("{\r\n// place for your code\r\n}")));
        }

        var newClass = oldClass.AddMembers(members.ToArray());
        var newRoot = root.ReplaceNode(oldClass, newClass);
        newCode = newRoot.NormalizeWhitespace().ToFullString();

        return newCode;
    }

    public string updateType(string oldType, string newType, string className, string code)
    {
        var classOrInterface = oldType != "interface" ? " class " : " ";
        var newClassOrInterface = newType != "interface" ? " class " : " ";
        var startIndex = code.IndexOf(oldType + classOrInterface + className);
        if (startIndex < 0)
        {
            tabletCanvas.GetComponent<HomeController>().showError($"Update type failed, not found '{oldType + classOrInterface + className}', check white spaces!");
            Debug.LogWarning($"Update type failed, not found '{oldType + classOrInterface + className}', check white spaces!");
        }
        else
        {
            code = code.Remove(startIndex, (oldType + classOrInterface).Length);
            code = code.Insert(startIndex, newType + newClassOrInterface);
        }

        return code;
    }

    public void removeNode(GameObject node, bool remove = false)
    {
        GNode gNode = getGNode(node);

        var newEdges = new List<GEdge>();
        foreach (GEdge e in graph.edges)
        {
            if (e.type == et.composition && e.from == gNode && e.to.path == "nested")
            {
                removeNode(e.to.go, remove);
            }
            if (e.from != gNode && e.to != gNode)
            {
                newEdges.Add(e);
            }
        }

        if (remove && File.Exists(gNode.path)) File.Delete(gNode.path);

        graph.edges = newEdges;
        graph.nodes.Remove(gNode);
        graph.RemoveNode(node);
        graph.UpdateGraph();

        tabletHandler.showCanvas(0);
    }

    public void showUpdateEdge(GameObject edge)
    {
        GEdge gEdge = getGEdge(edge);

        var dropdowns = tabletHandler.canvases[5].GetComponentsInChildren<TMP_Dropdown>();
        int typeIndex = dropdowns[1].options.FindIndex((i) => { return i.text.ToLower().Equals(gEdge.type); });

        int fromIndex = graph.nodes.IndexOf(gEdge.from);
        int toIndex = graph.nodes.IndexOf(gEdge.to);

        //dropdowns[0].onValueChanged.AddListener();
        dropdowns[1].value = typeIndex;
        dropdowns[2].value = fromIndex;
        dropdowns[3].value = toIndex;

        var inputFields = tabletHandler.canvases[5].GetComponentsInChildren<MyTMPInputField>();
        inputFields[0].text = gEdge.fromMulti;
        inputFields[1].text = gEdge.toMulti;

        var buttons = tabletHandler.canvases[5].GetComponentsInChildren<Button>();
        buttons[buttons.Length - 2].onClick.RemoveAllListeners();
        buttons[buttons.Length - 1].onClick.RemoveAllListeners();
        buttons[buttons.Length - 2].onClick.AddListener(delegate { updateEdge(edge); });
        buttons[buttons.Length - 1].onClick.AddListener(delegate { removeEdge(edge, gEdge.type, gEdge.to, gEdge.from, false); });
    }

    public void updateEdge(GameObject edge)
    {
        GEdge gEdge = getGEdge(edge);

        var dropdowns = tabletHandler.canvases[5].GetComponentsInChildren<TMP_Dropdown>();
        var inputFields = tabletHandler.canvases[5].GetComponentsInChildren<MyTMPInputField>();

        var type = dropdowns[1].options[dropdowns[1].value].text.ToLower();
        var fromStr = dropdowns[2].options[dropdowns[2].value].text;
        var toStr = dropdowns[3].options[dropdowns[3].value].text;

        var fromId = int.Parse(fromStr.Substring(fromStr.IndexOf("ID:") + 3));
        var toId = int.Parse(toStr.Substring(toStr.IndexOf("ID:") + 3));

        GNode fromNode = graph.getNodeById(fromId);
        GNode toNode = graph.getNodeById(toId);

        var fromMulti = inputFields[0].text == "" ? "1" : inputFields[0].text;
        var toMulti = inputFields[1].text == "" ? "1" : inputFields[1].text;

        graph.RemoveEdge(edge);
        if (tabletCanvas.GetComponent<HomeController>().notIn(fromNode.ID, toNode.ID, type) == 1
            || (type == et.realisation && toNode.type != "interface" || fromNode.type == "interface")
            || (type == et.realisation && toNode.type == "interface" || fromNode.type == "interface"))
        {
            tabletCanvas.GetComponent<HomeController>().showError($"Bad relationship, type is: {type}, to node is: {toNode.type} and from node is: {fromNode.type}, or your subtype already exist!");
            Debug.LogWarning($"Bad relationship, type is: {type}, to node is: {toNode.type} and from node is: {fromNode.type}, or your subtype already exist!");
            return;
        }

        var e = type == et.generalization ? graph.AddEdge(toNode.go, fromNode.go) : graph.AddEdge(fromNode.go, toNode.go);
        e.GetComponentsInChildren<Text>()[0].text = type == et.generalization ? toMulti : fromMulti;
        e.GetComponentsInChildren<Text>()[1].text = type == et.generalization ? fromMulti : toMulti;
        e.GetComponent<UILineRenderer>().material = tabletCanvas.GetComponent<HomeController>().getMaterial(type);
        e.GetComponentInChildren<Image>().sprite = tabletCanvas.GetComponent<HomeController>().getArrowSprite(type);
        if (gEdge.type != type || gEdge.from.Nname != fromNode.Nname || gEdge.to.Nname != toNode.Nname)
        {
            if (gEdge.type == et.generalization)
            {
                tabletCanvas.GetComponent<AddController>().RemoveCodeWithEdge(gEdge.type, gEdge.from, gEdge.to);
            }
            else
            {
                tabletCanvas.GetComponent<AddController>().RemoveCodeWithEdge(gEdge.type, gEdge.to, gEdge.from);
            }
            tabletCanvas.GetComponent<AddController>().AddCodeWithEdge(type, fromNode, toNode);
        }

        gEdge.type = type;
        gEdge.from = fromNode;
        gEdge.to = toNode;
        gEdge.fromMulti = fromMulti;
        gEdge.toMulti = toMulti;
        gEdge.go = e;

        var buttons = tabletHandler.canvases[5].GetComponentsInChildren<Button>();
        buttons[buttons.Length - 2].onClick.RemoveAllListeners();
        buttons[buttons.Length - 1].onClick.RemoveAllListeners();
        buttons[buttons.Length - 2].onClick.AddListener(delegate { updateEdge(e); });
        buttons[buttons.Length - 1].onClick.AddListener(delegate { removeEdge(e, type, toNode, fromNode, false); });

        graph.UpdateGraph();

        if (tabletCanvas.GetComponent<HomeController>().checkIfAssociationIsInFile(type, fromNode, toNode))
        {
            tabletCanvas.GetComponent<HomeController>().showError($"You remove realitonship: {type} on: {toNode.Nname}, but it is in {fromNode.path}!");
            Debug.LogWarning($"You remove realitonship: {type} on: {toNode.Nname}, but it is in {fromNode.path}!");
        }

        tabletCanvas.GetComponent<TabletHandler>().showCanvas(5);
    }

    public void removeEdge(GameObject edge, string type, GNode toNode, GNode fromNode, bool auto)
    {
        GEdge gEdge = getGEdge(edge);

        if (type == et.generalization)
        {
            if (tabletCanvas.GetComponent<HomeController>().isInTmpEdges(fromNode.Nname))
                tabletCanvas.GetComponent<AddController>().RemoveCodeWithEdge(type, fromNode, toNode);
        }
        else
        {
            if (tabletCanvas.GetComponent<HomeController>().isInTmpEdges(fromNode.Nname))
                tabletCanvas.GetComponent<AddController>().RemoveCodeWithEdge(type, toNode, fromNode);
        }

        //// pri kompozicii vymazeme aj child triedu
        //if (type == et.composition && toNode.path != "nested")
        //{
        //    removeNode(toNode.go);
        //}

        graph.RemoveEdge(edge);
        graph.edges.Remove(gEdge);

        graph.UpdateGraph();

        if (!auto && tabletCanvas.GetComponent<HomeController>().checkIfAssociationIsInFile(type, fromNode, toNode))
        {
            tabletCanvas.GetComponent<HomeController>().showError($"You remove realitonship: {type} on: {toNode.Nname}, but it is in {fromNode.path}!");
            Debug.LogWarning($"You remove realitonship: {type} on: {toNode.Nname}, but it is in {fromNode.path}!");
        }

        tabletHandler.showCanvas(0);
    }

    public void changeSelected(Canvas canvas)
    {
        var dropdowns = canvas.GetComponentsInChildren<TMP_Dropdown>();
        var edgeStr = dropdowns[0].options[dropdowns[0].value].text;
        var edgeID = int.Parse(edgeStr.Substring(edgeStr.IndexOf("ID:") + 3));

        GEdge gEdge = graph.getEdgeById(edgeID);

        showUpdateEdge(gEdge.go);
    }
}
