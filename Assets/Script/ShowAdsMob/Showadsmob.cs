using UnityEngine;
using System.Collections;

public class Showadsmob : MonoBehaviour {

    public static Showadsmob instance;
    void Awake()
    {
        DontDestroyOnLoad(transform.gameObject);
    }

	// Use this for initialization
	void Start () {
        instance = this;
	}
    public float timecount;
    float timedelay = 50f;
    public bool isadsmob;
	// Update is called once per frame
	void Update () {
        timecount += Time.deltaTime;

        if (timecount > timedelay)
        {
            isadsmob = true;
                
        }
	}

}
