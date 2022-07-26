using FrostweepGames.Plugins.GoogleCloud.SpeechRecognition;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TabletHandler : MonoBehaviour
{
    public Canvas graphCanvas;
    public List<Canvas> canvases;
    public GameObject RC_Keyboard;

    private Graph graph;

    // Start is called before the first frame update
    void Start()
    {
        // 0 - home
        // 1 - add
        // 2 - addNode
        // 3 - addEdge
        // 4 - updateNode
        // 5 - updateEdge
        // 6 - code

        if (canvases.Count > 0)
        {
            showCanvas(0);
        }

        graph = graphCanvas.GetComponentInChildren<Graph>();
    }

    // Update is called once per frame
    void Update()
    {
        if (graph == null)
        {
            Debug.LogWarning("Missing graph");
            graph = graphCanvas.GetComponentInChildren<Graph>();
        }
    }

    public void showCanvas(int index)
    {

        hideAllCanvases();
        canvases[index].gameObject.SetActive(true);
        if (index == 1)
        {
            if (canvases[index].GetComponentInChildren<TMP_Dropdown>().value == 0)
            {
                showCanvas(2);
                canvases[1].gameObject.SetActive(true);
            }
            else
            {
                showCanvas(3);
                canvases[1].gameObject.SetActive(true);
            }
        }

        if (index == 2)
        {
            canvases[1].GetComponentInChildren<TMP_Dropdown>().value = 0;
            canvases[index].GetComponentInChildren<TMP_Dropdown>().ClearOptions();
            canvases[index].GetComponentInChildren<TMP_Dropdown>().AddOptions(graph.NodeTypes);

            var inputfields = canvases[index].GetComponentsInChildren<MyTMPInputField>();
            foreach (MyTMPInputField i in inputfields)
            {
                i.text = "";
            }
        }

        if (index == 3)
        {
            canvases[1].GetComponentInChildren<TMP_Dropdown>().value = 1;
            var dropdowns = canvases[index].GetComponentsInChildren<TMP_Dropdown>();
            List<string> nodes = new List<string>();

            foreach (GNode n in graph.nodes)
            {
                nodes.Add(n.Nname + ", ID:" + n.ID);
            }

            dropdowns[0].ClearOptions();
            dropdowns[0].AddOptions(graph.EdgeTypes);
            dropdowns[1].ClearOptions();
            dropdowns[1].AddOptions(nodes);
            dropdowns[2].ClearOptions();
            dropdowns[2].AddOptions(nodes);

            var inputfields = canvases[index].GetComponentsInChildren<MyTMPInputField>();
            foreach (MyTMPInputField i in inputfields)
            {
                i.text = "";
            }
        }

        if (index == 4)
        {
            canvases[index].GetComponentsInChildren<Text>()[1].text = "";
            canvases[index].GetComponentInChildren<TMP_Dropdown>().ClearOptions();
            canvases[index].GetComponentInChildren<TMP_Dropdown>().AddOptions(graph.NodeTypes);
            var buttons = canvases[index].GetComponentsInChildren<Button>();
            buttons[buttons.Length - 2].onClick.RemoveAllListeners();
            buttons[buttons.Length - 1].onClick.RemoveAllListeners();

            var inputfields = canvases[index].GetComponentsInChildren<MyTMPInputField>();
            foreach (MyTMPInputField i in inputfields)
            {
                i.text = "";
            }
        }

        if (index == 5)
        {
            if (graph.edges.Count > 0)
            {
                var dropdowns = canvases[index].GetComponentsInChildren<TMP_Dropdown>();
                List<string> nodes = new List<string>();

                foreach (GNode n in graph.nodes)
                {
                    nodes.Add(n.Nname + ", ID:" + n.ID);
                }

                dropdowns[1].ClearOptions();
                dropdowns[1].AddOptions(graph.EdgeTypes);
                dropdowns[2].ClearOptions();
                dropdowns[2].AddOptions(nodes);
                dropdowns[3].ClearOptions();
                dropdowns[3].AddOptions(nodes);

                List<string> edges = new List<string>();
                foreach (GEdge e in graph.edges)
                {
                    edges.Add(e.from.Nname + " -> " + e.to.Nname + ", ID:" + e.ID);
                }

                dropdowns[0].ClearOptions();
                dropdowns[0].AddOptions(edges);

                GetComponent<UpdateController>().changeSelected(canvases[5]);

                //var inputfields = canvases[index].GetComponentsInChildren<MyTMPInputField>();
                //foreach (MyTMPInputField i in inputfields)
                //{
                //    i.text = "";
                //}
            }
            else
            {
                showCanvas(3);
                canvases[1].gameObject.SetActive(true);
            }
        }
    }

    void hideAllCanvases()
    {
        foreach (Canvas c in canvases)
        {
            c.gameObject.SetActive(false);
        }
    }

    public void changeAdd(TMP_Dropdown dropdown)
    {
        if (dropdown.name == "AddDropdown")
        {
            if (dropdown.value == 0)
            {
                showCanvas(2);
                canvases[1].gameObject.SetActive(true);
            }

            if (dropdown.value == 1)
            {
                showCanvas(3);
                canvases[1].gameObject.SetActive(true);
            }
        }
    }

    public void EndApp()
    {
        Application.Quit();
    }

    public void ChangeKeyboard()
    {
            RC_Keyboard.SetActive(!RC_Keyboard.activeSelf);
    }
}
