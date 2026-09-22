using UnityEngine;
using System.Collections;


public class BulletFind : MonoBehaviour {

    public GameObject Target;
    UnityEngine.AI.NavMeshAgent agent;
	// Use this for initialization
	void Start () {
        if (gameObject.tag == "findplayer")
        {
            Target = GameObject.FindWithTag("player");
        }
      
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
	}

    float timedelay = 6f;
   public float timmecount;
   bool notagent = true;

	// Update is called once per frame
	void Update () {
       
            timmecount += Time.deltaTime;
            if (timmecount > timedelay)
            {
                notagent = false;
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                Destroy(gameObject);
            }


            if (Target != null && notagent)
            {
                iscatched = true;
                agent.SetDestination(Target.transform.position);
            }
            if (gameObject.tag == "findemnemy")
            {
                if (Target == null && iscatched)
                {
                    Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                    Destroy(gameObject);
                }
            }
           

        
	}

    bool iscatched = false;
    public GameObject PrefabExplosionBullet;
    public GameObject PrefabExplosionTank;

    bool completed = true;
    void OnTriggerEnter(Collider other)
    {
        if (gameObject.tag == "findplayer")
        {
            if (other.gameObject.tag == "thanh")
            {
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                Destroy(gameObject);
            }

            if (other.gameObject.tag == "player")
            {
                PlayerMoverment.instance.DeadPlayer(1);
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                Destroy(gameObject);
            }     

        }


        if (gameObject.tag == "findemnemy" )
        {
            

            if (other.gameObject.tag == "thanh")
            {
                Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
                Destroy(gameObject);
            }

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

            if (other.gameObject.tag == "targetMask" && completed)
            {
                completed = false;

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
         
    }


}
