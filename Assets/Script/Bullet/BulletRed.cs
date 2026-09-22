using UnityEngine;
using System.Collections;

public class BulletRed : MonoBehaviour {

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "player")
        {
            PlayerMoverment.instance.DeadPlayer(3);
        }

    }
}
