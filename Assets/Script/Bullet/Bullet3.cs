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
        rb.linearVelocity = transform.up * 10f;
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
            else if (other.gameObject.tag == "emnemyorange" || other.gameObject.tag == "emnemyblack")
            {
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                other.gameObject.GetComponent<EmnemyRed>().DeadPlayer(1);
                Destroy(gameObject);
            }

            // D-046-AC-06: on Veteran and Elite a target takes the tier's health; a hit it survives only bursts the shell.
            if (other.gameObject.tag == "targetMask" && TreadShredTargetHealth.Survives(other.gameObject))
            {
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                Destroy(gameObject);
                return;
            }

            if (other.gameObject.tag == "targetMask" && !ManagerScore.instance.ClaimKill(other.gameObject))
            {
                // D-032-AC-01: already counted (a second collider or Overdrive round in the same frame).
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                Destroy(gameObject);
            }
            else if (other.gameObject.tag == "targetMask")
            {
                PlayerPrefs.SetFloat("CountKill", (PlayerPrefs.GetFloat("CountKill") + 1));
                PlayerPrefs.Save();

                ManagerScore.instance.CurDieEmnemy += 1;
                TreadShredImpact.Kill(other.transform.position);
                ManagerScore.instance.EnemyKilled();
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
