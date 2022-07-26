using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandControllersSwitcher : MonoBehaviour
{
    [Header("Hand Tracking Setup")]
    public OVRHand LeftHand;
    public OVRHand RightHand;

    bool wasHandTracking;
    bool isStart;

    public Transform LeftModelHolder;
    public Transform RightModelHolder;
    public GameObject laser;

    public bool IsHandTracking = false;

    void Update()
    {
        updateHandTracking();
    }

    void updateHandTracking()
    {
        IsHandTracking = OVRPlugin.GetHandTrackingEnabled() || OVRInput.GetActiveController() == OVRInput.Controller.Hands;

        if (isStart || IsHandTracking != wasHandTracking)
        {
            onHandTrackingChange(IsHandTracking);
        }

        wasHandTracking = IsHandTracking;
        isStart = false;
    }

    void onHandTrackingChange(bool handTrackingEnabled)
    {
        // We'll consider a controller active for anything but Hands
        LeftModelHolder.gameObject.SetActive(!handTrackingEnabled);
        RightModelHolder.gameObject.SetActive(!handTrackingEnabled);
        laser.SetActive(!handTrackingEnabled);
        LeftHand.gameObject.SetActive(handTrackingEnabled);
        RightHand.gameObject.SetActive(handTrackingEnabled);
    }

}
