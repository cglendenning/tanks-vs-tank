using UnityEngine;
using System.Collections;

public class BulletRed : MonoBehaviour {

    private bool hitPlayer;

    // D-017-AC-09: the lob shell that clears walls takes one hit of armor, like every enemy shell, and only once.
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "player" && !hitPlayer)
        {
            hitPlayer = true;
            PlayerMoverment.instance.DeadPlayer(1);
        }

    }
}
