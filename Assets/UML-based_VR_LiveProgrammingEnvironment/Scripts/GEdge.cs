using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GEdge
{
    public int ID { get; set; }
    public string type { get; set; }
    public GNode from { get; set; }
    public GNode to { get; set; }
    public string fromMulti { get; set; }
    public string toMulti { get; set; }
    public GameObject go { get; set; }

    public GEdge(int i, string ty, GNode f, GNode t, string s, string e, GameObject g)
    {
        ID = i;
        type = ty;
        from = f;
        to = t;
        fromMulti = s;
        toMulti = e;
        go = g;
    }
}
