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
  
    
    // D-040: NEXT MISSION is the strongest "more" a player gives, so an offer opens first; NO THANKS, PLAY (or a
    // finished purchase) goes on to the mission.
    public void NextLv()
    {
        if (TreadShredNextMissionOffer.ShowBeforeNext(TreadShredUi.FindFont(GameObject.Find("Canvas")), LoadNextMission))
            return;
        LoadNextMission();
    }

    private void LoadNextMission()
    {
        string level = Application.loadedLevelName;
        level = level.Remove(2) + (int.Parse(level.Substring(2)) + 1).ToString();
        Application.LoadLevel(level);
    }

    public void Retry()
    {
        string level = Application.loadedLevelName;
        ManagerScore.BeginNewRun();
        Application.LoadLevel(level);
    }

    public void LoadMenu()
    {
        ManagerScore.EndRun();
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
            TreadShredOverdriveBadge.SetVisible(false);
            TreadShredWeaponToggle.SetVisible(false);
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
           
            CountObj.GetComponent<Animator>().enabled = true;
            PauseGameObj.SetActive(false);
            TreadShredOverdriveBadge.SetVisible(true);
            TreadShredWeaponToggle.SetVisible(true);
            BgIg.SetActive(true);
            KillScore.SetActive(true);
            Heart.SetActive(true);
            Styte.SetActive(true);
        }
    }

}
