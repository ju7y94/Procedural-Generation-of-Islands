using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuScene : MonoBehaviour
{
    public static MainMenuScene mainMenuScene;
    public TMP_InputField seedInputField;
    public int seedNumber;
    public TMP_Text seedNullMessage;

    private void Awake()
    {
        if(mainMenuScene == null)
        {
            mainMenuScene = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadGenerationScene()
    {
        if (int.TryParse(seedInputField.text, out seedNumber))
        {
            SceneManager.LoadSceneAsync("IslandGeneration");
        }
        else
        {
            seedNullMessage.text = "Invalid seed value. Please enter a valid integer!";
        }
    }

    public void QuitApp()
    {
        Application.Quit();
    }
}
