using System;
using System.IO;
using LearnXR.Core;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;
using MagicLeap.OpenXR.Features.EyeTracker;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using InputDevice = UnityEngine.XR.InputDevice;
using Logger = LearnXR.Core.Logger;

public class GazeInputManager : MonoBehaviour
{
    private MagicLeapEyeTrackerFeature eyeTrackerFeature;
    public bool EyeTrackingPermissionGranted { get; private set; }
    public Vector3 HeadsetPosition { get; private set; }
    public Quaternion HeadsetRotation { get; private set; }
    public Vector3 GazeDirection { get; private set; }
    public float LeftPupilDilation { get; private set; }
    public float RightPupilDilation { get; private set; }
    public bool IsLeftEyeBlinking { get; private set; }
    public bool IsRightEyeBlinking { get; private set; }
    private StreamWriter csvWriter;
    private string filePath;

    private float timeSinceLastLog = 0f;
    private const float logInterval = 1f;

    void Start()
    {
        MagicLeap.Android.Permissions.RequestPermissions(
            new string[] { MagicLeap.Android.Permissions.EyeTracking, MagicLeap.Android.Permissions.PupilSize },
            OnPermissionGranted,
            OnPermissionDenied
        );

        string timestamp = System.DateTime.Now.ToString("MM-dd_HH-mm-ss");
        filePath = Path.Combine(Application.persistentDataPath, $"GD-{timestamp}.csv");
        Debug.Log($"Attempting to create CSV file at: {filePath}");

        try
        {
            csvWriter = new StreamWriter(filePath, false);
            csvWriter.WriteLine("Timestamp,HeadsetPosX,HeadsetPosY,HeadsetPosZ,HeadsetRotX,HeadsetRotY,HeadsetRotZ,HeadsetRotW," +
                                "GazeDirectionX,GazeDirectionY,GazeDirectionZ," +
                                "GazeAmplitude,GazeVelocity,DirectionAngle," +
                                "LeftPupilDilation,RightPupilDilation,LeftBlink,RightBlink," +
                                "ControllerPosX,ControllerPosY,ControllerPosZ,ControllerRotX,ControllerRotY,ControllerRotZ,ControllerRotW,TriggerPress");
            Debug.Log("CSV file created at: " + filePath);
        }
        catch (IOException e)
        {
            Debug.LogError("Failed to create CSV file: " + e.Message);
        }

        eyeTrackerFeature = OpenXRSettings.Instance?.GetFeature<MagicLeapEyeTrackerFeature>();
        if (eyeTrackerFeature == null || !eyeTrackerFeature.enabled)
        {
            Debug.LogError("MagicLeapEyeTrackerFeature is not available or not enabled.");
            return;
        }

        try
        {
            eyeTrackerFeature.CreateEyeTracker();
            Debug.Log("Eye Tracker initialized.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create eye tracker: {ex.Message}");
        }
    }

    void Update()
    {
        Debug.Log("hello");
        InputDevice controller = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        bool triggerPressed = false;

        if (controller.isValid)
        {
            controller.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
        }

        if (triggerPressed)
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 10f))
            {
                var prefabHandler = hit.collider.GetComponent<PrefabClickHandler>();
                if (prefabHandler != null)
                {
                    prefabHandler.OnPointerClick();
                }
            }
        }
        if (!EyeTrackingPermissionGranted)
        {
            Debug.LogWarning("Eye tracking permission is not granted.");
            return;
        }

        if (eyeTrackerFeature == null || !eyeTrackerFeature.enabled)
        {
            Debug.LogWarning("Eye tracking feature is not available or initialized.");
            return;
        }

        EyeTrackerData data;
        try
        {
            data = eyeTrackerFeature.GetEyeTrackerData();
        }
        catch (NullReferenceException)
        {
            return;
        }

        if (data.PupilData == null || data.PupilData.Length < 2 || data.GeometricData == null || data.GeometricData.Length < 2)
        {
            Debug.LogWarning("Eye tracker data is incomplete.");
            return;
        }

        GazeBehavior gazeBehavior = data.GazeBehaviorData;
        if (gazeBehavior.MetaData.Direction == 0f && gazeBehavior.MetaData.Amplitude == 0f)
        {
            Debug.LogWarning("Invalid GazeBehavior data.");
            return;
        }

        float horizontalAngle = gazeBehavior.MetaData.Direction;
        float verticalAngle = Mathf.Deg2Rad * gazeBehavior.MetaData.Amplitude;
        GazeDirection = new Vector3(
            Mathf.Cos(verticalAngle) * Mathf.Sin(horizontalAngle),
            Mathf.Sin(verticalAngle),
            Mathf.Cos(verticalAngle) * Mathf.Cos(horizontalAngle)
        );

        LeftPupilDilation = data.PupilData[0].PupilDiameter;
        RightPupilDilation = data.PupilData[1].PupilDiameter;
        IsLeftEyeBlinking = data.GeometricData[0].EyeOpenness < 0.2f;
        IsRightEyeBlinking = data.GeometricData[1].EyeOpenness < 0.2f;

        HeadsetPosition = Camera.main.transform.position;
        HeadsetRotation = Camera.main.transform.rotation;

        timeSinceLastLog += Time.deltaTime;
        if (timeSinceLastLog >= logInterval)
        {
            LogEyeTrackingData();
            timeSinceLastLog = 0f;
        }
    }

    private void LogEyeTrackingData()
    {
        InputDevice controller = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        Vector3 controllerPosition = Vector3.zero;
        Quaternion controllerRotation = Quaternion.identity;
        float triggerPress = 0f;

        if (controller.isValid)
        {
            controller.TryGetFeatureValue(CommonUsages.devicePosition, out controllerPosition);
            controller.TryGetFeatureValue(CommonUsages.deviceRotation, out controllerRotation);
            controller.TryGetFeatureValue(CommonUsages.trigger, out triggerPress);
        }

        string timestamp = System.DateTime.Now.ToString();
        string dataLine = $"{timestamp},{HeadsetPosition.x},{HeadsetPosition.y},{HeadsetPosition.z}," +
                          $"{HeadsetRotation.x},{HeadsetRotation.y},{HeadsetRotation.z},{HeadsetRotation.w}," +
                          $"{GazeDirection.x},{GazeDirection.y},{GazeDirection.z}," +
                          $"{GazeDirection.magnitude},{GazeDirection.magnitude / Time.deltaTime}," +
                          $"{GazeDirection.z},{LeftPupilDilation},{RightPupilDilation},{IsLeftEyeBlinking},{IsRightEyeBlinking}," +
                          $"{controllerPosition.x},{controllerPosition.y},{controllerPosition.z}," +
                          $"{controllerRotation.x},{controllerRotation.y},{controllerRotation.z},{controllerRotation.w},{triggerPress}";

        csvWriter.WriteLine(dataLine);
        Logger.Instance.LogInfo($"Data Recorded: {dataLine}");
    }

    private void OnPermissionDenied(string permission)
    {
        Logger.Instance.LogError($"{permission} permission denied.");
    }

    private void OnPermissionGranted(string permission)
    {
        if (permission == MagicLeap.Android.Permissions.EyeTracking || permission == MagicLeap.Android.Permissions.PupilSize)
        {
            EyeTrackingPermissionGranted = true;
        }
    }

    private void OnApplicationQuit()
    {
        csvWriter?.Close();
        if (eyeTrackerFeature != null && eyeTrackerFeature.enabled)
        {
            eyeTrackerFeature.DestroyEyeTracker();
            Debug.Log("Eye Tracker destroyed.");
        }
    }
}
