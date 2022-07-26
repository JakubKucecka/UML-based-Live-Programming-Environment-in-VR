using FrostweepGames.Plugins.GoogleCloud.SpeechRecognition;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpeechRecognition : MonoBehaviour
{
    private GCSpeechRecognition speechRecognition;
    public Button refreshButton;
    public TMP_Dropdown microphoneDevicesDropdown;
    public MyTMPInputField tmp_inputField;
    public HomeController homeController;

    private void Start()
    {
        speechRecognition = GCSpeechRecognition.Instance;
        speechRecognition.RecognizeSuccessEvent += RecognizeSuccessEventHandler;
        speechRecognition.RecognizeFailedEvent += RecognizeFailedEventHandler;
        speechRecognition.FinishedRecordEvent += FinishedRecordEventHandler;
        speechRecognition.StartedRecordEvent += StartedRecordEventHandler;
        speechRecognition.RecordFailedEvent += RecordFailedEventHandler;

        refreshButton.onClick.AddListener(RefreshMicsButtonOnClickHandler);
        microphoneDevicesDropdown.onValueChanged.AddListener(MicrophoneDevicesDropdownOnValueChangedEventHandler);
        RefreshMicsButtonOnClickHandler();
    }

    private void RefreshMicsButtonOnClickHandler()
    {
        speechRecognition.RequestMicrophonePermission(null);
        microphoneDevicesDropdown.ClearOptions();

        for (int i = 0; i < speechRecognition.GetMicrophoneDevices().Length; i++)
        {
            microphoneDevicesDropdown.options.Add(new TMP_Dropdown.OptionData(speechRecognition.GetMicrophoneDevices()[i]));
        }

        //smart fix of dropdowns
        microphoneDevicesDropdown.value = 1;
        microphoneDevicesDropdown.value = 0;
    }

    private void MicrophoneDevicesDropdownOnValueChangedEventHandler(int value)
    {
        if (!speechRecognition.HasConnectedMicrophoneDevices()) return;
        speechRecognition.SetMicrophoneDevice(speechRecognition.GetMicrophoneDevices()[value]);
    }

    public void StartRecordButtonOnClickHandler(MyTMPInputField inputField)
    {
        tmp_inputField = inputField;
        speechRecognition.StartRecord(false);
    }

    public void StopRecordButtonOnClickHandler()
    {
        speechRecognition.StopRecord();
    }

    private void StartedRecordEventHandler()
    {
        Debug.Log("Recording ...");
    }

    private void RecordFailedEventHandler()
    {
        Debug.Log("Start record Failed. Please check microphone device and try again.");
    }

    private void FinishedRecordEventHandler(AudioClip clip, float[] raw)
    {
        if (clip == null)
            return;

        RecognitionConfig config = RecognitionConfig.GetDefault();
        config.languageCode = Enumerators.LanguageCode.sk_SK.Parse();
        config.audioChannelCount = clip.channels;

        GeneralRecognitionRequest recognitionRequest = new GeneralRecognitionRequest()
        {
            audio = new RecognitionAudioContent()
            {
                content = raw.ToBase64()
            },
            config = config
        };
        speechRecognition.Recognize(recognitionRequest);
    }

    private void RecognizeSuccessEventHandler(RecognitionResponse recognitionResponse)
    {
        Debug.Log("Recognize Success.");
        InsertRecognitionResponseInfo(recognitionResponse);
    }

    private void RecognizeFailedEventHandler(string error)
    {
        Debug.Log("Recognize Failed: " + error);
        homeController.showError("Recognize Failed: " + error);
    }

    private void InsertRecognitionResponseInfo(RecognitionResponse recognitionResponse)
    {
        if (recognitionResponse == null || recognitionResponse.results.Length == 0 || recognitionResponse.results[0].alternatives[0].transcript == null)
        {
            tmp_inputField.text += string.Empty;
            return;
        }

        tmp_inputField.text += CheckSpecialString(recognitionResponse.results[0].alternatives[0].transcript);
    }

    private string CheckSpecialString(string text)
    {
        string output;

        switch (text.ToLower())
        {
            case "nula":
                output = "0";
                break;
            case "jeden":
                output = "1";
                break;
            case "dva":
                output = "2";
                break;
            case "tri":
                output = "3";
                break;
            case "štyri":
                output = "4";
                break;
            case "pä":
                output = "5";
                break;
            case "šes":
                output = "6";
                break;
            case "sedem":
                output = "7";
                break;
            case "osem":
                output = "8";
                break;
            case "devä":
                output = "9";
                break;
            case "hviezdièka":
                output = "*";
                break;
            case "pomlèka":
                output = "-";
                break;
            case "podèiarkovník":
                output = "_";
                break;
            case "plus":
                output = "+";
                break;
            case "mínus":
                output = "-";
                break;
            case "krát":
                output = "*";
                break;
            case "deleno":
                output = "/";
                break;
            case "modulo":
                output = "%";
                break;
            case "rovné":
                output = "=";
                break;
            case "bodka":
                output = ".";
                break;
            case "èiarka":
                output = ",";
                break;
            default:
                output = text;
                break;
        }

        return output;
    }
}
