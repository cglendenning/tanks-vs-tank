using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Intro : MonoBehaviour {

    public GameObject PanelCount;
    public GameObject T1;
    public GameObject T2;
    public Animator anim;
    void Start()
    {
       
        if (PlayerPrefs.HasKey("intro") == false)
        {
            PlayerPrefs.SetFloat("intro", 0);
            PlayerPrefs.Save();
        }

        if (PlayerPrefs.GetFloat("intro") == 1)
        {
            gameObject.SetActive(false);
        }

        if (PlayerPrefs.GetFloat("intro") == 0)
        {
            PanelCount.SetActive(false);
            Time.timeScale = 0f;                  
        }
    }
   

    public void OKIntro()
    {
        if (T1.activeInHierarchy)
        {
            T1.SetActive(false);
            T2.SetActive(true);
        }
        else if(T2.activeInHierarchy)
        {
            PanelCount.SetActive(true);
            anim.SetTrigger("intro");
            transform.GetChild(1).gameObject.SetActive(false);
            StartCoroutine(Delayintro());
        }
        
    }

    IEnumerator Delayintro()
    {
        yield return new WaitForSeconds(1f);
        PlayerPrefs.SetFloat("intro", 1);
        PlayerPrefs.Save();
        gameObject.SetActive(false);

    }
	
}
