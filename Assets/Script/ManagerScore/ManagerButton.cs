using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class ManagerButton : MonoBehaviour {


    public GameObject BgIg;
    public GameObject KillScore;
    public GameObject Heart;
    public GameObject Styte;
    public GameObject PauseGameObj;

    public GameObject CountObj;
  
    
    public void NextLv()
    {

        string level = Application.loadedLevelName;
        level = level.Remove(2) + (int.Parse(level.Substring(2)) + 1).ToString();
        Application.LoadLevel(level);
    }

    public void Retry()
    {
        string level = Application.loadedLevelName;
        Application.LoadLevel(level);
    }

    public void LoadMenu()
    {
        Time.timeScale = 1;
        Application.LoadLevel("Start");
    }

    public void PauseGame()
    {
        if (!PauseGameObj.activeInHierarchy)
        {
            CountObj.GetComponent<Animator>().enabled = false;
          
            Styte.SetActive(false);
            BgIg.SetActive(false);
            KillScore.SetActive(false);
            Heart.SetActive(false);
            PauseGameObj.SetActive(true);
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
           
            CountObj.GetComponent<Animator>().enabled = true;
            PauseGameObj.SetActive(false);
            BgIg.SetActive(true);
            KillScore.SetActive(true);
            Heart.SetActive(true);
            Styte.SetActive(true);
        }
    }

}
