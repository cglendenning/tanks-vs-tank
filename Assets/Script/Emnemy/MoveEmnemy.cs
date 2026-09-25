using UnityEngine;
using System.Collections;

public class MoveEmnemy : MonoBehaviour {

    Rigidbody rb;
    UnityEngine.AI.NavMeshAgent agent;
    public enum DirectionRandom
    {
        tren,
        duoi,
        trai,
        phai,
        trenphai,
        trentrai,
        duoiphai,
        duoitrai,
        idel,
        tuantra,
        truykich
    }

    public DirectionRandom Dir = DirectionRandom.idel;
    GameObject player;
    void Start()
    {
        rb = GetComponent<Rigidbody>();       
        indexdir = 9;
        InvokeRepeating("RandomDir", 1f, 3f);
        player = GameObject.FindGameObjectWithTag("player");
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        agent.enabled = false;
       
    }
    public float speedtuantra;
    public float speedvelocity;
    void FixedUpdate()
    {
        switch (Dir)
        {
            case DirectionRandom.tren:
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, speedvelocity);
                break;
            case DirectionRandom.duoi:
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, -speedvelocity);
                break;
            case DirectionRandom.trai:
                rb.linearVelocity = new Vector3(-speedvelocity, rb.linearVelocity.y, rb.linearVelocity.z);
                break;
            case DirectionRandom.phai:
                rb.linearVelocity = new Vector3(speedvelocity, rb.linearVelocity.y, rb.linearVelocity.z);
                break;
            case DirectionRandom.trenphai:
                rb.linearVelocity = new Vector3(speedvelocity, rb.linearVelocity.y, speedvelocity);
                break;
            case DirectionRandom.trentrai:
                rb.linearVelocity = new Vector3(-speedvelocity, rb.linearVelocity.y, speedvelocity);
                break;
            case DirectionRandom.duoiphai:
                rb.linearVelocity = new Vector3(speedvelocity, rb.linearVelocity.y, -speedvelocity);
                break;
            case DirectionRandom.duoitrai:
                rb.linearVelocity = new Vector3(-speedvelocity, rb.linearVelocity.y, -speedvelocity);
                break;
            case DirectionRandom.idel:
                rb.linearVelocity = new Vector3(0, 0, 0);
                break;
            case DirectionRandom.tuantra:
                if (player != null)
                {
                    agent.SetDestination(player.transform.position);
                }
                         
                transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                break;
            case DirectionRandom.truykich:
               
                break;
            default:
                break;
        }  
    }


   public int indexdir;
    Vector3 dirplayer;
    public int ratetruyduoi;
    void RandomDir()
    {
        rb.linearVelocity = new Vector3(0, 0, 0);
        agent.enabled = false;
        indexdir = Random.Range(0, ratetruyduoi);
       //Debug.Log(indexdir);
       switch (indexdir)
       {
           case 0: 
               Dir = DirectionRandom.tren;
              transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
               break;
           case 1:
               Dir = DirectionRandom.duoi;
               transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
              
               break;
           case 2:
               Dir = DirectionRandom.trai;
               transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
               break;
           case 3:
               Dir = DirectionRandom.phai;
               transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
               break;
           case 4:
               Dir = DirectionRandom.trenphai;
               transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
               break;
           case 5:
               Dir = DirectionRandom.trentrai;
               transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
               break;
           case 6:
               Dir = DirectionRandom.duoiphai;
               transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, 145f, 0f);
               break;
           case 8:
               Dir = DirectionRandom.duoitrai;
               transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, -145f, 0f);
               break;
           case 9:
               Dir = DirectionRandom.idel;
               break;
           //case 10:
           //    Dir = DirectionRandom.tuantra;
           //    dirplayer = player.transform.position - transform.position;
           //    transform.LookAt(player.transform);
           //    break;
           //case 11:
           //    Dir = DirectionRandom.truykich;
           //    break;
           default:

                agent.enabled = true;
                Dir = DirectionRandom.tuantra;
               break;
       }
    }


    void Battuong()
    {
        switch (indexdir)
        {
            case 0:
                Dir = DirectionRandom.duoi;
               transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                break;
            case 1:
                Dir = DirectionRandom.tren;
              transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

                break;
            case 2:
                  Dir = DirectionRandom.phai;
               transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                break;
            case 3:
                Dir = DirectionRandom.trai;
               transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
                break;
            case 4:             
                 Dir = DirectionRandom.duoitrai;
                transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, -145f, 0f);
                break;
            case 5:

                 Dir = DirectionRandom.duoitrai;
                transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, -145f, 0f); 
                break;
            case 6:
                Dir = DirectionRandom.trentrai;
                transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
                break;
            case 8:
                Dir = DirectionRandom.trenphai;
                transform.GetChild(1).gameObject.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                break;
            case 9:
                Dir = DirectionRandom.idel;
                break;
            //case 10:
            //    Dir = DirectionRandom.tuantra;
            //    dirplayer = player.transform.position - transform.position;
            //    transform.LookAt(player.transform);
            //    break;
            //case 11:
            //    Dir = DirectionRandom.truykich;
            //    break;
            default:
                agent.enabled = true;

                Dir = DirectionRandom.tuantra;
                break;
        }
    }


    void OnCollisionEnter(Collision coll)
    {      
            Battuong();   
    }

    //void OnTriggerEnter(Collider other) {     

    //        Battuong();     
    //}

   
    //IEnumerator RandomDirection()
    //{
    //    while (true)
    //    {
    //        yield return new WaitForSeconds(2f);
    //        RandomDir();
    //    }

    //}
}
