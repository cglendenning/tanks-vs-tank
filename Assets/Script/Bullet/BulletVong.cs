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

        if (other.gameObject.tag == "emnemyred" && ManagerScore.instance.ClaimKill(other.gameObject))
        {
            
            PlayerPrefs.SetFloat("CountKill", (PlayerPrefs.GetFloat("CountKill") + 1));
            PlayerPrefs.Save();

            ManagerScore.instance.Rung();
            MusicManager.instance.ExplosionTank();

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


        // D-046-AC-06: on Veteran and Elite a target takes the tier's health; a hit it survives only bursts the shell.
        if (other.gameObject.tag == "targetMask" && TreadShredTargetHealth.Survives(other.gameObject))
        {
            Instantiate(PrefabExplosionBullet, transform.position, Quaternion.Euler(-90, 0, 0));
            Destroy(gameObject);
            return;
        }

        if (other.gameObject.tag == "targetMask" && ManagerScore.instance.ClaimKill(other.gameObject))
        {
            PlayerPrefs.SetFloat("CountKill", (PlayerPrefs.GetFloat("CountKill") + 1));
            PlayerPrefs.Save();

            ManagerScore.instance.Rung();
            MusicManager.instance.ExplosionTank();

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

}
