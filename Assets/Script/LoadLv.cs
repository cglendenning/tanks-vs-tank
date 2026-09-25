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
    Coroutine restoreStartLayoutRoutine;
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

        // The settings animator can leave the legacy Control container with
        // its old 100x100 layout after a round trip. Reassert the command
        // layout once the scene is live so the three start-screen controls
        // never collapse onto one another.
        RestoreStartScreenLayout();
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
            RestoreStartScreenLayout();
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
        RestoreStartScreenLayout();
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
        if (restoreStartLayoutRoutine != null)
            StopCoroutine(restoreStartLayoutRoutine);
        restoreStartLayoutRoutine = StartCoroutine(RestoreStartScreenLayoutAfterSettings());
    }

    private IEnumerator RestoreStartScreenLayoutAfterSettings()
    {
        // Let the OptionBack transition finish before writing the final
        // positions. This prevents the animation from immediately overwriting
        // the corrected anchors on the same frame.
        yield return new WaitForSecondsRealtime(0.45f);
        RestoreStartScreenLayout();
        restoreStartLayoutRoutine = null;
    }

    private void RestoreStartScreenLayout()
    {
        if (PanelStart == null)
            return;

        var control = PanelStart.transform.Find("Control");
        if (control == null)
            return;

        var controlRect = control.GetComponent<RectTransform>();
        if (controlRect != null)
        {
            controlRect.anchorMin = Vector2.zero;
            controlRect.anchorMax = Vector2.one;
            controlRect.offsetMin = Vector2.zero;
            controlRect.offsetMax = Vector2.zero;
            controlRect.anchoredPosition = Vector2.zero;
            controlRect.sizeDelta = Vector2.zero;
            controlRect.localRotation = Quaternion.identity;
            controlRect.localScale = Vector3.one;
        }

        RestoreStartControl(control.Find("Option"), new Vector2(0.20f, 0.16f), 146f);
        RestoreStartControl(control.Find("Stats"), new Vector2(0.50f, 0.10f), 108f);
        RestoreStartControl(control.Find("Play"), new Vector2(0.80f, 0.16f), 146f);
    }

    private static void RestoreStartControl(Transform control, Vector2 anchor, float size)
    {
        if (control == null)
            return;

        var rect = control.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size, size);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
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
