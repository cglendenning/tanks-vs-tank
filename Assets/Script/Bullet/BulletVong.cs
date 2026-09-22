using UnityEngine;
using System.Collections;

public class BulletVong : MonoBehaviour {

    public GameObject PrefabExplosionBullet;
    public GameObject PrefabExplosionTank;

    void OnTriggerEnter(Collider other)
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
            
            PlayerPrefs.SetFloat("CountKill", (PlayerPrefs.GetFloat("CountKill") + 1));
            PlayerPrefs.Save();

            ManagerScore.instance.Rung();
            MusicManager.instance.ExplosionTank();

            ManagerScore.instance.CurDieEmnemy += 1;
            ManagerScore.instance.Victory();
            ManagerScore.instance.Rung();
            MusicManager.instance.ExplosionTank();

            Destroy(other.gameObject);
            Instantiate(PrefabExplosionTank, other.gameObject.transform.position, Quaternion.Euler(-90, 0, 0));
            Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
            Destroy(gameObject);
        }


        if (other.gameObject.tag == "targetMask")
        {
            PlayerPrefs.SetFloat("CountKill", (PlayerPrefs.GetFloat("CountKill") + 1));
            PlayerPrefs.Save();

            ManagerScore.instance.Rung();
            MusicManager.instance.ExplosionTank();

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
