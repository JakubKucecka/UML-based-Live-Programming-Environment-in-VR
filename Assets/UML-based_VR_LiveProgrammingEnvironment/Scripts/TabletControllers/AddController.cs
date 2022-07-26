using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynCSharp.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class AddController : MonoBehaviour
{
    public Canvas tabletCanvas;
    private Canvas graphCanvas;

    private Graph graph;

    EdgeType et = new EdgeType();

    private void Start()
    {
        graphCanvas = tabletCanvas.GetComponent<TabletHandler>().graphCanvas;
        graph = graphCanvas.GetComponentInChildren<Graph>();
    }

    private void Update()
    {
        if(graph == null)
        {
            Debug.LogWarning("Missing graph");
            graph = graphCanvas.GetComponentInChildren<Graph>();
        }
    }

    public void addNode(Canvas canvas)
    {
        var dropdown = canvas.GetComponentInChildren<TMP_Dropdown>();
        var type = dropdown.options[dropdown.value].text.ToLower();

        var inputFields = canvas.GetComponentsInChildren<MyTMPInputField>();
        var name = inputFields[0].text.Replace(" ", "");
        var attrStr = inputFields[1].text.Replace("+", "").Replace("-", "");
        var opperStr = inputFields[2].text.Replace("+", "").Replace("-", "");

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

        if (type != "" && name != "")
        {
            name = char.ToUpper(name[0]) + name.Substring(1);
            var n = graph.AddNode();

            var buttons = n.GetComponentsInChildren<Button>();

            foreach (Button b in buttons)
            {
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

            graph.lastNodeId += 1;
            var filename = graph.teporaryProjectPath + name + ".cs";
            graph.nodes.Add(new GNode(graph.lastNodeId, name, type, "default", attrStr, opperStr, filename, n));

            GNode gNode = graph.getNodeById(graph.lastNodeId);

            generateCode(gNode);

            var texts = n.GetComponentsInChildren<Text>();

            foreach (Text t in texts)
            {
                if (t.name == "Header")
                {
                    t.text = type + " " + name;
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
        }

        tabletCanvas.GetComponent<TabletHandler>().showCanvas(1);
    }

    public void generateCode(GNode gNode)
    {
        var typeOfFile = gNode.type == "interface" ? "" : "class";
        var textCode = $@"using System.Collections;
using System.Collections.Generic;
using UnityEngine;

{gNode.type} {typeOfFile} {gNode.Nname} : MonoBehaviour
{'{'}
";

        foreach (string[] line in gNode.attributes)
        {
            textCode += $"\t{line[1]} {line[0]};\r\n";
        }

        foreach (string[] line in gNode.opperation)
        {
            textCode += $@"
    {line[1]} {line[0]}
    {'{'}
        // place for your code
    {'}'}
    ";
        }

        textCode += "\r\n}";

        File.WriteAllText(gNode.path, textCode);
    }

    public void addEdge(Canvas canvas)
    {
        var dropdowns = canvas.GetComponentsInChildren<TMP_Dropdown>();
        if (dropdowns[1].options.Count > 0 && dropdowns[2].options.Count > 0)
        {
            var fromStr = dropdowns[1].options[dropdowns[1].value].text;
            var toStr = dropdowns[2].options[dropdowns[2].value].text;

            var fromId = int.Parse(fromStr.Substring(fromStr.IndexOf("ID:") + 3));
            var toId = int.Parse(toStr.Substring(toStr.IndexOf("ID:") + 3));

            GNode fromNode = graph.getNodeById(fromId);
            GNode toNode = graph.getNodeById(toId);

            var type = dropdowns[0].options[dropdowns[0].value].text.ToLower();
            var inputFields = canvas.GetComponentsInChildren<MyTMPInputField>();
            var fromMulti = inputFields[0].text == "" ? "1" : inputFields[0].text;
            var toMulti = inputFields[1].text == "" ? "1" : inputFields[1].text;

            if (type != "")
            {
                if ((type == et.realisation && toNode.type == "interface" && fromNode.type != "interface") || (type != et.realisation && toNode.type != "interface" && fromNode.type != "interface"))
                {
                    var update = tabletCanvas.GetComponent<HomeController>().notIn(fromNode.ID, toNode.ID, type);
                    if (update == 2)
                    {
                        var e = type == et.generalization ? graph.AddEdge(toNode.go, fromNode.go) : graph.AddEdge(fromNode.go, toNode.go);
                        e.GetComponent<UILineRenderer>().material = tabletCanvas.GetComponent<HomeController>().getMaterial(type);
                        e.GetComponentInChildren<Image>().sprite = tabletCanvas.GetComponent<HomeController>().getArrowSprite(type);
                        e.GetComponentsInChildren<Text>()[0].text = type == et.generalization ? toMulti : fromMulti;
                        e.GetComponentsInChildren<Text>()[1].text = type == et.generalization ? fromMulti : toMulti;

                        //e.GetComponent<EdgeCotroller>().onClick.RemoveAllListeners();
                        //e.GetComponent<EdgeCotroller>().onClick.AddListener(delegate { tabletCanvas.GetComponent<UpdateController>().showUpdateEdge(e); });

                        graph.lastEdgeId += 1;
                        if (type == et.generalization)
                        {
                            graph.edges.Add(new GEdge(graph.lastEdgeId, type, toNode, fromNode, toMulti, fromMulti, e));
                        }
                        else
                        {
                            graph.edges.Add(new GEdge(graph.lastEdgeId, type, fromNode, toNode, fromMulti, toMulti, e));
                        }
                        graph.UpdateGraph();

                    }

                    if (update > 0)
                    {
                        AddCodeWithEdge(type, fromNode, toNode);
                    }
                }
                else
                {
                    Debug.LogWarning($"Bad relationship, type is: {type}, to node is: {toNode.type} and from node is: {fromNode.type}!");
                }
            }
            if (type == et.generalization)
            {
                if (!tabletCanvas.GetComponent<HomeController>().checkIfAssociationIsInFile(type, toNode, fromNode))
                {
                    Debug.LogWarning($"You add realitonship: {type} on: {fromNode.Nname}, but it not in {toNode.path}!");
                }
            }
            else
            {
                if (!tabletCanvas.GetComponent<HomeController>().checkIfAssociationIsInFile(type, fromNode, toNode))
                {
                    Debug.LogWarning($"You add realitonship: {type} on: {toNode.Nname}, but it not in {fromNode.path}!");
                }
            }
        }

        tabletCanvas.GetComponent<TabletHandler>().showCanvas(1);
    }

    public void RemoveCodeWithEdge(string type, GNode toNode, GNode fromNode)
    {
        if (fromNode.path == "nested") return;

        string code = File.ReadAllText(fromNode.path);
        if (type == et.realisation || type == et.generalization)
        {
            code = removeClassfromHeader(fromNode, toNode.Nname, code);
        }
        File.WriteAllText(fromNode.path, code);
    }

    public void AddCodeWithEdge(string type, GNode fromNode, GNode toNode)
    {
        string code;
        if (type == et.generalization)
        {
            code = File.ReadAllText(toNode.path);
            code = checkOrAddNamespace(code, fromNode.namespaceName);
            code = addClassToHeader(toNode, fromNode.Nname, code);
            File.WriteAllText(toNode.path, code);
            return;
        }

        code = File.ReadAllText(fromNode.path);
        code = checkOrAddNamespace(code, toNode.namespaceName);

        if (type == et.realisation)
        {
            code = addClassToHeader(fromNode, toNode.Nname, code);
        }
        else if (type == et.aggregation)
        {
            var syntaxTrees = RoslynCSharpCompiler.ParseSource(code, null)[0];
            CompilationUnitSyntax root = syntaxTrees.GetCompilationUnitRoot();

            foreach (var c in root.Members)
            {
                if (c.IsKind(SyntaxKind.ClassDeclaration))
                {
                    var oldClass = (ClassDeclarationSyntax)c;
                    var newClass = oldClass;
                    bool startExist = false;
                    string propertyName = toNode.Nname[0].ToString().ToLower() + toNode.Nname.Substring(1);

                    var variableDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(toNode.Nname))
                        .AddVariables(SyntaxFactory.VariableDeclarator(propertyName));
                    var fieldDeclaration = SyntaxFactory.FieldDeclaration(variableDeclaration)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));

                    newClass = newClass.AddMembers(fieldDeclaration);
                    updateBubbleAndGNode(fromNode, "Attributes", propertyName, toNode.Nname);
                    var statement = $"{propertyName} = GetComponent<{toNode.Nname}>;\r\n";

                    foreach (var element in newClass.Members)
                    {
                        if (element.IsKind(SyntaxKind.MethodDeclaration))
                        {
                            var mElem = (MethodDeclarationSyntax)element;
                            if (mElem.Identifier.ToString() == "Start")
                            {
                                startExist = true;
                                break;
                            }
                        }
                    }

                    if (!startExist)
                    {
                        var methodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("void"), "Start")
                            .WithBody((BlockSyntax)SyntaxFactory.ParseStatement("{\r\n" + statement + "}"));
                        newClass = newClass.AddMembers(methodDeclaration);
                        updateBubbleAndGNode(fromNode, "Opperations", "Start", "void");
                    }

                    var newRoot = root.ReplaceNode(oldClass, newClass);
                    code = newRoot.NormalizeWhitespace().ToFullString();

                    if (startExist)
                    {
                        var match = Regex.Match(code, @"void\b\s+\bStart\b\((.*)\)\s*{");
                        var matchWithTabs = Regex.Match(code, @"void\b\s+\bStart\b\((.*)\)\s*{\s*");
                        int startIndex = matchWithTabs.Index + matchWithTabs.Value.Length;

                        for (int i = 0; i < (matchWithTabs.Value.Length - match.Value.Length) / 2; i++) statement += "\t";
                        code = code.Insert(startIndex, "\t" + statement);
                    }

                    break;
                }
            }
        }
        else if (type == et.composition)
        {
            var syntaxTrees = RoslynCSharpCompiler.ParseSource(code, null)[0];
            CompilationUnitSyntax root = syntaxTrees.GetCompilationUnitRoot();

            foreach (var c in root.Members)
            {
                if (c.IsKind(SyntaxKind.ClassDeclaration))
                {
                    var oldClass = (ClassDeclarationSyntax)c;
                    var newClass = oldClass;
                    string propertyName = toNode.Nname[0].ToString().ToLower() + toNode.Nname.Substring(1);

                    var variableDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(toNode.Nname))
                        .AddVariables(SyntaxFactory.VariableDeclarator($"{propertyName} = new {toNode.Nname}()"));
                    var fieldDeclaration = SyntaxFactory.FieldDeclaration(variableDeclaration)
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

                    newClass = newClass.AddMembers(fieldDeclaration);
                    var newRoot = root.ReplaceNode(oldClass, newClass);
                    code = newRoot.NormalizeWhitespace().ToFullString();
                    updateBubbleAndGNode(fromNode, "Attributes", propertyName, toNode.Nname);
                    break;
                }
            }
        }

        File.WriteAllText(fromNode.path, code);
    }

    public void updateBubbleAndGNode(GNode node, string textName, string propertyName, string propertyType)
    {
        var texts = node.go.GetComponentsInChildren<Text>();
        foreach (Text t in texts)
        {
            if (t.name == textName)
            {
                if (t.name == "Attributes")
                {
                    t.text += $"\r\n- {propertyName}: {propertyType}";
                    node.attributes.Add(new string[] { propertyName, propertyType });
                    break;
                }

                if (t.name == "Opperations")
                {
                    propertyName += "()";
                    t.text += $"\r\n+ {propertyName}: {propertyType}";
                    node.attributes.Add(new string[] { propertyName, propertyType });
                    break;
                }
            }
        }
    }

    public string checkOrAddNamespace(string code, string namespaceName)
    {
        if (namespaceName != "default")
        {
            MatchCollection matches = Regex.Matches(code, $@"\s*\busing\b\s*(\b{namespaceName}\b)\s*;\s*");

            if (matches.Count <= 0)
            {
                code = code.Insert(0, $"using {namespaceName};\r\n");
            }
        }

        return code;
    }

    public string removeClassfromHeader(GNode fromNode, string headerClass, string code)
    {
        var classOrInterface = fromNode.type == "interface" ? " " : " class ";
        var headerIndex = code.IndexOf(fromNode.type + classOrInterface + fromNode.Nname);

        if (headerIndex < 0)
        {
            Debug.LogWarning($"Remove class or interface from bubble declaration failed, not found: '{fromNode.type + classOrInterface + fromNode.Nname}', check white spaces!");
            tabletCanvas.GetComponent<HomeController>().showError($"Remove class or interface from bubble declaration failed, not found: '{fromNode.type + classOrInterface + fromNode.Nname}', check white spaces!");
            return code;
        }

        var oldHeader = code.Substring(headerIndex);
        oldHeader = oldHeader.Remove(oldHeader.IndexOf("{"));

        var headreArray = oldHeader.Split(':');
        if (headreArray.Length < 2)
        {
            return code;
        }

        headreArray = headreArray[1].Replace(" ", "").Split(',');
        var newHeaderArray = new List<string>();
        foreach (var a in headreArray)
        {
            if (a.Replace("\r", "").Replace("\n", "") != headerClass) newHeaderArray.Add(a);
        }

        var newHeader = oldHeader.Split(':')[0];
        if (newHeaderArray.Count > 0)
        {
            newHeader += ": " + String.Join(", ", newHeaderArray.ToArray());
        }

        code = code.Remove(headerIndex, oldHeader.Length);
        code = code.Insert(headerIndex, newHeader + "\r\n");

        return code;
    }

    public string addClassToHeader(GNode fromNode, string newHeaderClass, string code)
    {
        var classOrInterface = fromNode.type == "interface" ? " " : " class ";
        var headerIndex = code.IndexOf(fromNode.type + classOrInterface + fromNode.Nname);

        if (headerIndex < 0)
        {
            Debug.LogWarning($"Add class or interface from buble declaration failed, not found: '{fromNode.type + classOrInterface + fromNode.Nname}', check white spaces!");
            tabletCanvas.GetComponent<HomeController>().showError($"Add class or interface from buble declaration failed, not found: '{fromNode.type + classOrInterface + fromNode.Nname}', check white spaces!");
            return code;
        }

        var startIndex = code.IndexOf(":") + 1;
        var headerString = " " + newHeaderClass + ",";

        if (startIndex < 1)
        {
            startIndex = headerIndex + (fromNode.type + classOrInterface + fromNode.Nname).Length;
            headerString = " : " + newHeaderClass;
        }

        return code.Insert(startIndex, headerString);
    }
}
