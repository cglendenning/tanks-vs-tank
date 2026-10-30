using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TreadShred.Progression;

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

        // Upgrade shop entry (D-017) and tier choice on mission select (D-019).
        var font = TreadShredUi.FindFont(PanelStart);
        if (PanelStart != null)
        {
            var upgrades = TreadShredUi.CreateButton("UPGRADES", (RectTransform)PanelStart.transform, Vector2.zero,
                new Vector2(320f, 96f), 34, font, Upgrades);
            var rect = (RectTransform)upgrades.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.86f, 0.50f);

            // Platform leaderboard (D-032).
            if (TreadShredLeaderboard.Available)
            {
                var leaderboard = TreadShredUi.CreateButton("LEADERBOARD", (RectTransform)PanelStart.transform, Vector2.zero,
                    new Vector2(360f, 96f), 34, font, OpenLeaderboard);
                var boardRect = (RectTransform)leaderboard.transform;
                boardRect.anchorMin = boardRect.anchorMax = new Vector2(0.86f, 0.64f);
            }
        }
        TreadShredTierSelector.Attach(PanelMenu, font);
    }

    // D-032: the leaderboard, Apple's sign-in, or a note on where to sign in.
    public void OpenLeaderboard()
    {
        if (TreadShredLeaderboard.Open() != LeaderboardOpenResult.SignInFromSettings || PanelStart == null)
            return;
        var note = TreadShredUi.CreateText(LeaderboardRules.SignInFromSettings, (RectTransform)PanelStart.transform,
            Vector2.zero, new Vector2(760f, 40f), 24, TreadShredUi.FindFont(PanelStart));
        note.color = TreadShredUi.TitleColor;
        var rect = note.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.80f, 0.74f);
        Destroy(note.gameObject, 4f);
    }

    public void Upgrades()
    {
        TreadShredUpgradePanel.Show(TreadShredUi.FindFont(PanelStart));
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

    // Opens the full-screen settings panel; it replaces the legacy animated
    // OptionPanel, which overlapped the start screen and had no clear exit.
    public void Option()
    {
        if (settingsPanel == null)
            settingsPanel = TreadShredSettingsPanel.Show(this, TreadShredUi.FindFont(PanelStart));
    }

    TreadShredSettingsPanel settingsPanel;

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

        int TotalHighScore = TreadShredScores.TotalScore();

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

    // Saved settings use 0 for on (and 0 for the follow camera); the legacy
    // on/off icons are kept in step so either panel shows the same state.
    public bool SoundEnabled { get { return PlayerPrefs.GetFloat("Sound") == 0; } }
    public bool MusicEnabled { get { return PlayerPrefs.GetFloat("Music") == 0; } }
    public bool VibrationEnabled { get { return PlayerPrefs.GetFloat("Vibrate") == 0; } }
    public bool CameraFollows { get { return PlayerPrefs.GetFloat("Camera") == 0; } }

    public GameObject SoundOn;
    public GameObject SoundOff;
    public void SoundGame()
    {
        SetSoundEnabled(!SoundEnabled);
    }

    public void SetSoundEnabled(bool enabled)
    {
        SetLegacyIcons(SoundOn, SoundOff, enabled);
        SavePreference("Sound", enabled);
    }

    public GameObject MusicdOn;
    public GameObject MusicOff;
    public void MusicGame()
    {
        SetMusicEnabled(!MusicEnabled);
    }

    public void SetMusicEnabled(bool enabled)
    {
        if (Outgame != null)
        {
            if (enabled)
                Outgame.Play();
            else
                Outgame.Stop();
        }
        SetLegacyIcons(MusicdOn, MusicOff, enabled);
        SavePreference("Music", enabled);
    }

    public GameObject ViBrateOn;
    public GameObject ViBrateOff;
    public void ViBrateGame()
    {
        SetVibrationEnabled(!VibrationEnabled);
    }

    public void SetVibrationEnabled(bool enabled)
    {
        SetLegacyIcons(ViBrateOn, ViBrateOff, enabled);
        SavePreference("Vibrate", enabled);
    }

    public GameObject CameraFllow;
    public GameObject CameraFixed;
    public void CameraGame()
    {
        SetCameraFollows(!CameraFollows);
    }

    public void SetCameraFollows(bool follows)
    {
        SetLegacyIcons(CameraFllow, CameraFixed, follows);
        SavePreference("Camera", follows);
    }

    private static void SetLegacyIcons(GameObject onIcon, GameObject offIcon, bool on)
    {
        if (onIcon != null)
            onIcon.SetActive(on);
        if (offIcon != null)
            offIcon.SetActive(!on);
    }

    private static void SavePreference(string key, bool on)
    {
        PlayerPrefs.SetFloat(key, on ? 0 : 1);
        PlayerPrefs.Save();
    }

    public void ExitGame()
    {
         Application.Quit(); 
    }
}
