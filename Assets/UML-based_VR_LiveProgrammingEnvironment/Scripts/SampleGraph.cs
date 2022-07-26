using System.Collections;
using UnityEngine;

public class SampleGraph : MonoBehaviour {

	public GameObject graphPrefab;
    public GameObject nodePrefab;
    public GameObject edgePrefab;
	public Graph graph;

	private IEnumerator LayoutTest()
	{
		//relayout test after 10s
		yield return new WaitForSecondsRealtime(10);
		graph.Layout();
	}

	void Start () {
		//Instantiate graph
		var go = GameObject.Instantiate(graphPrefab);
		go.transform.SetParent(transform);
		graph = go.GetComponent<Graph>();

		//Generate
		Generate();

		//Relayout after 10s, for testing purposes only
		//StartCoroutine(LayoutTest());

		Invoke("removeMesh", 2f);
	}

	void removeMesh()
	{
		var mesh = gameObject.GetComponent<MeshCollider>();
		if (mesh != null) mesh.enabled = false;
	}

    public void Generate()
	{
		graph.Layout();
	}
}
