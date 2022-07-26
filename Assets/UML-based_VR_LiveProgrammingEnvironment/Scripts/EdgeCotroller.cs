using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class EdgeCotroller : MonoBehaviour, IPointerClickHandler
{
    public UnityEvent onClick;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("click on line");
        onClick.Invoke();
    }
}
