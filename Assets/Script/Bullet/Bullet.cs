using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour {
   
    private bool m_rotate;
    Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
    
        transform.rotation = Quaternion.LookRotation(rb.linearVelocity);
       
    }
    bool bulletexplosion;
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "thanh")
        {
          //  Debug.Log("ggg");
            if (bulletexplosion)
            {
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                Destroy(gameObject);
            }
            else
            {
                bulletexplosion = true;
            }
          
        }

      
    }

    //void CheckColision(float distans)
    //{
    //    Ray ray = new Ray(transform.position,transform.forward);
    //    RaycastHit hit;
    //    if (Physics.Raycast(ray, out hit, distans, collisonmask,QueryTriggerInteraction.Collide))
    //    {
    //        OnHitObject(hit);
    //    }
    //}

    //void  OnHitObject(RaycastHit hit)
    //{

    //    if (hit.collider.gameObject.tag == "player")
    //    {
    //        PlayerMoverment.instance.DeadPlayer(1);
    //    }
    //    else
    //    {
    //        ManagerScore.instance.CurDieEmnemy += 1;
    //        ManagerScore.instance.Victory();
    //        Destroy(hit.collider.gameObject);
    //        Instantiate(PrefabExplosionTank, hit.collider.gameObject.transform.position, Quaternion.Euler(-90, 0, 0));
    //    }

    //   Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));


    //    GameObject.Destroy(gameObject);
    //}



    public GameObject PrefabExplosionTank;
    public GameObject PrefabExplosionBullet;

    void OnTriggerEnter(Collider other)
    {
        if (gameObject.tag == "bulletplayer")
        {
            if (other.gameObject.tag == "emnemyorange")
            {
                other.gameObject.GetComponent<EmnemyRed>().DeadPlayer(1);
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                Destroy(gameObject);
            }
            if (other.gameObject.tag == "emnemyblack")
            {
                other.gameObject.GetComponent<EmnemyRed>().DeadPlayer(1);
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                Destroy(gameObject);
            }
            if (other.gameObject.tag == "emnemyred")
            {
                other.gameObject.GetComponent<EmnemyRed>().DeadPlayer(1);
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                Destroy(gameObject);
            }

            if (other.gameObject.tag == "targetMask")
            {
                PlayerPrefs.SetFloat("CountKill", (PlayerPrefs.GetFloat("CountKill") + 1));
                PlayerPrefs.Save();

                ManagerScore.instance.CurDieEmnemy += 1;
                ManagerScore.instance.Victory();
                                                   
                MusicManager.instance.ExplosionTank();
                Destroy(other.gameObject);
                Instantiate(PrefabExplosionTank, other.gameObject.transform.position, Quaternion.Euler(-90, 0, 0));
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                ManagerScore.instance.Rung();
                Destroy(gameObject);
            }

        }

        if (gameObject.tag == "bulletemnemy")
        {
             if (other.gameObject.tag == "player")
            {
                PlayerMoverment.instance.DeadPlayer(1);
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                Destroy(gameObject);
            }

        }
      

    }


}
