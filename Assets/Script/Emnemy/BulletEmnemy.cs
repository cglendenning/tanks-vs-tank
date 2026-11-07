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
   // D-019-AC-02: the tank's field of view calls BulletPlayer every 0.2 s while it sees this enemy; the enemy remembers
   // when, so it can fire again the moment it reloads instead of waiting for the next look.
   float lastSeen = float.NegativeInfinity;
   const float SeenFor = 0.25f;
   public void BulletPlayer()
    {
        lastSeen = Time.time;
        // D-035-AC-03: enemies hold fire during a first-time player's first play of mission 1.
        if (TreadShredFirstMinute.EnemiesHoldFire)
            return;
        if (canbullet && gameObject.GetComponent<BoxCollider>().enabled)
        {
            if (gameObject.tag != "emnemyred")
            {
                canbullet = false;
                TreadShredFirstMinute.EnemyFired();
                GameObject obj = Instantiate(Bullet, pointbullet.position, pointbullet.rotation) as GameObject;
                Vector3 dir = tower.position - pointbullet.position;

                if (gameObject.tag != "emnemyorange" && gameObject.tag != "emnemyblack")
                {
                    obj.GetComponent<Rigidbody>().linearVelocity = -dir.normalized * 15f;
                }

                if (gameObject.tag == "emnemyblack")
                {
                    obj.GetComponent<Rigidbody>().linearVelocity = -dir.normalized * 5f;
                }  

                // D-019-AC-02: tougher tiers fire more often: the follow-up shell and the reload both come sooner.
                var fireRate = TreadShredProgress.Ensure().TierModifiers.EnemyFireRate;
                StartCoroutine(BulletPlayerLan2(1f / fireRate));
                StartCoroutine(DelayBulletPlayer(3f / fireRate));
            }           
           
        }
       
    }
   bool canbullet = true;
   IEnumerator DelayBulletPlayer(float delay)
   {     
       yield return new WaitForSeconds(delay);
       canbullet = true;
       if (Time.time - lastSeen <= SeenFor)
           BulletPlayer();
   }

   IEnumerator BulletPlayerLan2(float delay)
   {
       yield return new WaitForSeconds(delay);
       // D-035-AC-03: no follow-up shell on a first play.
       if (TreadShredFirstMinute.FirstPlay)
           yield break;
       GameObject obj = Instantiate(Bullet, pointbullet.position, pointbullet.rotation) as GameObject;

       Vector3 dir = tower.position - pointbullet.position;
       if (gameObject.tag == "emnemyblack")
       {
           obj.GetComponent<Rigidbody>().linearVelocity = -dir.normalized * 5f;
       }  
       if (gameObject.tag != "emnemyorange" && gameObject.tag != "emnemyblack")
       {
           obj.GetComponent<Rigidbody>().linearVelocity = -dir.normalized * 15f;
       }    

   }

   //void OnDestroy()
   //{
   //    ManagerScore.instance.CurDieEmnemy += 1;
   //    ManagerScore.instance.Victory();
   //}

}
