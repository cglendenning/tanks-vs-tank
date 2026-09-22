using UnityEngine;
using System.Collections;

public class Camerafllow : MonoBehaviour {

   public Transform target;
    public float zdistance;
    public float ydistance;

    public float speed;
    public float speedrotate;

    bool isfllow;
    void Start()
    {
        target = GameObject.Find("Player").transform;

        if (PlayerPrefs.GetFloat("Camera") == 1)
        {
            isfllow = true;
        }
        else
        {
            isfllow = false;
        }
       
    }
	// Update is called once per frame
	void LateUpdate () {

        if (target != null && isfllow == false)
        {
            if (target.position.z > 14.2f && target.position.z < 17.53f )
            {
                transform.position = Vector3.Lerp (transform.position, new Vector3(transform.position.x, transform.position.y, target.transform.position.z -1f),speed*Time.deltaTime);
               
            }
            else if (target.position.z < 14.2f)
            {
                transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, transform.position.y, -1.1f), speed * Time.deltaTime);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, transform.position.y, 1.2f), speed * Time.deltaTime);
            }



            if (target.position.x > -4.2f && target.position.x < 14.6f)
            {
                transform.position = Vector3.Lerp(transform.position, new Vector3(target.transform.position.x, transform.position.y, transform.position.z), speed * Time.deltaTime);
              //  transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(73f, 0f, 0f), Time.deltaTime * speedrotate);

            }
            else if (target.position.x < -4.2f)
            {
                transform.position = Vector3.Lerp(transform.position, new Vector3(-2.2f, transform.position.y, transform.position.z), speed * Time.deltaTime);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(73f, 1f, 0f), Time.deltaTime * speedrotate);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, new Vector3(13.5f, transform.position.y, transform.position.z), speed * Time.deltaTime);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(73f, -1f, 0f), Time.deltaTime * speedrotate);
            }

            transform.position = new Vector3(transform.position.x, transform.position.y, Mathf.Clamp(transform.position.z, -3.4f, 4.9f));
        }
           
	}
}
