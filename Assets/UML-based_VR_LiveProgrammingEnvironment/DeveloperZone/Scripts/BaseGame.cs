using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseGame : MonoBehaviour
{
    public GameObject gameStockGO;
    public GameObject player;
    public GameObject ghost;
    public List<GameObject> levels = new List<GameObject>();
    public List<GameObject> barriers;

    public string logText = "";
    public int level;

    public virtual void StartPauseGame()
    {
        Debug.Log("StartPauseGame");
    }

    public virtual void StopGame()
    {
        Debug.Log("StopGame");
    }

    public virtual void RunGame()
    {
        Debug.Log("RunGame");
    }

    public virtual void MovePlayer(Vector3 dir)
    {
        Debug.Log("MovePlayer");
    }

    public virtual void ShowLevel(int levelNumber)
    {
        Debug.Log("ShowLevel");
    }

    public virtual void RestartGame()
    {
        Debug.Log("RestartGame");
    }
}
