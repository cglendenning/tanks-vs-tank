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

    [SerializeField] private bool enforceArenaBounds = true;
    [SerializeField] private float arenaBoundaryInset = 0.05f;
    private bool arenaBoundsCached;
    private Bounds arenaBounds;

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

    // D-038: the mission's own weapon; the HUD toggle switches typeplayer between it and the cannon.
    private PlayerType missionType;
    public TreadShred.Progression.Weapon MissionWeapon => (TreadShred.Progression.Weapon)(int)missionType;
    public TreadShred.Progression.Weapon WeaponInHand => (TreadShred.Progression.Weapon)(int)typeplayer;

    /// <summary>D-043-AC-04: a rented weapon goes in hand at once.</summary>
    public void Rent(TreadShred.Progression.Weapon weapon)
    {
        typeplayer = (PlayerType)(int)weapon;
        TreadShredWeaponToggle.Refresh();
    }

    /// <summary>D-038-AC-03: the drawer's tap; the cannon, the mission's own weapon or a rented one can be in hand.</summary>
    public void SelectWeapon(TreadShred.Progression.Weapon weapon)
    {
        if (weapon == TreadShred.Progression.Weapon.Cannon || weapon == MissionWeapon
            || TreadShredProgress.Ensure().Progression.RentalLeft(weapon) > 0)
            typeplayer = (PlayerType)(int)weapon;
    }
	// Use this for initialization
	void Start () {
        instance = this;
        _playerRigidbody = GetComponent<Rigidbody>();
        if (_playerRigidbody != null)
        {
            // Movement uses Rigidbody.MovePosition. Continuous detection plus a
            // sweep in Move() prevents the tank from tunneling through the
            // thin fortification colliders at high joystick input or corners.
            _playerRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _playerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }
        CacheArenaBounds();
        viewCamera = Camera.main;
        canbullet = true;
        // Armor and speed upgrades (D-017); Armored Redeploy adds one hit (D-009).
        var progress = TreadShredProgress.Ensure();
        var loadout = TreadShredBoosts.Current.ConsumeForMission(progress.ArmorHits);
        countdie = loadout.ArmorHits;
        missionArmorHits = loadout.ArmorHits;
        // D-035-AC-03: a first-time player's first play of mission 1 has Overdrive free for that mission and a fire hint.
        var firstPlay = TreadShredFirstMinute.BeginMission(TreadShredProgress.CurrentMission());
        overdriveActive = loadout.RoundsPerTap > 1 || firstPlay;
        if (overdriveActive)
            TreadShredOverdriveBadge.Show(TreadShredUi.FindFont(GameObject.Find("Canvas")));
        // D-017-AC-10: the upgraded tracks under the Overdrive tag, and armor beyond the three shields as +N beside them.
        var hudFont = TreadShredUi.FindFont(GameObject.Find("Canvas"));
        TreadShredOverdriveBadge.ShowUpgrades(hudFont, TreadShred.Progression.ProgressionLabels.ActiveUpgrades(progress.Progression.Level));
        extraArmor = TreadShredExtraArmor.Attach(Heart, hudFont);
        ShowExtraArmor();
        if (firstPlay)
        {
            var font = TreadShredUi.FindFont(GameObject.Find("Canvas"));
            TreadShredFireHint.Show(font, transform);
            TreadShredFirstMinuteCues.Show(font, joystick);
        }
        Speed *= progress.SpeedMultiplier;
        // D-039: the mission's ground. D-038-AC-01: the old unlabeled weapon icon never shows; a special weapon gets the
        // named toggle instead.
        TreadShredGround.Apply(TreadShredProgress.CurrentMission());
        missionType = typeplayer;
        // D-043: playing a special weapon's map makes it rentable on every later mission; the mission starts on its own
        // weapon, or on a cannon-only mission on a running rental's. D-038: the weapon button and drawer.
        var nativeWeapon = MissionWeapon;
        progress.Progression.WeaponUsed(nativeWeapon);
        typeplayer = (PlayerType)(int)progress.Progression.StartingWeapon(nativeWeapon);
        TreadShredWeaponToggle.Attach(this, MissionWeapon, TreadShredUi.FindFont(GameObject.Find("Canvas")));
        TreadShredMissionLabel.Show(TreadShredUi.FindFont(GameObject.Find("Canvas")));
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
    private bool overdriveActive;
    // Full armor for this mission, including an Armored Redeploy hit (D-009, D-020).
    private int missionArmorHits;
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
                for (int i = 0; i < Mathf.Min(2, nbTouches); i++)
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
                                                obj.GetComponent<Rigidbody>().linearVelocity = dir.normalized * 15f;
                                                FireOverdrive(point);
                                                break;
                                            case PlayerType.banvong:
                                                Vector3 point1 = ray.GetPoint(raydistane);
                                                Vector3 heightcorrect1 = new Vector3(point1.x, transform.GetChild(0).gameObject.transform.position.y, point1.z);
                                                transform.GetChild(0).gameObject.transform.LookAt(heightcorrect1);
                                                GameObject ExP1 = Instantiate(Ex, pointbullet.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
                                                ExP1.transform.SetParent(transform);
                                                MusicManager.instance.GunTank();
                                                gameObject.GetComponent<ThrowSimulation>().CallFromPlayer(point1);
                                                FireOverdrive(point1);
                                                break;
                                            case PlayerType.bandan3:

                                                Vector3 point3 = ray.GetPoint(raydistane);
                                                Vector3 heightcorrect3 = new Vector3(point3.x, transform.GetChild(0).gameObject.transform.position.y, point3.z);
                                                transform.GetChild(0).gameObject.transform.LookAt(heightcorrect3);
                                                GameObject ExP3 = Instantiate(Ex, pointbullet.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
                                                ExP3.transform.SetParent(transform);
                                                MusicManager.instance.GunTank();
                                                GameObject obj3 = Instantiate(Bullet3, pointbullet.position, pointbullet.rotation) as GameObject;
                                                FireOverdrive(point3);

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
                                                obj4.GetComponent<Rigidbody>().linearVelocity = dir4.normalized * 10f;
                                                FireOverdrive(point4);

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

                                FireAt(ray.GetPoint(raydistane));

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
        if (_playerRigidbody == null || _movment.sqrMagnitude <= 0.000001f)
            return;

        var direction = _movment.normalized;
        var distance = _movment.magnitude;
        if (_playerRigidbody.SweepTest(direction, out RaycastHit wallHit, distance + Physics.defaultContactOffset,
            QueryTriggerInteraction.Ignore))
        {
            // Stop just short of the wall and preserve the component of input
            // that runs along it. This keeps the tank responsive without
            // allowing a single frame to cross the arena boundary.
            var skin = Mathf.Max(Physics.defaultContactOffset * 2f, 0.01f);
            var travel = Mathf.Max(0f, wallHit.distance - skin);
            var safeMovement = direction * Mathf.Min(distance, travel);
            var remainingMovement = _movment - safeMovement;
            _movment = safeMovement + Vector3.ProjectOnPlane(remainingMovement, wallHit.normal);
        }

        var nextPosition = transform.position + _movment;
        if (arenaBoundsCached)
        {
            var playerCollider = GetComponent<Collider>();
            var halfWidth = playerCollider != null ? playerCollider.bounds.extents.x : 0f;
            var halfDepth = playerCollider != null ? playerCollider.bounds.extents.z : 0f;
            var inset = Mathf.Max(0f, arenaBoundaryInset);
            nextPosition.x = Mathf.Clamp(nextPosition.x,
                arenaBounds.min.x + halfWidth + inset,
                arenaBounds.max.x - halfWidth - inset);
            nextPosition.z = Mathf.Clamp(nextPosition.z,
                arenaBounds.min.z + halfDepth + inset,
                arenaBounds.max.z - halfDepth - inset);
        }

        _playerRigidbody.MovePosition(nextPosition);
    }

    private void CacheArenaBounds()
    {
        if (!enforceArenaBounds)
            return;

        var wallObjects = GameObject.FindGameObjectsWithTag("thanh");
        var foundCollider = false;
        foreach (var wallObject in wallObjects)
        {
            var colliders = wallObject.GetComponentsInChildren<Collider>(true);
            foreach (var wallCollider in colliders)
            {
                if (wallCollider == null || wallCollider.isTrigger)
                    continue;

                if (!foundCollider)
                {
                    arenaBounds = wallCollider.bounds;
                    foundCollider = true;
                }
                else
                {
                    arenaBounds.Encapsulate(wallCollider.bounds);
                }
            }
        }

        arenaBoundsCached = foundCollider;
    }


    // Fires the current weapon at a ground point. Used by mouse input and the
    // store ad-reel capture mode, so both go through the same weapon code.
    public void FireAt(Vector3 target)
    {
        switch (typeplayer)
        {
            case PlayerType.banthang:
                Vector3 point = target;
                //  Debug.DrawRay(ray.origin, point, Color.red);
                Vector3 heightcorrect = new Vector3(point.x, transform.GetChild(0).gameObject.transform.position.y, point.z);
                transform.GetChild(0).gameObject.transform.LookAt(heightcorrect);

                GameObject obj = Instantiate(Bullet, pointbullet.position, pointbullet.rotation) as GameObject;
                Vector3 dir = point - pointbullet.position;

                GameObject ExP = Instantiate(Ex, pointbullet.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
                ExP.transform.SetParent(transform);
                MusicManager.instance.GunTank();
                obj.GetComponent<Rigidbody>().linearVelocity = dir.normalized * 15f;
                FireOverdrive(point);
                break;
            case PlayerType.banvong:
                Vector3 point1 = target;
                Vector3 heightcorrect1 = new Vector3(point1.x, transform.GetChild(0).gameObject.transform.position.y, point1.z);
                transform.GetChild(0).gameObject.transform.LookAt(heightcorrect1);
                GameObject ExP1 = Instantiate(Ex, pointbullet.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
                ExP1.transform.SetParent(transform);
                MusicManager.instance.GunTank();
                gameObject.GetComponent<ThrowSimulation>().CallFromPlayer(point1);
                FireOverdrive(point1);
                break;
            case PlayerType.bandan3:

                Vector3 point3 = target;
                Vector3 heightcorrect3 = new Vector3(point3.x, transform.GetChild(0).gameObject.transform.position.y, point3.z);
                transform.GetChild(0).gameObject.transform.LookAt(heightcorrect3);
                GameObject ExP3 = Instantiate(Ex, pointbullet.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
                ExP3.transform.SetParent(transform);
                MusicManager.instance.GunTank();
                GameObject obj3 = Instantiate(Bullet3, pointbullet.position, pointbullet.rotation) as GameObject;
                FireOverdrive(point3);

                break;
            case PlayerType.bandanduoi:
                Vector3 point4 = target;
                //  Debug.DrawRay(ray.origin, point, Color.red);
                Vector3 heightcorrect4 = new Vector3(point4.x, transform.GetChild(0).gameObject.transform.position.y, point4.z);
                transform.GetChild(0).gameObject.transform.LookAt(heightcorrect4);

                GameObject obj4 = Instantiate(BulletFind, pointbullet.position, pointbullet.rotation) as GameObject;

                Vector3 dir4 = point4 - pointbullet.position;

                GameObject ExP4 = Instantiate(Ex, pointbullet.position, Quaternion.Euler(-90, 0, 0)) as GameObject;
                ExP4.transform.SetParent(transform);
                MusicManager.instance.GunTank();
                obj4.GetComponent<Rigidbody>().linearVelocity = dir4.normalized * 10f;
                FireOverdrive(point4);

                break;
            default:
                break;
        }
    }

    private void FireOverdrive(Vector3 target)
    {
        if (!overdriveActive || pointbullet == null)
            return;

        var offset = transform.right * 0.42f;
        var secondaryTarget = target + transform.right * 0.72f;
        switch (typeplayer)
        {
            case PlayerType.banthang:
            {
                var projectile = Instantiate(Bullet, pointbullet.position + offset, pointbullet.rotation);
                var direction = secondaryTarget - pointbullet.position;
                projectile.GetComponent<Rigidbody>().linearVelocity = direction.normalized * 15f;
                break;
            }
            case PlayerType.banvong:
                gameObject.GetComponent<ThrowSimulation>().CallFromPlayer(secondaryTarget);
                break;
            case PlayerType.bandan3:
                Instantiate(Bullet3, pointbullet.position + offset, pointbullet.rotation);
                break;
            case PlayerType.bandanduoi:
            {
                var projectile = Instantiate(BulletFind, pointbullet.position + offset, pointbullet.rotation);
                var direction = secondaryTarget - pointbullet.position;
                projectile.GetComponent<Rigidbody>().linearVelocity = direction.normalized * 10f;
                break;
            }
        }
    }

   public int countdie = 3;
  
   public void DeadPlayer(int index)
    {
        if (countdie >= 1)
        {
            ManagerScore.instance.CurHeath++;
        }

        // D-035-AC-03: on a first play a hit never takes the last armor.
        countdie = TreadShred.Progression.FirstMinute.ArmorAfterHit(countdie, index, TreadShredFirstMinute.FirstPlay);
        ShowExtraArmor();
        TreadShredImpact.HitTaken();
        if (shieldFade != null)
            StopCoroutine(shieldFade);
        shieldFade = StartCoroutine(DelayHeath());

        if (countdie < 1 && countdie > -1)
        {
            // D-017-AC-09: destroyed means no shields left on the HUD, at once (the revive panel pauses the game).
            StopCoroutine(shieldFade);
            shieldFade = null;
            SetShields(0.15f);
            // D-020: offer a revive before the tank is lost.
            if (!TreadShredRevivePanel.TryOffer(this))
                Die();
        }
    }

    // The shield fade under way, stopped by a revive so it can never dim a shield the revive restored.
    private Coroutine shieldFade;

    // D-017-AC-10: armor beyond the drawn shields, as +N beside them.
    private Text extraArmor;

    private void ShowExtraArmor()
    {
        if (extraArmor != null)
            extraArmor.text = TreadShred.Progression.ProgressionLabels.ExtraArmor(countdie, Heart.Count);
    }

    // Lit shields on the HUD (-1 before the first hit), and the shields in the order they go out.
    private int shieldsShown = -1;
    private int[] shieldLine;

    private void SetShields(float alpha)
    {
        foreach (var heart in Heart)
            ShowShield(heart, alpha, Vector3.one);
        shieldsShown = alpha > 0.5f ? Heart.Count : 0;
    }

    private static void ShowShield(GameObject heart, bool lit) =>
        ShowShield(heart, lit ? 1f : TreadShred.Progression.ShieldRules.DimAlpha, Vector3.one);

    private static void ShowShield(GameObject heart, float alpha, Vector3 scale)
    {
        if (heart == null)
            return;
        heart.transform.localScale = scale;
        var image = heart.GetComponent<Image>();
        if (image == null)
            return;
        var color = image.color;
        color.a = alpha;
        image.color = color;
    }

    // D-020: restore the tank in place at full armor after a paid revive, with the enemy shells still in flight cleared
    // so fire aimed at the destroyed tank never hits the revived one (D-020-AC-07).
    public void Revive()
    {
        var cleared = TreadShredEnemyShells.Clear();
        if (cleared > 0)
            Debug.Log("[Revive] cleared " + cleared + " enemy shells in flight");
        countdie = missionArmorHits;
        ShowExtraArmor();
        if (shieldFade != null)
            StopCoroutine(shieldFade);
        shieldFade = null;
        SetShields(1f);
    }

    public void Die()
    {
        {
            MusicManager.instance.ExplosionTank();
            ManagerScore.instance.Rung();
            Instantiate(ExTank, pointbullet.position, Quaternion.Euler(-90, 0, 0));
            ManagerScore.instance.CaculaterScore(false);
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

   // D-017-AC-09: every shield shows the hits left at once, so a hit landing during another's pulse can never leave a
   // middle shield out alone; only the shield this hit took pulses (in real time, so a pause cannot freeze it) and dims.
   IEnumerator DelayHeath()
   {
       var line = ShieldLine();
       var lit = TreadShred.Progression.ShieldRules.Lit(countdie, line.Length);
       var before = shieldsShown < 0 ? line.Length : shieldsShown;
       shieldsShown = lit;
       var losing = lit < before ? line.Length - lit - 1 : -1;
       for (var place = 0; place < line.Length; place++)
           ShowShield(Heart[line[place]], place == losing || TreadShred.Progression.ShieldRules.IsLit(place, countdie, line.Length));
       if (losing < 0 || Heart[line[losing]] == null)
           yield break;
       var shield = Heart[line[losing]].transform;
       for (var step = 0; step < 10; step++)
       {
           yield return new WaitForSecondsRealtime(0.05f);
           shield.localScale += (step < 5 ? 0.1f : -0.1f) * new Vector3(1f, 1f, 0f);
       }
       ShowShield(Heart[line[losing]], false);
   }

   // D-017-AC-09: the shields in the order they go out, from the one beside +N (the leftmost), whatever order the
   // scene lists them in.
   private int[] ShieldLine()
   {
       if (shieldLine == null || shieldLine.Length != Heart.Count)
       {
           shieldLine = new int[Heart.Count];
           for (var i = 0; i < shieldLine.Length; i++)
               shieldLine[i] = i;
           System.Array.Sort(shieldLine, (a, b) => ShieldX(a).CompareTo(ShieldX(b)));
       }
       return shieldLine;
   }

   private float ShieldX(int index) => Heart[index] != null ? Heart[index].transform.position.x : 0f;


}
