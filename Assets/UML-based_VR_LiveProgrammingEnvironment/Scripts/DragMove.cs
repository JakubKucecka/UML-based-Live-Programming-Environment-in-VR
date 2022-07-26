using UnityEngine;
using UnityEngine.EventSystems;
public class DragMove : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public bool canMove;
    Canvas c;

    public void Start()
    {
        c = GetComponentInParent<Canvas>();
        canMove = false;
    }

    public void OnDrag(PointerEventData data)
    {
        if (canMove)
        {
            transform.SetAsLastSibling();

            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(c.transform as RectTransform, data.position, c.worldCamera, out pos);
            transform.position = c.transform.TransformPoint(pos);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canMove = false;
    }
}
