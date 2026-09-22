using UnityEngine;
using System.Collections;

public class Bullet3 : MonoBehaviour {
   
    Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Fire();
    }
	
    void Fire()
    {
        rb.velocity = transform.up * 10f;
    }


    void OnCollisionEnter(Collision collision)
    {
        if (gameObject.tag == "bullet3" || gameObject.tag == "bulletorgane")
        {
            if (collision.gameObject.tag == "thanh")
            {
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                Destroy(gameObject);
            }
        }
    }
   
   
   
    void OnTriggerEnter(Collider other)
    {
        if (gameObject.tag == "bullet3")
        {
            if (other.gameObject.tag == "emnemyred")
            {
                other.gameObject.GetComponent<EmnemyRed>().DeadPlayer(1);
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                Destroy(gameObject);
            }

            if (other.gameObject.tag == "emnemyorange" || other.gameObject.tag == "emnemyblack" || other.gameObject.tag == "emnemyred")
            {
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                other.gameObject.GetComponent<EmnemyRed>().DeadPlayer(1);
                Destroy(gameObject);
            }

            if (other.gameObject.tag == "targetMask")
            {
                PlayerPrefs.SetFloat("CountKill", (PlayerPrefs.GetFloat("CountKill") + 1));
                PlayerPrefs.Save();

                ManagerScore.instance.CurDieEmnemy += 1;
                ManagerScore.instance.Victory();
                ManagerScore.instance.Rung();
                MusicManager.instance.ExplosionTank();
                Destroy(other.gameObject);
                Instantiate(PrefabExplosionTank, other.gameObject.transform.position, Quaternion.Euler(-90, 0, 0));
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));               
                Destroy(gameObject);
            }


        }


        if (gameObject.tag == "bulletorgane")
        {
            if (other.gameObject.tag == "player")
            {
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                PlayerMoverment.instance.DeadPlayer(1);
                Destroy(gameObject);
            }



            
        }


    }

    public GameObject PrefabExplosionBullet;
    public GameObject PrefabExplosionTank;

  

}
