using Microsoft.Msagl.Core.Layout;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

public class UEdge : Unit
{
	public Edge graphEdge { get; set; }

	public float Width {
		get
		{
			var lr = GetComponent<UILineRenderer>();
			return lr.LineThickness;
        }
	}

	protected override void OnDestroy()
	{
		graph.RemoveEdge(gameObject);
	}
}
