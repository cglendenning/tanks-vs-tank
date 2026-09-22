using UnityEngine;
using System.Collections;

public class BulletEmnemy : MonoBehaviour {

    Transform tower;
    Transform pointbullet;
    GameObject player;

	// Use this for initialization
	void Start () {
        tower = transform.GetChild(0).gameObject.GetComponent<Transform>();
        pointbullet = transform.GetChild(0).gameObject.transform.GetChild(0).gameObject.transform;
        player = GameObject.FindGameObjectWithTag("player");
        StartCoroutine(TowerDelayRotate(.2f));
	}
	

    IEnumerator TowerDelayRotate(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            LookPlayer();
        }
    }

    void LookPlayer()
    {
        if (player != null)
        {
            Vector3 targetPostition = new Vector3(player.transform.position.x,
                                       this.transform.position.y,
                                       player.transform.position.z);

            tower.LookAt(targetPostition);
        }
        
    }

   public GameObject Bullet;
   public void BulletPlayer()
    {
        if (canbullet && gameObject.GetComponent<BoxCollider>().enabled)
        {
            if (gameObject.tag != "emnemyred")
            {
                canbullet = false;
                GameObject obj = Instantiate(Bullet, pointbullet.position, pointbullet.rotation) as GameObject;
                Vector3 dir = tower.position - pointbullet.position;

                if (gameObject.tag != "emnemyorange" && gameObject.tag != "emnemyblack")
                {
                    obj.GetComponent<Rigidbody>().velocity = -dir.normalized * 15f;
                }

                if (gameObject.tag == "emnemyblack")
                {
                    obj.GetComponent<Rigidbody>().velocity = -dir.normalized * 5f;
                }  

                StartCoroutine(BulletPlayerLan2(1f));
                StartCoroutine(DelayBulletPlayer(3f));
            }           
           
        }
       
    }
   bool canbullet = true;
   IEnumerator DelayBulletPlayer(float delay)
   {     
       yield return new WaitForSeconds(delay);
       canbullet = true;
   }

   IEnumerator BulletPlayerLan2(float delay)
   {
       yield return new WaitForSeconds(delay);
       GameObject obj = Instantiate(Bullet, pointbullet.position, pointbullet.rotation) as GameObject;

       Vector3 dir = tower.position - pointbullet.position;
       if (gameObject.tag == "emnemyblack")
       {
           obj.GetComponent<Rigidbody>().velocity = -dir.normalized * 5f;
       }  
       if (gameObject.tag != "emnemyorange" && gameObject.tag != "emnemyblack")
       {
           obj.GetComponent<Rigidbody>().velocity = -dir.normalized * 15f;
       }    

   }

   //void OnDestroy()
   //{
   //    ManagerScore.instance.CurDieEmnemy += 1;
   //    ManagerScore.instance.Victory();
   //}

}
