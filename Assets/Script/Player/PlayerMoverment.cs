using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;

public class PlayerMoverment : MonoBehaviour {

    public static PlayerMoverment instance;

    private Vector3 _movment;
    public float Speed;

    Rigidbody _playerRigidbody;

    public Joystick joystick;

    Camera viewCamera;

    //Bắn đạn Player
    public GameObject Bullet;
    public Transform pointbullet;
    // Bullet Ban 3
    public GameObject Bullet3;
    // Ban dan duoi
    public GameObject BulletFind;
    //Vụ nổ đầu pháo
    public GameObject Ex;
    //Vụ Nổ Player
    public GameObject ExTank;
    public List<GameObject> Heart = new List<GameObject>();
    public GameObject StyteBullet;
    public enum PlayerType
    {
        banthang,
        banvong,
        bandan3,
        bandanduoi
    }
    public PlayerType typeplayer;
	// Use this for initialization
	void Start () {
        instance = this;
        _playerRigidbody = GetComponent<Rigidbody>();
        viewCamera = Camera.main;
        canbullet = true;
        if (PlayerPrefs.GetInt("TreadShredArmorCache", 0) > 0)
        {
            countdie = 4;
            PlayerPrefs.DeleteKey("TreadShredArmorCache");
            PlayerPrefs.Save();
        }
       if (StyteBullet == null)
        {
            StyteBullet = GameObject.Find("Canvas");
        }
       
        switch (typeplayer)
        {
            case PlayerType.banthang:
                StyteBullet.transform.GetChild(1).gameObject.transform.GetChild(0).gameObject.SetActive(true);
                break;
            case PlayerType.banvong:
                StyteBullet.transform.GetChild(1).gameObject.transform.GetChild(1).gameObject.SetActive(true);
                break;
            case PlayerType.bandan3:
                StyteBullet.transform.GetChild(1).gameObject.transform.GetChild(2).gameObject.SetActive(true);
                break;
            case PlayerType.bandanduoi:
                StyteBullet.transform.GetChild(1).gameObject.transform.GetChild(3).gameObject.SetActive(true);
                break;
            default:
                break;
        }
	}
  
    Vector2 destiny;
    Vector3 dirbullet;
    public bool isstop;
	// Update is called once per frame
    private void FixedUpdate()
    {
        float hjoystick = joystick.Horizontal();
        float vjoystick = joystick.Vertical();
        if (isstop == false)
        {
            Move(hjoystick, vjoystick);
            joystick.RotateTarget(transform.GetChild(1));
        }
        

      
      
	}

    float raydistane;
    //Thời gian hạn chết bắn đạn
 
