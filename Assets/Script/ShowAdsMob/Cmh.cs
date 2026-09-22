using UnityEngine;
using System.Collections;

public class Cmh : MonoBehaviour {

    public string folder = "ScreenshotFolder";
    public int frameRate = 1;
    public bool snap;
    void Start()
    {
        // Set the playback framerate (real time will not relate to game time after this).
       // Time.captureFramerate = frameRate;

        // Create the folder
        System.IO.Directory.CreateDirectory(folder);
    }

    float timecheck;
    void Update()
    {
        if (snap)
        {
            timecheck += Time.deltaTime;
            if (timecheck>=2)
            {
                timecheck = 0;
                // Append filename to folder name (format is '0005 shot.png"')
                string name = string.Format("{0}/{1:D04} shot.png", folder, Time.frameCount);
                // Capture the screenshot to the specified file.
                ScreenCapture.CaptureScreenshot(name);
            }
           
        }
      
    }
}
