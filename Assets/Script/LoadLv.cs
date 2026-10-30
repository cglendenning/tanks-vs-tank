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

    private void LateUpdate()
    {
        // Animator states from the legacy settings screen used to keep
        // writing the obsolete Control position after the transition ended.
        // Keep the command layout authoritative whenever the actual start
        // screen is visible, while leaving the settings/stats overlays free
        // to animate normally.
        if (ShouldEnforceStartScreenLayout())
            RestoreStartScreenLayout();
    }

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

        

        // Start each fresh command-deck session at the authored full level so
        // nothing that paused or faded the global listener earlier can leave
        // launch audio quiet or paused.
        AudioListener.pause = false;
        AudioListener.volume = 1f;

        if (isSoundStart == false)
        {
            if (Outgame != null)
                Outgame.Play();
        }

        // The settings animator can leave the legacy Control container with
        // its old 100x100 layout after a round trip. Reassert the command
        // layout once the scene is live so the three start-screen controls
        // never collapse onto one another.
        RestoreStartScreenLayout();
        RestoreSettingsInteractivity();
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
        RestoreSettingsInteractivity();
        anim.SetTrigger("Option");
    }

    public void OptionBack()
    {
        if (anim != null)
            anim.SetTrigger("OptionBack");
        if (restoreStartLayoutRoutine != null)
            StopCoroutine(restoreStartLayoutRoutine);
        restoreStartLayoutRoutine = StartCoroutine(RestoreStartScreenLayoutAfterSettings());
    }

    private IEnumerator RestoreStartScreenLayoutAfterSettings()
    {
        // OptionBack contains legacy RectTransform keys for the old 100x100
        // Control layout. Reapplying the layout only once is racy: on some
        // devices the animator writes those keys after the old 0.45 second
        // delay. Hold the corrected layout through the full transition and
        // one settled frame so Settings can never collapse the command
        // controls or hide Start Mission.
        for (var i = 0; i < 24; i++)
        {
            RestoreStartScreenLayout();
            yield return new WaitForSecondsRealtime(0.05f);
        }

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

    private void RestoreSettingsInteractivity()
    {
        if (PanelStart == null)
            return;

        var optionPanel = PanelStart.transform.Find("OptionPanel");
        if (optionPanel == null)
            return;

        var panelImage = optionPanel.GetComponent<Image>();
        if (panelImage != null)
            panelImage.raycastTarget = false;

        foreach (Transform child in optionPanel)
        {
            var button = child.GetComponent<UnityEngine.UI.Button>();
            if (button == null)
                continue;

            button.interactable = true;
            var rowImage = child.GetComponent<Image>();
            if (rowImage != null)
                rowImage.raycastTarget = true;

            foreach (var image in child.GetComponentsInChildren<Image>(true))
            {
                if (image.transform != child)
                    image.raycastTarget = false;
            }
        }
    }

    private bool ShouldEnforceStartScreenLayout()
    {
        if (PanelStart == null || !PanelStart.activeInHierarchy)
            return false;
        if (PanelMenu != null && PanelMenu.activeInHierarchy)
            return false;

        var optionBack = PanelStart.transform.Find("BackOption");
        if (optionBack != null && optionBack.gameObject.activeInHierarchy)
            return false;

        var backStatic = PanelStart.transform.Find("BackStatic");
        if (backStatic != null && backStatic.gameObject.activeInHierarchy)
            return false;

        var backStaticAlt = PanelStart.transform.Find("BackStatic (1)");
        if (backStaticAlt != null && backStaticAlt.gameObject.activeInHierarchy)
            return false;

        var panelStatics = PanelStart.transform.Find("PanelStatics");
        return panelStatics == null || !panelStatics.gameObject.activeInHierarchy;
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
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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

            if (Outgame != null)
                Outgame.Stop();
            MusicdOn.SetActive(false);
            MusicOff.SetActive(true);
            PlayerPrefs.SetFloat("Music", 1);
            PlayerPrefs.Save();
        }
        else
        {
            if (Outgame != null)
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
        if (CameraFllow == null || CameraFixed == null)
            return;

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
