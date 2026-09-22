using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FPS : MonoBehaviour {

    float deltaTime;
    public Text SHOW;
    // Use this for initialization
    void Start () {
    
	}
	
	// Update is called once per frame
	void Update () {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        fps = Mathf.Round(fps);
        SHOW.text = "FPS // " + fps.ToString();
    }
  

    public void ReLoadPlay()
    {
        int scene = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
        Time.timeScale = 1;
    }
}
