using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Count : MonoBehaviour {

    public Text Level;
	// Use this for initialization
	void Awake () {

        if (PlayerPrefs.HasKey("intro") == false)
        {
            PlayerPrefs.SetFloat("intro", 0);
            PlayerPrefs.Save();
        }

            string level = Application.loadedLevelName;
            level = (int.Parse(level.Substring(2))).ToString();
            Level.text = "LEVEL: " + level;
            Time.timeScale = 0;
        
       
	}

    public void Letstart()
    {
        Time.timeScale = 1;
        MusicManager.instance.InGameTank();
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
