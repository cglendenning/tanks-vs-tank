using UnityEngine;
using System.Collections;

public class ThrowSimulation : MonoBehaviour
{
    //Mục tiêu bắn
    public Transform Target;
    public float firingAngle = 45.0f;
    public float gravity = 9.8f;

    //Viên đạn
    public Transform Projectile;
    //Điểm bắn
    public Transform myTransform;

   
    void Start()
    {
       
    }

   public float timecount;
   public float timedelay;


    void Update()
    {
        
        if (gameObject.tag != "player")
        {
            if (Target)
            {
                if (gameObject.GetComponent<BoxCollider>().enabled)
                {
                    if (Target.GetComponent<BoxCollider>().enabled)
                    {
                        timecount += Time.deltaTime;
                        if (timecount > timedelay)
                        {
                            timecount = 0f;
                            StartCoroutine(SimulateProjectile2());
                        }
                    }
                }
            }
           
        }

        
    }

  
  
   public void Call()
    {
        
            StartCoroutine(SimulateProjectile());
       
    }
   
   public void CallFromPlayer(Vector3 target)
   {

       Target.position = target;

       StartCoroutine(SimulateProjectile2());

   }



    public GameObject PrefabExplosionBullet;

    IEnumerator SimulateProjectile()
    {
      
        Projectile.gameObject.SetActive(true);
        // Short delay added before Projectile is thrown
        //Projectile.SetParent(null);
        //Projectile.SetParent(null);
        //-0.092
       
        // Move projectile to the position of throwing object + add some offset if needed.
        Projectile.position = myTransform.position;
       
        // Calculate distance to target
        float target_Distance = Vector3.Distance(Projectile.position, Target.position);

        // Calculate the velocity needed to throw the object to the target at specified angle.
        float projectile_Velocity = target_Distance / (Mathf.Sin(2 * firingAngle * Mathf.Deg2Rad) / gravity);

        // Extract the X  Y componenent of the velocity
        float Vx = Mathf.Sqrt(projectile_Velocity) * Mathf.Cos(firingAngle * Mathf.Deg2Rad);
        float Vy = Mathf.Sqrt(projectile_Velocity) * Mathf.Sin(firingAngle * Mathf.Deg2Rad);

        // Calculate flight time.
        float flightDuration = target_Distance / Vx;

        // Rotate projectile to face the target.
        Projectile.rotation = Quaternion.LookRotation(Target.position - Projectile.position);

        float elapse_time = 0;
    
      //  Projectile.gameObject.SetActive(true);
        while (elapse_time < flightDuration)
        {
            Projectile.Translate(0,  (Vy - (gravity * elapse_time)) * Time.deltaTime, Vx * Time.deltaTime);

            elapse_time +=  Time.deltaTime;

            yield return null;
        }

        Projectile.gameObject.SetActive(false);

        Instantiate(PrefabExplosionBullet, Projectile.gameObject.transform.position + new Vector3(0f,2f,0f), Quaternion.Euler(-90, 0, 0));
    }
   
    // Bắn đạn Destroy và Sinh ra

    public GameObject Bullet;
    IEnumerator SimulateProjectile2()
    {

        GameObject BulletDestroy = Instantiate(Bullet, transform.position, Quaternion.identity) as GameObject;
    
        BulletDestroy.gameObject.SetActive(true);
        // Short delay added before Projectile is thrown
        //Projectile.SetParent(null);
        //Projectile.SetParent(null);
        //-0.092

        // Move projectile to the position of throwing object + add some offset if needed.
        BulletDestroy.transform.position = myTransform.position;

        // Calculate distance to target
        float target_Distance = Vector3.Distance(BulletDestroy.transform.position, Target.position);

        // Calculate the velocity needed to throw the object to the target at specified angle.
        float projectile_Velocity = target_Distance / (Mathf.Sin(2 * firingAngle * Mathf.Deg2Rad) / gravity);

        // Extract the X  Y componenent of the velocity
        float Vx = Mathf.Sqrt(projectile_Velocity) * Mathf.Cos(firingAngle * Mathf.Deg2Rad);
        float Vy = Mathf.Sqrt(projectile_Velocity) * Mathf.Sin(firingAngle * Mathf.Deg2Rad);

        // Calculate flight time.
        float flightDuration = target_Distance / Vx;

        // Rotate projectile to face the target.
        BulletDestroy.transform.rotation = Quaternion.LookRotation(Target.position - BulletDestroy.transform.position);

        float elapse_time = 0;

        //  Projectile.gameObject.SetActive(true);
        while (elapse_time < flightDuration && BulletDestroy != null)
        {
            BulletDestroy.transform.Translate(0, (Vy - (gravity * elapse_time)) * Time.deltaTime, Vx * Time.deltaTime);

            elapse_time += Time.deltaTime;
            yield return null;
        }

        if (BulletDestroy != null || gameObject.GetComponent<BoxCollider>().enabled == false)
        {
            Destroy(BulletDestroy);
            MusicManager.instance.ExplosionTank();
            Instantiate(PrefabExplosionBullet, BulletDestroy.transform.position + new Vector3(0f, 2f, 0f), Quaternion.Euler(-90, 0, 0));          
        }
       
    }
   

}