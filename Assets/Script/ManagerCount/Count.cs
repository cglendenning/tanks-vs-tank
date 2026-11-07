using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Count : MonoBehaviour {

    public Text Level;
    private static Count current;
    private bool played;
	// Use this for initialization
	void Awake () {

        if (PlayerPrefs.HasKey("intro") == false)
        {
            PlayerPrefs.SetFloat("intro", 0);
            PlayerPrefs.Save();
        }

            // D-019-AC-03: the campaign number and its tier.
            Level.text = TreadShred.Progression.Campaign.HudLabel(TreadShredProgress.Ensure().CurrentNumber());
            Time.timeScale = 0;
            current = this;
       
	}

    /// <summary>
    /// D-020-AC-06: the 3-2-1 again, from 3, before play resumes after a revive or a pause; Letstart resumes play.
    /// False when the scene has no countdown.
    /// </summary>
    public static bool Replay()
    {
        if (current == null)
            return false;
        Time.timeScale = 0;
        TreadShredWeaponToggle.SetVisible(false);
        current.gameObject.SetActive(true);
        var animator = current.GetComponent<Animator>();
        animator.enabled = true;
        animator.Rebind();
        animator.Update(0f);
        return true;
    }

    public void Letstart()
    {
        Time.timeScale = 1;
        // The mission's music starts with its first countdown; a replay leaves it playing.
        if (!played)
            MusicManager.instance.InGameTank();
        played = true;
        TreadShredWeaponToggle.SetVisible(true);
        gameObject.SetActive(false);
    }

    public void CountThree()
    {
        MusicManager.instance.Three();
    }

    public void CountTwo()
    {
        MusicManager.instance.Two();
    }

    public void CountOne()
    {
        MusicManager.instance.One();
    }
}
