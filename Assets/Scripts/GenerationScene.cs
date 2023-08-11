using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerationScene : MonoBehaviour
{
    public GameObject pausePanel;
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape) && !pausePanel.activeSelf)
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }
        else if(Input.GetKeyDown(KeyCode.Escape) && pausePanel.activeSelf)
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }
    public void QuitApp()
    {
        Application.Quit();
    }
}
