using System;
using System.Collections.Generic;
using UnityEngine;

public class GNode
{
    public int ID { get; set; }
    public string Nname { get; set; }
    public string type { get; set; }
    public string namespaceName { get; set; }
    public List<string[]> attributes { get; set; }
    public List<string[]> opperation { get; set; }
    public string path { get; set; }
    public GameObject go { get; set; }

    public GNode(int i, string n, string t, string nn, string a, string o, string p, GameObject g)
    {
        ID = i;
        Nname = n;
        type = t;
        namespaceName = nn;
        attributes = getListFromString(a);
        opperation = getListFromString(o);
        path = p;
        go = g;
    }

    public List<string[]> getListFromString(string s)
    {
        s = s.Replace(" ", "").Replace("\r", "");
        string[] textLines = s.Split(new[] { "\n" }, StringSplitOptions.None);

        var items = new List<string[]>();
        foreach (string line in textLines)
        {
            if (line.Length >= 2)
            {
                var lineCharNum = line.Length;
                var newLine = line;
                while (lineCharNum > 0)
                {
                    if (newLine[0].ToString() == "+" || newLine[0].ToString() == "-")
                    {
                        newLine = newLine.Substring(1);
                        lineCharNum -= 1;
                    }
                    else
                    {
                        break;
                    }
                }
                var lineArray = newLine.Split(':');
                items.Add(new string[] { lineArray[0], lineArray[1] });
            }
        }

        return items;
    }
}