  public  float timercountup;
    //Hạn chế số lượng đạn là 3
  public int countbullet = 0;
    bool canbullet;
    //Biến kiểm tra dùng trên điện thoại khi không có touch nào trên object
    bool isnotoverobject;
    void Update()
    {

        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
        {
            // Đếm thời gian
            if (canbullet == false)
            {
                timercountup += Time.deltaTime;
                if (timercountup > 0.5f)
                {
                    timercountup = 0f;
                    canbullet = true;
                }
            }


            //Kiểm tra khi touch va chạm màn hình
            int nbTouches = Input.touchCount;
            // Nếu touch lớn hơn 0
            if (nbTouches > 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    Touch touch = Input.GetTouch(i);

                    if (touch.phase == TouchPhase.Began)
                    {

                        // Nếu touch đấy va chạm vào gameobject mới cho bắn
                        if (EventSystem.current.IsPointerOverGameObject(touch.fingerId) == false)
                        {

                            if (gameObject.GetComponent<BoxCollider>().enabled)
                            {
                                // Raycast
                                if (countbullet < 3 && canbullet)
                                {
                                    countTimeButtonUp = false;
                                    timercountup = 0f;
                                    countbullet++;
                                    isnotoverobject = true;
                                    ManagerScore.instance.CurCountbullet++;
                                    Ray ray = Camera.main.ScreenPointToRay(touch.position);

                                    Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                                    if (groundPlane.Raycast(ray, out raydistane))
                                    {
                                        switch (typeplayer)
                                        {
                                            case PlayerType.banthang:
                                                Vector3 point = ray.GetPoint(raydistane);
                                                //  Debug.DrawRay(ray.origin, point, Color.red);
                                                Vector3 heightcorrect = new Vector3(point.x, transform.GetChild(0).gameObject.transform.position.y, point.z);
                                                transform.GetChild(0).gameObject.transform.LookAt(heightcorrect);

                                                GameObject obj = Instantiate(Bullet, pointbullet.position, pointbullet.rotation) as GameObject;
                                                Vector3 dir = point - pointbullet.position;

                                                GameObject ExP = Instantiate(Ex, pointbullet.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
                                                ExP.transform.SetParent(transform);
                                                MusicManager.instance.GunTank();
                                                obj.GetComponent<Rigidbody>().velocity = dir.normalized * 15f;
                                                break;
                                            case PlayerType.banvong:
                                                Vector3 point1 = ray.GetPoint(raydistane);
                                                Vector3 heightcorrect1 = new Vector3(point1.x, transform.GetChild(0).gameObject.transform.position.y, point1.z);
                                                transform.GetChild(0).gameObject.transform.LookAt(heightcorrect1);
                                                GameObject ExP1 = Instantiate(Ex, pointbullet.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
                                                ExP1.transform.SetParent(transform);
                                                MusicManager.instance.GunTank();
                                                gameObject.GetComponent<ThrowSimulation>().CallFromPlayer(point1);
                                                break;
                                            case PlayerType.bandan3:

                                                Vector3 point3 = ray.GetPoint(raydistane);
                                                Vector3 heightcorrect3 = new Vector3(point3.x, transform.GetChild(0).gameObject.transform.position.y, point3.z);
                                                transform.GetChild(0).gameObject.transform.LookAt(heightcorrect3);
                                                GameObject ExP3 = Instantiate(Ex, pointbullet.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
                                                ExP3.transform.SetParent(transform);
                                                MusicManager.instance.GunTank();
                                                GameObject obj3 = Instantiate(Bullet3, pointbullet.position, pointbullet.rotation) as GameObject;

                                                break;
                                            case PlayerType.bandanduoi:
                                                Vector3 point4 = ray.GetPoint(raydistane);
                                                //  Debug.DrawRay(ray.origin, point, Color.red);
                                                Vector3 heightcorrect4 = new Vector3(point4.x, transform.GetChild(0).gameObject.transform.position.y, point4.z);
                                                transform.GetChild(0).gameObject.transform.LookAt(heightcorrect4);

                                                GameObject obj4 = Instantiate(BulletFind, pointbullet.position, pointbullet.rotation) as GameObject;

                                                Vector3 dir4 = point4 - pointbullet.position;

                                                GameObject ExP4 = Instantiate(Ex, pointbullet.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
                                                ExP4.transform.SetParent(transform);
                                                MusicManager.instance.GunTank();
                                                obj4.GetComponent<Rigidbody>().velocity = dir4.normalized * 10f;

                                                break;
                                            default:
                                                break;
                                        }
                                    }

                                }
                                else
                                {
                                    canbullet = false;
                                    countbullet = 0;
                                }
                            }

                            

                        }

                        isnotoverobject = false;
                    }
                }

            }
            else
            {
                isnotoverobject = false;
            }

            //else
            //{
            //    timercountup += Time.deltaTime;
            //    if (timercountup > 0.5f)
            //    {
            //        timercountup = 0f;
            //        countbullet = 0;
            //        canbullet = true;
            //    }
            //}

            //if (nbTouches > 0)
            //{
            //    //for (int i = 0; i < 2; i++)
            //    //{
            //    //    Touch touch = Input.GetTouch(i);

            //    //    if (touch.phase == TouchPhase.Began)
            //    //    {

            //    //        // Nếu touch chạm trên UI 
            //    //        if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            //    //        {
            //    //            isnotoverobject = true;
            //    //        }
            //    //        //else
            //    //        //{
            //    //        //    isnotoverobject = false;
            //    //        //}
            //    //    }
            //    //}

            //    if (Input.GetTouch(0).phase == TouchPhase.Began)
            //    {
            //        // Check if finger is over a UI element
            //        if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            //        {
            //            isnotoverobject = true;
            //        }
            //    }	

            //}

            //else
            //{
            //    isnotoverobject = true;
            //}

            if (isnotoverobject == false)
            {
                countTimeButtonUp = true;
            }

            //Đang sửa
            //if (Input.GetMouseButtonUp(0))
            //{
            //    countTimeButtonUp = true;

            //}


            if (countTimeButtonUp)
            {
                if (canbullet)
                {
                    timercountup += Time.deltaTime;
                    if (timercountup > 0.3f)
                    {
                        timercountup = 0f;
                        canbullet = true;
                        countbullet = 0;
                    }

                }

            }



        }
        // Bắt đầu chỉ dùng cho máy tính
        else
        {
            //Đếm thời gian
            if (canbullet == false)
            {
                timercountup += Time.deltaTime;
                if (timercountup > 1f)
                {
                    timercountup = 0f;
                    canbullet = true;
                }
            }

           

            if (Input.GetMouseButtonDown(0))
            {
                if (gameObject.GetComponent<BoxCollider>().enabled)
                {
                    if (countbullet < 3 && canbullet)
                    {
                        countTimeButtonUp = false;
                        timercountup = 0f;
                        countbullet++;
                        ManagerScore.instance.CurCountbullet++;
                        if (!EventSystem.current.IsPointerOverGameObject())
                        {
                            Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
                            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                            if (groundPlane.Raycast(ray, out raydistane))
                            {

                                switch (typeplayer)
                                {
                                    case PlayerType.banthang:
                                        Vector3 point = ray.GetPoint(raydistane);
                                        //  Debug.DrawRay(ray.origin, point, Color.red);
                                        Vector3 heightcorrect = new Vector3(point.x, transform.GetChild(0).gameObject.transform.position.y, point.z);
                                        transform.GetChild(0).gameObject.transform.LookAt(heightcorrect);

                                        GameObject obj = Instantiate(Bullet, pointbullet.position, pointbullet.rotation) as GameObject;
                                        Vector3 dir = point - pointbullet.position;

                                        GameObject ExP = Instantiate(Ex, pointbullet.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
                                        ExP.transform.SetParent(transform);
                                        MusicManager.instance.GunTank();
                                        obj.GetComponent<Rigidbody>().velocity = dir.normalized * 15f;
                                        break;
                                    case PlayerType.banvong:
                                        Vector3 point1 = ray.GetPoint(raydistane);
                                        Vector3 heightcorrect1 = new Vector3(point1.x, transform.GetChild(0).gameObject.transform.position.y, point1.z);
                                        transform.GetChild(0).gameObject.transform.LookAt(heightcorrect1);
                                        GameObject ExP1 = Instantiate(Ex, pointbullet.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
                                        ExP1.transform.SetParent(transform);
                                        MusicManager.instance.GunTank();
                                        gameObject.GetComponent<ThrowSimulation>().CallFromPlayer(point1);
                                        break;
                                    case PlayerType.bandan3:

                                        Vector3 point3 = ray.GetPoint(raydistane);
                                        Vector3 heightcorrect3 = new Vector3(point3.x, transform.GetChild(0).gameObject.transform.position.y, point3.z);
                                        transform.GetChild(0).gameObject.transform.LookAt(heightcorrect3);
                                        GameObject ExP3 = Instantiate(Ex, pointbullet.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
                                        ExP3.transform.SetParent(transform);
                                        MusicManager.instance.GunTank();
                                        GameObject obj3 = Instantiate(Bullet3, pointbullet.position, pointbullet.rotation) as GameObject;

                                        break;
                                    case PlayerType.bandanduoi:
                                        Vector3 point4 = ray.GetPoint(raydistane);
                                        //  Debug.DrawRay(ray.origin, point, Color.red);
                                        Vector3 heightcorrect4 = new Vector3(point4.x, transform.GetChild(0).gameObject.transform.position.y, point4.z);
                                        transform.GetChild(0).gameObject.transform.LookAt(heightcorrect4);

                                        GameObject obj4 = Instantiate(BulletFind, pointbullet.position, pointbullet.rotation) as GameObject;

                                        Vector3 dir4 = point4 - pointbullet.position;

                                        GameObject ExP4 = Instantiate(Ex, pointbullet.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
                                        ExP4.transform.SetParent(transform);
                                        MusicManager.instance.GunTank();
                                        obj4.GetComponent<Rigidbody>().velocity = dir4.normalized * 10f;

                                        break;
                                    default:
                                        break;
                                }

                            }
                        }
                    }

                    else
                    {
                        canbullet = false;
                        countbullet = 0;
                    }

                }
               
            }




            if (Input.GetMouseButtonUp(0))
            {
                countTimeButtonUp = true;
               
            }


            if (countTimeButtonUp)
            {
                if (canbullet)
                {
                    timercountup += Time.deltaTime;
                    if (timercountup > 0.3f)
                    {                 
                            timercountup = 0f;
                            canbullet = true;
                            countbullet = 0;
                    }
                   
                }

            }

        }
        
      

     
    }

    bool countTimeButtonUp;
    float nextshot;
    
    public void Move(float h, float v)
    {
        _movment.Set(h, 0, v);
        _movment = _movment.normalized * Speed * Time.deltaTime;
        _playerRigidbody.MovePosition(transform.position + _movment);
    }

   public int countdie = 3;
  
   public void DeadPlayer(int index)
    {
        if (countdie >= 1)
        {
            ManagerScore.instance.CurHeath++;
        }

        countdie -= index;
        StartCoroutine(DelayHeath());

        if (countdie < 1 && countdie > -1)
        {
            MusicManager.instance.ExplosionTank();
            ManagerScore.instance.Rung();
            Instantiate(ExTank, pointbullet.position, Quaternion.Euler(-90, 0, 0));
            ManagerScore.instance.CaculaterScore();
            ManagerScore.instance.Failure();
         
            GetComponent<BoxCollider>().enabled = false;
            GetComponent<BoxCollider>().enabled = false;
            transform.GetChild(0).gameObject.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
            transform.GetChild(1).gameObject.GetComponent<MeshRenderer>().enabled = false;
            transform.GetChild(2).gameObject.GetComponent<MeshRenderer>().enabled = false;

            gameObject.GetComponent<BoxCollider>().enabled = false;
            gameObject.GetComponent<BoxCollider>().enabled = false;
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(false);
            transform.GetChild(2).gameObject.SetActive(false);
            Destroy(gameObject, 4f);
        }
    }

   IEnumerator DelayHeath()
   {
      

       switch (countdie)
       {
           case 2:

               for (int i = 0; i < 10; i++)
               {
                   if (i<5)
                   {
                       yield return new WaitForSeconds(0.05f);
                       Heart[0].gameObject.transform.localScale += new Vector3(0.1f, 0.1f, 0);
                   }
                   else
                   {
                       yield return new WaitForSeconds(0.05f);
                       Heart[0].gameObject.transform.localScale += new Vector3(-0.1f,-0.1f, 0);
                   }
                   
               }
              
               Color a = Heart[0].gameObject.GetComponent<Image>().color;
               a.a = 0.15f;
               Heart[0].gameObject.GetComponent<Image>().color = a;
               break;
           case 1:

               for (int i = 0; i < 10; i++)
               {
                   if (i < 5)
                   {
                       yield return new WaitForSeconds(0.05f);
                       Heart[1].gameObject.transform.localScale += new Vector3(0.1f, 0.1f, 0);
                   }
                   else
                   {
                       yield return new WaitForSeconds(0.05f);
                       Heart[1].gameObject.transform.localScale += new Vector3(-0.1f, -0.1f, 0);
                   }

               }
               Color b = Heart[1].gameObject.GetComponent<Image>().color;
               b.a = 0.15f;
               Heart[1].gameObject.GetComponent<Image>().color = b;
               break;
           case 0:

               if (Heart[1].gameObject.GetComponent<Image>().color.a < 0.5f)
               {

                   for (int i = 0; i < 10; i++)
                   {
                       if (i < 5)
                       {
                           yield return new WaitForSeconds(0.05f);
                           Heart[2].gameObject.transform.localScale += new Vector3(0.1f, 0.1f, 0);
                       }
                       else
                       {
                           yield return new WaitForSeconds(0.05f);
                           Heart[2].gameObject.transform.localScale += new Vector3(-0.1f, -0.1f, 0);
                       }

                   }
                   Color c = Heart[2].gameObject.GetComponent<Image>().color;
                   c.a = 0.15f;
                   Heart[2].gameObject.GetComponent<Image>().color = c;
                   break;

               }
               else
               {

                   for (int i = 0; i < 10; i++)
                   {
                       if (i < 5)
                       {
                           yield return new WaitForSeconds(0.05f);
                           Heart[0].gameObject.transform.localScale += new Vector3(0.1f, 0.1f, 0);
                       }
                       else
                       {
                           yield return new WaitForSeconds(0.05f);
                           Heart[0].gameObject.transform.localScale += new Vector3(-0.1f, -0.1f, 0);
                       }

                   }

                   Color a1 = Heart[0].gameObject.GetComponent<Image>().color;
                   a1.a = 0.15f;
                   Heart[0].gameObject.GetComponent<Image>().color = a1;

                   for (int i = 0; i < 10; i++)
                   {
                       if (i < 5)
                       {
                           yield return new WaitForSeconds(0.05f);
                           Heart[1].gameObject.transform.localScale += new Vector3(0.1f, 0.1f, 0);
                       }
                       else
                       {
                           yield return new WaitForSeconds(0.05f);
                           Heart[1].gameObject.transform.localScale += new Vector3(-0.1f, -0.1f, 0);
                       }

                   }
                   Color b1 = Heart[1].gameObject.GetComponent<Image>().color;
                   b1.a = 0.15f;
                   Heart[1].gameObject.GetComponent<Image>().color = b1;

                   for (int i = 0; i < 10; i++)
                   {
                       if (i < 5)
                       {
                           yield return new WaitForSeconds(0.05f);
                           Heart[2].gameObject.transform.localScale += new Vector3(0.1f, 0.1f, 0);
                       }
                       else
                       {
                           yield return new WaitForSeconds(0.05f);
                           Heart[2].gameObject.transform.localScale += new Vector3(-0.1f, -0.1f, 0);
                       }

                   }
                   Color c1 = Heart[2].gameObject.GetComponent<Image>().color;
                   c1.a = 0.15f;
                   Heart[2].gameObject.GetComponent<Image>().color = c1;
                   break;
               }
           default:
               break;
       }

   }


}
