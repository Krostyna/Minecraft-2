using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    public GameObject mainMenuObject;
    public GameObject settingsObject;

    [Header("Main Menu UI Elements")]
    public TextMeshProUGUI seedField;

    [Header("Settings Menu UI Elements")]
    public Slider viewDistanceSlider;
    public TextMeshProUGUI viewDistanceText;
    public Slider mouseSlider;
    public TextMeshProUGUI mouseTextSlider;
    public Toggle threadingToggle;

    Settings settings;

    private void Awake()
    {

        if (File.Exists(Application.dataPath + "/settings.cfg"))
        {
            Debug.Log("Settings file found. Loading file.");

            string jsonImport = File.ReadAllText(Application.dataPath + "/settings.cfg");
            settings = JsonUtility.FromJson<Settings>(jsonImport);
        }
        else
        {
            Debug.Log("No settings file found, creating new one.");

            settings = new Settings();
            string jsonExport = JsonUtility.ToJson(settings);
            File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);
        }
    }

    public void StartGame()
    {
        VoxelData.seed = Mathf.Abs(seedField.text.GetHashCode()) / VoxelData.WorldSizeInChunks;
        Debug.Log(VoxelData.seed);
        if(VoxelData.seed.ToString().Length > 6)
        {
            VoxelData.seed = int.Parse(VoxelData.seed.ToString().Substring(0, 6));
        }
        VoxelData.worldName = seedField.text;
        SceneManager.LoadScene("World", LoadSceneMode.Single);
    }

    public void EnterSettings()
    {
        viewDistanceSlider.value = settings.viewDistance;
        UpdateViewDistanceSlider();
        viewDistanceText.text = "View Distance: " + viewDistanceSlider.value;

        mouseSlider.value = settings.mouseSensitivity;
        UpdateMouseSlider();
        mouseTextSlider.text = "Mouse Sensitivity: " + mouseSlider.value.ToString("F1");

        threadingToggle.isOn = settings.enableThreading;

        mainMenuObject.SetActive(false);
        settingsObject.SetActive(true);
    }

    public void LeaveSettings()
    {
        settings.viewDistance = (int)viewDistanceSlider.value;
        settings.mouseSensitivity = mouseSlider.value;
        settings.enableThreading = threadingToggle.isOn;

        string jsonExport = JsonUtility.ToJson(settings);
        File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);

        mainMenuObject.SetActive(true);
        settingsObject.SetActive(false);
    }

    public void QuitGame()
    {
        //If we are running in a standalone build of the game
        #if UNITY_STANDALONE
        //Quit the application
        Application.Quit();
        #endif

        //If we are running in the editor
        #if UNITY_EDITOR
        //Stop playing the scene
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public void UpdateViewDistanceSlider()
    {
        viewDistanceText.text = "View Distance: " + viewDistanceSlider.value;
    }

    public void UpdateMouseSlider()
    {
        mouseTextSlider.text = "Mouse Sensitivity: " + mouseSlider.value.ToString("F1");
    }
}
