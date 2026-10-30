using UnityEngine;
using System.Collections;

public class EmnemyRed : MonoBehaviour {
   public int countdie;
    // Health is kept in tenths of a hit so tier health (D-019) and firepower
    // upgrades (D-017) can scale it while countdie stays an integer.
    const int HitScale = 10;
    void Start()
    {
        var baseHits = gameObject.tag == "emnemyred" ? 5 : 4;
        countdie = Mathf.RoundToInt(baseHits * HitScale * TreadShredProgress.Ensure().TierModifiers.EnemyHealth);
    }
    public GameObject ExTank;
    bool completed = true;
    public void DeadPlayer(int index)
    {
        countdie -= Mathf.Max(1, Mathf.RoundToInt(index * HitScale * TreadShredProgress.Ensure().DamageMultiplier));
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
