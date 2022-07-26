using System;
using TMPro;
using UnityEngine;

public class KeyboardScript : MonoBehaviour
{
    private MyTMPInputField TextField;
    private int selectionPosition = 0;
    private int selectionFocusPosition = 0;
    private int bias = 0;

    public GameObject EngSmall, EngBig, Layout;
    public GameObject context;

    private bool isShift = false;
    private bool isCaps = false;
    MyTMPInputField[] inputs;

    private void Start()
    {
        inputs = context.GetComponentsInChildren<MyTMPInputField>(true);
        ShowLayout(EngSmall);
    }

    private void Update()
    {
        //if (context != null && (TextField == null || (TextField != null && !TextField.isFocused)))
        if (context != null)
        {
            foreach (MyTMPInputField i in inputs)
            {
                if (i.isFocused)
                {
                    TextField = i;
                    bias = 0;
                    selectionFocusPosition = i.selectionFocusPosition;
                    break;
                }
            }

            selectionPosition = selectionFocusPosition + bias > 0 ? selectionFocusPosition + bias : 0;
        }
    }

    void ChangeLayoutIfShift()
    {
        if (isShift)
        {
            isShift = false;
            ShowLayout(EngSmall);
        }
    }

    public void alphabetFunction(string alphabet)
    {
        if (TextField != null)
            TextField.text = TextField.text.Insert(selectionPosition, alphabet);
        bias += 1;
        ChangeLayoutIfShift();
    }

    public void newLineFunction()
    {
        if (TextField != null)
            TextField.text = TextField.text.Insert(selectionPosition, "\n");
        bias += 1;
    }

    public void tabFunction()
    {
        if (TextField != null)
            TextField.text = TextField.text.Insert(selectionPosition, "\t");
        bias += 1;
    }

    public void backspaceFunction()
    {
        if (TextField != null)
            if (TextField.selectionAnchorPosition != TextField.selectionFocusPosition)
            {
                int start = TextField.selectionAnchorPosition < TextField.selectionFocusPosition ? TextField.selectionAnchorPosition : TextField.selectionFocusPosition;
                int end = TextField.selectionAnchorPosition < TextField.selectionFocusPosition ? TextField.selectionFocusPosition : TextField.selectionAnchorPosition;
                TextField.text = TextField.text.Remove(start, end - start);
            }
            else
            {
                if (TextField.text.Length > 0 && selectionPosition - 1 >= 0) TextField.text = TextField.text.Remove(selectionPosition - 1, 1);
            }
        bias -= 1;
    }

    public void shiftFunction()
    {
        if (isShift)
        {
            isShift = false;
            ShowLayout(EngSmall);
        }
        else
        {
            isShift = true;
            ShowLayout(EngBig);
        }
    }

    public void capsFunction()
    {
        if (isCaps)
        {
            isCaps = false;
            ShowLayout(EngSmall);
        }
        else
        {
            isCaps = true;
            ShowLayout(EngBig);
        }
    }

    public void Delete()
    {
        if (TextField != null)
            if (TextField.text.Length > selectionPosition) TextField.text = TextField.text.Remove(selectionPosition, 1);
    }

    public void CloseAllLayouts()
    {
        EngSmall.SetActive(false);
        EngBig.SetActive(false);
        Layout.SetActive(false);
    }

    public void ShowLayout(GameObject SetLayout)
    {
        CloseAllLayouts();
        Layout.SetActive(true);
        SetLayout.SetActive(true);
    }
}
