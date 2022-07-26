using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHandler : MonoBehaviour
{
    public TMP_InputField console;

    public Button upButton;
    public Button downButton;
    public Button leftButton;
    public Button rightButton;
    public Button l1Button;
    public Button l2Button;
    public Button l3Button;
    public Button l4Button;
    public Button runButton;
    public Button stopButton;
    public Button restartButton;

    public GameObject player;
    public GameObject ghost;
    public List<GameObject> barriers;

    public List<GameObject> levels = new List<GameObject>();

    public CompileFromFile compiler;

    private void Update()
    {
        AppendLog("");
    }

    public void LoadScene()
    {
        if (console == null)
        {
            console = GameObject.FindGameObjectWithTag("Console").GetComponent<TMP_InputField>();
            AppendLog("Console load successfully.\n");
        }

        LoadButtons();
        AppendLog("Buttons load successfully.\n");

        LoadFields();
        AppendLog("Fields load successfully.\n");
    }

    void LoadButtons()
    {
        BaseGame game = getGame();

        upButton.onClick.RemoveAllListeners();
        upButton.onClick.AddListener(delegate { game.MovePlayer(Vector3.up); });

        downButton.onClick.RemoveAllListeners();
        downButton.onClick.AddListener(delegate { game.MovePlayer(Vector3.down); });

        leftButton.onClick.RemoveAllListeners();
        leftButton.onClick.AddListener(delegate { game.MovePlayer(Vector3.left); });

        rightButton.onClick.RemoveAllListeners();
        rightButton.onClick.AddListener(delegate { game.MovePlayer(Vector3.right); });

        l1Button.onClick.RemoveAllListeners();
        l1Button.onClick.AddListener(delegate { game.ShowLevel(1); });

        l2Button.onClick.RemoveAllListeners();
        l2Button.onClick.AddListener(delegate { game.ShowLevel(2); });

        l3Button.onClick.RemoveAllListeners();
        l3Button.onClick.AddListener(delegate { game.ShowLevel(3); });

        l4Button.onClick.RemoveAllListeners();
        l4Button.onClick.AddListener(delegate { game.ShowLevel(4); });

        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(delegate { game.StartPauseGame(); });

        stopButton.onClick.RemoveAllListeners();
        stopButton.onClick.AddListener(delegate { game.StopGame(); });

        runButton.onClick.RemoveAllListeners();
        runButton.onClick.AddListener(delegate { game.RunGame(); });
    }

    void LoadFields()
    {
        BaseGame game = getGame();
        game.gameStockGO = gameObject;
        game.ghost = ghost;
        game.player = player;
        game.barriers = barriers;
        game.levels = levels;
    }

    public void AppendLog(string t)
    {
        BaseGame game = getGame();

        if (t != "")
        {
            t = $"{ DateTime.Now}: {t}";
        }
        else if (game != null && game.logText != "")
        {
            t = $"{ DateTime.Now}: {game.logText}";
            game.logText = String.Empty;
        }
        else
        {
            return;
        }

        console.text += $"{t}";
        console.verticalScrollbar.value = 1;
    }

    private BaseGame getGame()
    {
        return GetComponent<BaseGame>();
    }
}
