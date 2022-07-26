using Microsoft.Msagl.Core.Layout;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UNode : Unit
{
	public Node GraphNode { get; set; }

	public Rect Size {
		get
		{
			return GetComponent<RectTransform>().rect;
		}
	}

	private Rect oldSize;

	protected override void OnDestroy()
	{
		graph.RemoveNode(gameObject);
	}

	private void Start()
	{
		oldSize = GetComponent<RectTransform>().rect;
	}

	private void Update()
	{
		var size = GetComponent<RectTransform>().rect;
		if (transform.hasChanged || oldSize != size)
		{
			graph.UpdateGraph();
			transform.hasChanged = false;
			oldSize = size;
		}
	}
}
