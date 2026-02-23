using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraphicsSettings : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown resDropDown;
    [SerializeField] private Toggle fullScreenToggle;
    [SerializeField] private Toggle VSyncToggle;
    private Resolution[] allResolutions;
    private bool IsFullScreen;
    private bool VSync;
    private int selectedResolution;
    private List<Resolution> selectedResolutionList = new List<Resolution>();

    private void Start()
    {
        DisplayResolutions();

        GetPlayersGrapicsSettings();
    }

    public void QuitButton_OnClick()
    {
        Application.Quit();
    }

    private void SaveNewPlayerSettings()
    {
        PlayerPrefs.SetInt("IsFullScreen", (IsFullScreen? 1 : 0));
        PlayerPrefs.SetInt("selectedResolution", selectedResolution);
        PlayerPrefs.SetInt("VSync", (VSync? 1 : 0));
    } 
    
    private void GetPlayersGrapicsSettings()
    {
        IsFullScreen = (PlayerPrefs.GetInt("IsFullScreen") == 0? false : true);
        selectedResolution = PlayerPrefs.GetInt("selectedResolution", selectedResolution);
        VSync = (PlayerPrefs.GetInt("VSync") == 0 ? false : true);

        VSyncToggle.isOn = VSync;
        fullScreenToggle.isOn = IsFullScreen;
        resDropDown.value = selectedResolution;
    }

    public void ChangeResolution()
    {
        selectedResolution = resDropDown.value;
        Screen.SetResolution(selectedResolutionList[selectedResolution].width, selectedResolutionList[selectedResolution].height, IsFullScreen);
        SaveNewPlayerSettings();
    }

    public void ChangeFullScreen()
    {
        IsFullScreen = fullScreenToggle.isOn;
        Screen.SetResolution(selectedResolutionList[selectedResolution].width, selectedResolutionList[selectedResolution].height, IsFullScreen);
        SaveNewPlayerSettings();
    }

    private void DisplayResolutions()
    {
        allResolutions = Screen.resolutions;

        List<string> resolutionStringList = new List<string>();
        string newRes;
        foreach (Resolution resolution in allResolutions)
        {
            newRes = resolution.width.ToString() + " x " + resolution.height.ToString();
            if (!resolutionStringList.Contains(newRes))
            {
                resolutionStringList.Add(newRes);
                selectedResolutionList.Add(resolution);
            }
        }
        resDropDown.AddOptions(resolutionStringList);
    }

    public void ChangeVsync()
    {
        VSync = VSyncToggle.isOn;
        QualitySettings.vSyncCount = (VSync == false ? 0 : 1);
        SaveNewPlayerSettings();
    }
    
    public void ChangeBrightness()
    {

    }
}
