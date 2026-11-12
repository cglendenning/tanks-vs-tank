using UnityEngine;
using System.Collections;
using TreadShred.Progression;

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
            // D-046-AC-04: on Veteran and Elite every attack is a volley that cycles through every weapon.
            var combat = TierCombat.For(TreadShredProgress.Ensure().SelectedTier);
            if (combat.VolleyShots > 0)
            {
                canbullet = false;
                TreadShredFirstMinute.EnemyFired();
                StartCoroutine(Volley(combat));
                StartCoroutine(DelayBulletPlayer(3f / TreadShredProgress.Ensure().TierModifiers.EnemyFireRate));
                return;
            }
            if (gameObject.tag != "emnemyred")
            {
                canbullet = false;
                TreadShredFirstMinute.EnemyFired();
                GameObject obj = Instantiate(Bullet, pointbullet.position, pointbullet.rotation) as GameObject;
                TreadShredEnemyShells.Fired(obj);
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
   // D-046-AC-04: the shot this enemy fires next, so its volleys keep cycling through the weapons.
   int volleyShot;
   static GameObject cannonShell, arcShell, seekerShell;

   IEnumerator Volley(TierCombat combat)
   {
       for (var shot = 0; shot < combat.VolleyShots; shot++)
       {
           if (shot > 0)
               yield return new WaitForSeconds(TierCombat.VolleyGap);
           if (!enabled || player == null)
               yield break;
           Fire(TierCombat.VolleyWeapon(volleyShot++), combat);
       }
   }

   void Fire(EnemyWeapon weapon, TierCombat combat)
   {
       // D-046-AC-02: shells fly faster on Veteran and Elite.
       var speed = 15f * combat.ShellSpeed;
       var aim = Aim(combat, speed);
       switch (weapon)
       {
           case EnemyWeapon.TripleShot:
               foreach (var angle in new[] { -TierCombat.TripleSpreadDegrees, 0f, TierCombat.TripleSpreadDegrees })
                   Launch(Shell(ref cannonShell, "BulletEmnemy"), Quaternion.AngleAxis(angle, Vector3.up) * aim * speed, straight: true);
               break;
           case EnemyWeapon.ArcShot:
               Lob(Shell(ref arcShell, "BulletRed"), combat);
               break;
           case EnemyWeapon.Seeker when OnNavMesh():
               var seeker = Launch(Shell(ref seekerShell, "BulletFindPlayer"), aim * 5f * combat.ShellSpeed);
               var agent = seeker != null ? seeker.GetComponent<UnityEngine.AI.NavMeshAgent>() : null;
               if (agent != null)
                   agent.speed *= combat.ShellSpeed;
               break;
           default:
               // The cannon, and the seeker on a map without a navigation mesh to steer it.
               Launch(Shell(ref cannonShell, "BulletEmnemy"), aim * speed, straight: true);
               break;
       }
   }

   Vector3 Barrel => (pointbullet.position - tower.position).normalized;

   // D-046-AC-03/04: the line to the middle of the tank's hit box, where it is, or on Elite where it will be when the
   // shell arrives. Not the barrel's level line: a tall turret's (the red tanks') passes over the tank (mission 52).
   Vector3 Aim(TierCombat combat, float speed)
   {
       var from = pointbullet.position;
       var hitBox = HitBox();
       var target = hitBox.center;
       if (combat.LeadsShots && PlayerMoverment.instance != null)
       {
           var velocity = PlayerMoverment.instance.Velocity;
           var ahead = TierCombat.Lead(from.x, from.z, target.x, target.z, velocity.x, velocity.z, speed);
           target = new Vector3(ahead.x, target.y, ahead.z);
       }
       var line = target - from;
       return line.sqrMagnitude < 0.0001f ? Barrel : line.normalized;
   }

   // The tank's hit box: the trigger its shells strike (Bullet), or a tank-sized box at its position without one.
   Bounds HitBox()
   {
       foreach (var collider in player.GetComponentsInChildren<Collider>())
           if (collider.isTrigger && collider.enabled)
               return collider.bounds;
       return new Bounds(player.transform.position + Vector3.up * 0.9f, new Vector3(1.5f, 1.8f, 1.5f));
   }

   // D-046-AC-04: the arc shot lobs over walls and comes down where the tank is (or, on Elite, will be).
   void Lob(GameObject prefab, TierCombat combat)
   {
       if (player == null)
           return;
       var target = player.transform.position;
       if (combat.LeadsShots && PlayerMoverment.instance != null)
           target += PlayerMoverment.instance.Velocity * 1.1f;
       var from = pointbullet.position;
       var flat = new Vector3(target.x - from.x, 0f, target.z - from.z);
       var time = Mathf.Clamp(flat.magnitude / (12f * combat.ShellSpeed), 0.8f, 1.6f);
       var velocity = flat / time + Vector3.up * ((target.y - from.y) / time + 0.5f * -Physics.gravity.y * time);
       var shell = Launch(prefab, velocity);
       if (shell == null)
           return;
       var body = shell.GetComponent<Rigidbody>();
       if (body != null)
           body.useGravity = true;
       // A lobbed shell that lands without a hit does not lie on the ground.
       Destroy(shell, 4f);
   }

   // Shells keep the height they leave the muzzle at (locked on the prefab); a straight one aimed down at the tank is
   // let descend, and is gone when it reaches the ground past a tank that moved, or after 4 s.
   GameObject Launch(GameObject prefab, Vector3 velocity, bool straight = false)
   {
       if (prefab == null)
           return null;
       var rotation = Quaternion.FromToRotation(Barrel, velocity.normalized) * pointbullet.rotation;
       var shell = Instantiate(prefab, pointbullet.position, rotation);
       TreadShredEnemyShells.Fired(shell);
       var body = shell.GetComponent<Rigidbody>();
       if (body != null)
       {
           if (straight)
               body.constraints &= ~RigidbodyConstraints.FreezePositionY;
           body.linearVelocity = velocity;
       }
       if (straight)
       {
           // Descending, it crosses other enemy tanks' height: it passes through them instead of striking or shoving them.
           var shellCollider = shell.GetComponent<Collider>();
           if (shellCollider != null)
               foreach (var enemy in FindObjectsByType<BulletEmnemy>(FindObjectsSortMode.None))
                   foreach (var enemyCollider in enemy.GetComponentsInChildren<Collider>())
                       Physics.IgnoreCollision(shellCollider, enemyCollider);
           var drop = pointbullet.position.y - HitBox().min.y - ShellClearance;
           Destroy(shell, velocity.y < -0.01f ? Mathf.Min(drop / -velocity.y, StraightLifetime) : StraightLifetime);
       }
       return shell;
   }

   const float ShellClearance = 0.3f;
   const float StraightLifetime = 4f;

   bool OnNavMesh() => UnityEngine.AI.NavMesh.SamplePosition(pointbullet.position, out _, 2f, UnityEngine.AI.NavMesh.AllAreas);

   static GameObject Shell(ref GameObject cached, string name)
   {
       if (cached == null)
       {
           cached = Resources.Load<GameObject>("EnemyShells/" + name);
           if (cached == null)
               Debug.LogError("[Volley] Resources/EnemyShells/" + name + " is missing; that shot is skipped");
       }
       return cached;
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
       TreadShredEnemyShells.Fired(obj);

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
