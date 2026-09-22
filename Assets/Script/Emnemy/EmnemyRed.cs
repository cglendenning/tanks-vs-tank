using UnityEngine;
using System.Collections;

public class EmnemyRed : MonoBehaviour {
   public int countdie;
    void Start()
    {
        if (gameObject.tag == "emnemyred")
        {
            countdie = 5;
        }
        else
        {
            countdie = 4;
        }
    }
    public GameObject ExTank;
    bool completed = true;
    public void DeadPlayer(int index)
    {
        countdie -= index;
        if (countdie < 1 && completed)
        {
            ManagerScore.instance.Rung();
            MusicManager.instance.ExplosionTank();
            completed = false;
            ManagerScore.instance.CurDieEmnemy += 1;
            ManagerScore.instance.Victory();
            gameObject.layer = 0;
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(false);
            gameObject.GetComponent<BoxCollider>().enabled = false;
            gameObject.GetComponent<BoxCollider>().enabled = false;
            gameObject.GetComponent<BulletEmnemy>().enabled = false;
            Destroy(gameObject,3f);
            Instantiate(ExTank, transform.position, Quaternion.Euler(-90, 0, 0));
        }
    }
}
