using RoslynCSharp;
using RoslynCSharp.Compiler;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

[System.Serializable]
public class AsyncCompileOperationEvent : UnityEvent<AsyncCompileOperation> { }
public class CompileFromFile : MonoBehaviour
{
    public Canvas tabletCanvas;
    private Canvas graphCanvas;
    private Graph graph;
    private TabletHandler tabletHandler;

    public GameHandler gameHandler;
    public GameObject levels;

    private ScriptDomain domain = null;
    public AssemblyReferenceAsset[] assemblyReferences;
    public AsyncCompileOperationEvent onCompilationFinished;
    private bool initialized = false;

    private UnityWebRequestAsyncOperation LoadAssemblyData(string location)
    {
        var path = "file://" + location;
#if UNITY_ANDROID && !UNITY_EDITOR
        path = "jar:" + path;
#endif
        UnityWebRequest wr = UnityWebRequest.Get("file://" + location);
        return wr.SendWebRequest();
    }

    private IEnumerator Start()
    {
        graphCanvas = tabletCanvas.GetComponent<TabletHandler>().graphCanvas;
        graph = graphCanvas.GetComponentInChildren<Graph>();
        tabletHandler = tabletCanvas.GetComponent<TabletHandler>();
        gameHandler.compiler = this;

        // Create the domain
        domain = ScriptDomain.CreateDomain("DeveloperZone", true);

        // Write line in debug
        domain.RoslynCompilerService.GenerateSymbols = true;

        // Add assembly references
        List<string> locations = new List<string>();
        locations.Add(typeof(GameObject).Assembly.Location);
        locations.Add(typeof(object).Assembly.Location);
        locations.Add(typeof(Enumerable).Assembly.Location);
        locations.Add(typeof(Regex).Assembly.Location);
        locations.Add(typeof(BaseGame).Assembly.Location);
        locations.Add(typeof(Collider).Assembly.Location);

        foreach (var location in locations)
        {
            UnityWebRequestAsyncOperation assemblyRequest = LoadAssemblyData(location);
            yield return assemblyRequest;
            domain.RoslynCompilerService.ReferenceAssemblies.Add(AssemblyReference.FromImage(assemblyRequest.webRequest.downloadHandler.data));
        }
        initialized = true;
    }

    private void Update()
    {
        if (graph == null)
        {
            graph = graphCanvas.GetComponentInChildren<Graph>();
        }
    }
    public void CompileFiles()
    {
        StartCoroutine(CompileFilesCorutine());
    }
    public IEnumerator CompileFilesCorutine()
    {
        // Compile and load code
        string[] sourceFiles = tabletHandler.GetComponent<HomeController>().getAllCSFiles(graph.teporaryProjectPath);
        gameHandler.console.text = String.Empty;
        gameHandler.AppendLog("Compilation running, wait ...\n");

        // Wait if not initialized
        yield return new WaitUntil(() => initialized);
        // Compile and load code
        AsyncCompileOperation compileRequest = domain.CompileAndLoadFilesAsync(sourceFiles, ScriptSecurityMode.UseSettings);

        // Wait for operation to complete
        yield return compileRequest;

        ScriptType[] types = compileRequest.CompiledAssembly != null ? compileRequest.CompiledAssembly.FindAllTypes() : new ScriptType[] { };

        foreach (Transform t in levels.GetComponentsInChildren<Transform>(true)) t.gameObject.SetActive(true);

        foreach (var t in types)
        {
            if (Regex.IsMatch(t.FullName, "Game", RegexOptions.IgnoreCase))
            {
                BaseGame baseGame = gameHandler.gameObject.GetComponent<BaseGame>();
                int l = 1;
                if (baseGame != null) l = baseGame.level;
                manageScript(gameHandler.gameObject, "Game", t);
                gameHandler.gameObject.GetComponent<BaseGame>().level = l;
            }
            if (Regex.IsMatch(t.FullName, "Stock", RegexOptions.IgnoreCase))
            {
                manageScript(gameHandler.gameObject, "Stock", t);
            }
            else if (Regex.IsMatch(t.FullName, "Player", RegexOptions.IgnoreCase))
            {
                manageScript(gameHandler.player, "Player", t);
            }
            else if (Regex.IsMatch(t.FullName, "Ghost", RegexOptions.IgnoreCase))
            {
                manageScript(gameHandler.ghost, "Ghost", t);
            }
            else if (Regex.IsMatch(t.FullName, "Wall", RegexOptions.IgnoreCase))
            {
                foreach (GameObject wall in GameObject.FindGameObjectsWithTag("Wall"))
                {
                    manageScript(wall, "Wall", t);
                }
            }
            else if (Regex.IsMatch(t.FullName, "Barrier1", RegexOptions.IgnoreCase))
            {
                manageScript(gameHandler.barriers[0], "Barrier1", t);
            }
            else if (Regex.IsMatch(t.FullName, "Barrier2", RegexOptions.IgnoreCase))
            {
                manageScript(gameHandler.barriers[1], "Barrier2", t);
            }
            else if (Regex.IsMatch(t.FullName, "Barrier3", RegexOptions.IgnoreCase))
            {
                manageScript(gameHandler.barriers[2], "Barrier3", t);
            }
            else if (Regex.IsMatch(t.FullName, "Barrier4", RegexOptions.IgnoreCase))
            {
                manageScript(gameHandler.barriers[3], "Barrier4", t);
            }
            else if (Regex.IsMatch(t.FullName, "Barrier5", RegexOptions.IgnoreCase))
            {
                manageScript(gameHandler.barriers[4], "Barrier5", t);
            }
        }

        // Check for compiler errors
        if (domain.CompileResult != null && domain.CompileResult.Success == false)
        {
            // Get all errors
            foreach (CompilationError error in domain.CompileResult.Errors)
            {
                gameHandler.AppendLog(error.ToString() + "\n");
            }
        }
        else
        {
            gameHandler.LoadScene();
            gameHandler.AppendLog($"Compilation success!\n");
        }

    }

    private void manageScript(GameObject go, string scriptName, ScriptType type)
    {
        var g = go.GetComponent(scriptName) as MonoBehaviour;
        if (g != null)
        {
            g.enabled = false;
        }
        type.CreateInstance(go);
        if (g != null && !g.enabled) DestroyImmediate(g);
    }
}
