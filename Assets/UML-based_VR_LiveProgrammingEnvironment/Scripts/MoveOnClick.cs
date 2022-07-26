using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MoveOnClick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public GameObject vertex;
    public void OnPointerDown(PointerEventData eventData)
    {
        vertex.GetComponent<DragMove>().canMove = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("Move button is up");
    }
}
