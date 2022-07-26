using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Linq;

public class CodeController : MonoBehaviour
{
    private MyTMPInputField inputField;
    public GameObject context;
    MyTMPInputField[] inputs;
    private List<string> prevTexts = new List<string>();
    private List<string> nextTexts = new List<string>();

    private void Start()
    {
        inputs = context.GetComponentsInChildren<MyTMPInputField>(true);
    }

    private void Update()
    {
        if (context != null && (inputField == null || (inputField != null && !inputField.isFocused)))
        {
            bool isFocused = false;
            foreach (MyTMPInputField i in inputs)
            {
                if (i.isFocused)
                {
                    isFocused = true;
                    Debug.Log("inputFiled: " + i.name);
                    inputField = i;
                    inputField.onValueChanged.RemoveAllListeners();
                    inputField.onValueChanged.AddListener(delegate { addToPrevTexts(); });

                    if (prevTexts.Count == 0)
                    {
                        prevTexts.Add(inputField.text);
                        nextTexts = new List<string>();
                    }

                        break;
                }
            }

            if (!isFocused)
            {
                prevTexts = new List<string>();
                nextTexts = new List<string>();
            }
        }

        if (inputField != null && inputField.isFocused && Input.GetKey(KeyCode.F7))
        {
            pullFromPrevTexts();
        }

        if (inputField != null && inputField.isFocused && Input.GetKey(KeyCode.F8))
        {
            pullFromNextTexts();
        }
    }

    public void addToPrevTexts()
    {
        if (prevTexts.Count > 100)
        {
            prevTexts.Remove(prevTexts[0]);
        }
        prevTexts.Add(inputField.text);
        nextTexts = new List<string>();
    }

    public void pullFromPrevTexts()
    {
        if (prevTexts.Count > 0)
        {
            inputField.onValueChanged.RemoveAllListeners();
            int textDiff = inputField.text.Length - prevTexts.Last().Length;
            inputField.text = prevTexts.Last();
            inputField.caretPosition -= textDiff;
            nextTexts.Add(prevTexts.Last());
            prevTexts.Remove(prevTexts.Last());
            inputField.onValueChanged.AddListener(delegate { addToPrevTexts(); });
        }
    }

    public void pullFromNextTexts()
    {
        if (nextTexts.Count > 0)
        {
            inputField.onValueChanged.RemoveAllListeners();
            int textDiff = inputField.text.Length - nextTexts.Last().Length;
            inputField.text = nextTexts.Last();
            inputField.caretPosition -= textDiff;
            prevTexts.Add(inputField.text);
            nextTexts.Remove(nextTexts.Last());
            inputField.onValueChanged.AddListener(delegate { addToPrevTexts(); });
        }
    }

    public void CopyToClipboard()
    {
        TextEditor textEditor = new TextEditor();
        int startPos, charCount;
        if (inputField.selectionStringAnchorPosition < inputField.selectionStringFocusPosition)
        {
            startPos = inputField.selectionStringAnchorPosition;
            charCount = inputField.selectionStringFocusPosition - inputField.selectionStringAnchorPosition;
        }
        else
        {
            startPos = inputField.selectionStringFocusPosition;
            charCount = inputField.selectionStringAnchorPosition - inputField.selectionStringFocusPosition;
        }
        textEditor.text = inputField.text.Substring(startPos, charCount);
        textEditor.SelectAll();
        textEditor.Copy();
    }

    public void PasteFromClipboard()
    {
        TextEditor textEditor = new TextEditor();
        textEditor.multiline = true;
        textEditor.Paste();
        inputField.text = inputField.text.Insert(inputField.caretPosition, textEditor.text);
    }
}
