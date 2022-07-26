using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RecognitionButton : MonoBehaviour
{
    public Button emptyButton;
    public Button recognitionButton;
    public MyTMPInputField tmp_inputField;

    public SpeechRecognition speechRecognitionScript;

    // Start is called before the first frame update
    void Start()
    {
        emptyButton.onClick.AddListener(EmptyImputField);

        EventTrigger triggerDown = recognitionButton.gameObject.AddComponent<EventTrigger>();
        var pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((e) => StartRecordButtonOnClickHandler());
        triggerDown.triggers.Add(pointerDown);

        EventTrigger triggerUp = recognitionButton.gameObject.AddComponent<EventTrigger>();
        var pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((e) => StopRecordButtonOnClickHandler());
        triggerUp.triggers.Add(pointerUp);
    }

    private void EmptyImputField()
    {
        tmp_inputField.text = string.Empty;
    }

    private void StartRecordButtonOnClickHandler()
    {
        speechRecognitionScript.StartRecordButtonOnClickHandler(tmp_inputField);
    }

    private void StopRecordButtonOnClickHandler()
    {
        speechRecognitionScript.StopRecordButtonOnClickHandler();
    }
}
