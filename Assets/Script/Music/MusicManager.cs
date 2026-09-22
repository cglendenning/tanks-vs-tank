using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour {

    public static MusicManager instance;

    void Awake()
    {
        instance = this;
    }
    void Start()
    {
        if (PlayerPrefs.GetFloat("Sound") == 0)
        {
            issound = false;
        }
        else
        {
            issound = true;
        }

        if (PlayerPrefs.GetFloat("Music") == 0)
        {
            ismusic = false;
        }
        else
        {
            ismusic = true;
        }
    }
   public bool issound;
   public bool ismusic;

    public void GunTank()
    {
        if (issound == false)
        {
            transform.GetChild(0).gameObject.GetComponent<AudioSource>().Play();
        }
    }

    public void ExplosionTank()
    {
        if (issound == false)
        {
            transform.GetChild(1).gameObject.GetComponent<AudioSource>().Play();
        }
    }

    public void InGameTank()
    {
        if (ismusic == false)
        {
            transform.GetChild(2).gameObject.GetComponent<AudioSource>().Play();
        }
    }

    public void OutTank()
    {
        if (issound == false)
        {
            transform.GetChild(3).gameObject.GetComponent<AudioSource>().Play();
        }
    }

    public void CountDown()
    {
        if (issound == false)
        {
            transform.GetChild(4).gameObject.GetComponent<AudioSource>().Play();
        }
    }

    public void Victory()
    {
        if (issound == false)
        {
            transform.GetChild(2).gameObject.GetComponent<AudioSource>().Stop();
            transform.GetChild(5).gameObject.GetComponent<AudioSource>().Play();
        }
    }

    public void Three()
    {
        if (issound == false)
        {
            transform.GetChild(6).gameObject.GetComponent<AudioSource>().Play();
        }
    }

    public void Two()
    {
        if (issound == false)
        {
            transform.GetChild(7).gameObject.GetComponent<AudioSource>().Play();
        }
    }

    public void One()
    {
        if (issound == false)
        {
            transform.GetChild(8).gameObject.GetComponent<AudioSource>().Play();
        }
    }
}
