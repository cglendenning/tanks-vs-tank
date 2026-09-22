using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LoadLv : MonoBehaviour {

    public static LoadLv instance;
    bool isSoundStart;
    public AudioSource Outgame;


    public GameObject PanelMenu;
    public GameObject PanelStart;
    Animator anim;
    void Start()
    {
       // PlayerPrefs.DeleteAll();
        instance = this;
        anim = GetComponent<Animator>();

        if (PlayerPrefs.HasKey("Sound") == false)
        {
            PlayerPrefs.SetFloat("Sound", 0);
            PlayerPrefs.Save();
        }

        if (PlayerPrefs.HasKey("Music") == false)
        {
            PlayerPrefs.SetFloat("Music", 0);
            PlayerPrefs.Save();
        }

        if (PlayerPrefs.HasKey("Vibrate") == false)
        {
            PlayerPrefs.SetFloat("Vibrate", 0);
            PlayerPrefs.Save();
        }

        if (PlayerPrefs.HasKey("Camera") == false)
        {
            PlayerPrefs.SetFloat("Camera", 0);
            PlayerPrefs.Save();
        }

        if (PlayerPrefs.GetFloat("Sound") == 0)
        {
            SoundOn.SetActive(true);
            SoundOff.SetActive(false);
           
        }
        else
        {
            SoundOn.SetActive(false);
            SoundOff.SetActive(true);
            
        }

        if (PlayerPrefs.GetFloat("Music") == 0)
        {
            MusicdOn.SetActive(true);
            MusicOff.SetActive(false);
            isSoundStart = false;
        }
        else
        {
            MusicdOn.SetActive(false);
            MusicOff.SetActive(true);
            isSoundStart = true;
        }

        if (PlayerPrefs.GetFloat("Camera") == 0)
        {

            CameraFllow.SetActive(true);
            CameraFixed.SetActive(false);
           
        }
        else
        {
            CameraFllow.SetActive(false);
            CameraFixed.SetActive(true);
          
        }

        //if (PlayerPrefs.GetFloat("Vibrate") == 0)
        //{
        //    isrung = false;
        //}
        //else
        //{
        //    isrung = true;
        //}

        

        if (isSoundStart == false)
        {
            Outgame.Play();
        }
    }

    public void StopMS()
    {
        if (isSoundStart == false)
        {
            Outgame.Stop();
        }
    }

    public void Loadlevel(string lv)
    {
        Application.LoadLevel(lv);
    }

    public void Fade()
    {
        anim.SetTrigger("Fade");
    }

    public void Choose()
    {
        if (PanelStart.activeInHierarchy)
        {
            PanelStart.SetActive(false);
            PanelMenu.SetActive(true);
            
        }
        else
        {
            PanelStart.SetActive(true);
            PanelMenu.SetActive(false);
        }
    }
  
    public void PlayToMenu()
    {
        PanelStart.SetActive(false);
        PanelMenu.SetActive(true);
    }

    public void BackToPlay()
    {
        PanelStart.SetActive(true);
        PanelMenu.SetActive(false);
    }


    public void OpenUrl(int index)
    {

        switch (index)
        {
            case 1:
                Application.OpenURL("https://www.facebook.com/Pet-Subway-Surfers-1184371881583532/");
                 break;
            case 2:
                 Application.OpenURL("https://plus.google.com/u/0/communities/118128855802044421559");
                 break;
            default:
                break;
        }
      
    }

    //public bool isrung;

    //public void Rung()
    //{
    //    if (isrung == false)
    //    {
    //        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
    //        {
    //            if (GUI.Button(new Rect(0, 10, 100, 32), "Vibrate!"))
    //                Handheld.Vibrate();
    //        }
    //    }
        
      
    //}

    public void Option()
    {
        anim.SetTrigger("Option");
    }

    public void OptionBack()
    {
        anim.SetTrigger("OptionBack");
    }

    public Text ScoreStatic;
    public Text KillStatic;
    public Text RankStatic;

    public void Static()
    {
        anim.SetTrigger("Static");

        int TotalHighScore = 0;
        for (int i = 1; i <= PlayerPrefs.GetFloat("OpenLv"); i++)
        {
            TotalHighScore += (int)PlayerPrefs.GetFloat("Lvs" + i);
        }

        ScoreStatic.text = "TOTAL SCORE // " + TotalHighScore.ToString();
        KillStatic.text = "CONFIRMED KILLS // " + PlayerPrefs.GetFloat("CountKill").ToString();

        if (TotalHighScore > 10000 && TotalHighScore < 23000)
        {
            RankStatic.text = "RANK // CORPORAL";
        }
        else if (TotalHighScore >= 23000 && TotalHighScore < 47000)
        {
            RankStatic.text = "RANK // SERGEANT";
        }
        else if (TotalHighScore >= 47000)
        {
            RankStatic.text = "RANK // STAFF SERGEANT";
        }
        else
        {
            RankStatic.text = "RANK // PRIVATE";
        }
	
	
    }



    public void StaticBack()
    {
        anim.SetTrigger("StaticBack");
    }

    public GameObject SoundOn;
    public GameObject SoundOff;
    public void SoundGame()
    {
        if (SoundOn.activeInHierarchy)
        {
            SoundOn.SetActive(false);
            SoundOff.SetActive(true);
            PlayerPrefs.SetFloat("Sound",1);
            PlayerPrefs.Save();
        }
        else
        {
            SoundOn.SetActive(true);
            SoundOff.SetActive(false);
            PlayerPrefs.SetFloat("Sound", 0);
            PlayerPrefs.Save();
        }
    }

    public GameObject MusicdOn;
    public GameObject MusicOff;
    public void MusicGame()
    {
        if (MusicdOn.activeInHierarchy)
        {
            
            Outgame.Stop();
            MusicdOn.SetActive(false);
            MusicOff.SetActive(true);
            PlayerPrefs.SetFloat("Music", 1);
            PlayerPrefs.Save();
        }
        else
        {
            Outgame.Play();
            MusicdOn.SetActive(true);
            MusicOff.SetActive(false);
            PlayerPrefs.SetFloat("Music", 0);
            PlayerPrefs.Save();
        }
    }

    public GameObject ViBrateOn;
    public GameObject ViBrateOff;
    public void ViBrateGame()
    {
        if (ViBrateOn.activeInHierarchy)
        {
            ViBrateOn.SetActive(false);
            ViBrateOff.SetActive(true);
            PlayerPrefs.SetFloat("Vibrate", 1);
            PlayerPrefs.Save();
        }
        else
        {
            ViBrateOn.SetActive(true);
            ViBrateOff.SetActive(false);
            PlayerPrefs.SetFloat("Vibrate", 0);
            PlayerPrefs.Save();
        }
    }

    public GameObject CameraFllow;
    public GameObject CameraFixed;
    public void CameraGame()
    {
        if (CameraFllow.activeInHierarchy)
        {
            CameraFllow.SetActive(false);
            CameraFixed.SetActive(true);
            PlayerPrefs.SetFloat("Camera", 1);
            PlayerPrefs.Save();
        }
        else
        {
            CameraFllow.SetActive(true);
            CameraFixed.SetActive(false);
            PlayerPrefs.SetFloat("Camera", 0);
            PlayerPrefs.Save();
        }
    }

    public void ExitGame()
    {
         Application.Quit(); 
    }
}
