using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ManagerScore : MonoBehaviour
{

    public static ManagerScore instance;

    public GameObject VictoryofFail;
    public GameObject VictoryObj;
    public GameObject FailureObj;
    public GameObject CanvasGame;

    public Text LevelScore;
    public Text AccurasyRating;
    public Text HealthRating;
    public Text KillRating;
    public Text Total;

    public Text KillScore;

    int SumEmnemy = 1;

    public int CurDieEmnemy;
    public int CurCountbullet;
    public int CurHeath;

    public int CountEmnemy;

    public GameObject Medel;

    void Start()
    {


        string level = Application.loadedLevelName;
 

        Medel = VictoryofFail.transform.GetChild(14).gameObject;

        instance = this;
        SumEmnemy = CountEmnemy;
        if (CanvasGame == null)
        {
            CanvasGame = GameObject.Find("Canvas");
        }
        KillScore = CanvasGame.transform.GetChild(5).gameObject.transform.GetChild(0).gameObject.GetComponent<Text>();
        if (PlayerPrefs.HasKey("OpenLv") == false)
        {
            PlayerPrefs.SetFloat("OpenLv", 0);
            PlayerPrefs.Save();
        }

        if (PlayerPrefs.HasKey("CountKill") == false)
        {
            PlayerPrefs.SetFloat("CountKill", 0);
            PlayerPrefs.Save();
        }

        if (PlayerPrefs.GetFloat("Vibrate") == 0)
        {
            isrung = false;
        }
        else
        {
            isrung = true;
        }
    }


    void Update()
    {
        KillScore.text = "SCORE // " + (CurDieEmnemy * 100).ToString();
    }

    public float test;
    int indexmedel;
    float hightscore;
    private GameObject rewardedActionPanel;
    private bool rewardUsedThisFailure;
    private Font rewardFont;


    public void CaculaterScore()
    {

        string level = Application.loadedLevelName;
        int currentLevel = int.Parse(level.Substring(2));

        int T1 = ((int)(CurDieEmnemy * 100)) + ((int)((float)CurDieEmnemy / (float)CurCountbullet) * 300) + (((int)((3 - (float)CurHeath) / 3)) * 1000) + (CurDieEmnemy * 100);
        int T2 = (SumEmnemy * 100) + 1300 + (SumEmnemy * 100);

        int LevelS = (int)(CurDieEmnemy * 100);

        int AccurasyR = (CurCountbullet > 0) ? (int)(((float)CurDieEmnemy / (float)CurCountbullet) * 300) : 0;

        int HealthR = (int)(((3 - (float)CurHeath) / 3) * 1000);
        int KillR = CurDieEmnemy * 100;
        int SumForMedel = AccurasyR + HealthR;
        if (CountEmnemy < 1)
        {
            if (SumForMedel >= 1200)
            {
                Medel.transform.GetChild(3).gameObject.SetActive(true);
                indexmedel = 3;
            }
            else if (SumForMedel >= 866)
            {
                Medel.transform.GetChild(2).gameObject.SetActive(true);
                indexmedel = 2;
            }
            else
            {
                Medel.transform.GetChild(1).gameObject.SetActive(true);
                indexmedel = 1;
            }


            if (indexmedel > PlayerPrefs.GetFloat("Lv" + currentLevel.ToString()))
            {
                PlayerPrefs.SetFloat("Lv" + currentLevel.ToString(), indexmedel);
                PlayerPrefs.Save();
            }


        }


        if (SumForMedel > PlayerPrefs.GetFloat("Lvs" + currentLevel.ToString()))
        {
            PlayerPrefs.SetFloat("Lvs" + currentLevel.ToString(), SumForMedel);
            PlayerPrefs.Save();
        }


        StartCoroutine(DelayScore(LevelS));
        StartCoroutine(DelayAccurasyR(AccurasyR));
        StartCoroutine(DelayKillRating(KillR, T1, T2));
        StartCoroutine(DelayHealthRating(HealthR));


    }

    IEnumerator DelayScore(int LevelS)
    {
        int indexLevel = (int)(LevelS / 10);

        for (int i = 0; i < 10; i++)
        {

            yield return new WaitForSeconds(0.1f);
            LevelScore.text = (indexLevel * i).ToString() + " / " + (SumEmnemy * 100).ToString();

        }
        LevelScore.text = LevelS.ToString() + " / " + (SumEmnemy * 100).ToString();

    }

    IEnumerator DelayAccurasyR(int AccurasyR)
    {
        int indexLevel = (int)(AccurasyR / 10);

        for (int i = 0; i < 10; i++)
        {

            yield return new WaitForSeconds(0.1f);
            AccurasyRating.text = (indexLevel * i).ToString() + " / " + "300";

        }

        AccurasyRating.text = AccurasyR.ToString() + " / " + "300";

    }


    IEnumerator DelayHealthRating(int HealthR)
    {
        int indexLevel = (int)(HealthR / 10);

        for (int i = 0; i < 10; i++)
        {

            yield return new WaitForSeconds(0.1f);
            HealthRating.text = (indexLevel * i).ToString() + " / " + "1000";

        }
        HealthRating.text = HealthR.ToString() + " / " + "1000";

    }

    IEnumerator DelayKillRating(int KillR, int T1, int T2)
    {
        int indexLevel = (int)(KillR / 10);

        for (int i = 0; i < 10; i++)
        {

            yield return new WaitForSeconds(0.1f);
            KillRating.text = (indexLevel * i).ToString() + " / " + "1000";

        }
        KillRating.text = KillR.ToString() + " / " + (SumEmnemy * 100).ToString();

        Total.text = T1.ToString() + " / " + T2.ToString();
    }

    bool wf;
    public void Victory()
    {


        CountEmnemy--;
        if (CountEmnemy < 1)
        {
            if (wf == false)
            {



                string level = Application.loadedLevelName;
                level = level.Substring(2);
                // Debug.Log(int.Parse(level));
                //  Debug.Log("Open"+PlayerPrefs.GetFloat("OpenLv"));
                if ((int.Parse(level) > PlayerPrefs.GetFloat("OpenLv")))
                {

                    PlayerPrefs.SetFloat("OpenLv", int.Parse(level));
                    //   Debug.Log(PlayerPrefs.GetFloat("OpenLv"));
                }

                wf = true;
                VictoryObj.SetActive(true);
                StartCoroutine(DelayVictotory());
                StartCoroutine(DelayShowAD(false));

                Debug.Log("thắng");


            }
        }

    }

    public IEnumerator DelayVictotory()
    {


        yield return new WaitForSeconds(2f);
        PlayerMoverment.instance.isstop = true;
        CanvasGame.transform.GetChild(2).gameObject.SetActive(false);
        CanvasGame.transform.GetChild(10).gameObject.SetActive(false);
        MusicManager.instance.Victory();
        CaculaterScore();
        VictoryofFail.SetActive(true);
        if (FailureObj != null && FailureObj.activeSelf)
            BuildFailureRewardActions();
        //if (Showadsmob.instance.isadsmob)
        //{
        //      Showadsmob.instance.isadsmob = false;
        //      Showadsmob.instance.timecount = 0f;
        //      GoogleMobileAdsDemoScript.Instance.ShowInterstitial();
        //}


    }
    public IEnumerator DelayShowAD(bool islost)
    {
        yield return new WaitForSeconds(1.5f);
        if (islost)
        {
            if (GoogleMobileAdsDemoScript.Instance.showOnLost)
            {
                GoogleMobileAdsDemoScript.Instance.ShowInterstitial();
            }
        }
        else
        {
            if (GoogleMobileAdsDemoScript.Instance.showOnWin)
            {
                GoogleMobileAdsDemoScript.Instance.ShowInterstitial();
            }
        }
    }
    public void Failure()
    {



        if (wf == false)
        {
            wf = true;
            FailureObj.SetActive(true);
            StartCoroutine(DelayVictotory());
            StartCoroutine(DelayShowAD(true));
            Debug.Log("thua");
        }
    }


    private void BuildFailureRewardActions()
    {
        if (rewardedActionPanel != null || VictoryofFail == null)
            return;

        rewardUsedThisFailure = false;
        var existingTexts = VictoryofFail.GetComponentsInChildren<Text>(true);
        for (var i = 0; i < existingTexts.Length; i++)
        {
            if (existingTexts[i].font != null)
            {
                rewardFont = existingTexts[i].font;
                break;
            }
        }
        rewardedActionPanel = new GameObject("TreadShredRewardActions", typeof(RectTransform));
        rewardedActionPanel.transform.SetParent(VictoryofFail.transform, false);
        var rootRect = rewardedActionPanel.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = new Vector2(0f, -130f);
        rootRect.sizeDelta = new Vector2(680f, 164f);

        var title = CreateRewardText(
            "ARMORED REDEPLOY // +1 HIT",
            rootRect,
            new Vector2(0f, 48f),
            new Vector2(680f, 34f),
            24);
        title.color = new Color(1f, 0.72f, 0.28f, 1f);

        var detail = CreateRewardText(
            "REPLAY IS FREE // WATCH FOR AN EXTRA HIT",
            rootRect,
            new Vector2(0f, 18f),
            new Vector2(680f, 24f),
            15);
        detail.color = new Color(0.78f, 0.86f, 0.88f, 1f);

        CreateRewardButton(
            "WATCH AD // ARMORED REDEPLOY",
            rootRect,
            new Vector2(0f, -28f),
            ArmoredRedeployAfterReward);
    }

    private Text CreateRewardText(string value, RectTransform parent, Vector2 position, Vector2 size, int fontSize)
    {
        var textObject = new GameObject("RewardLabel", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = rewardFont != null ? rewardFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateRewardButton(string label, RectTransform parent, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(480f, 72f);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.14f, 0.16f, 0.98f);
        var outline = buttonObject.GetComponent<Outline>();
        outline.effectColor = new Color(1f, 0.36f, 0.08f, 1f);
        outline.effectDistance = new Vector2(3f, 3f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.14f, 0.16f, 0.98f);
        colors.highlightedColor = new Color(0.18f, 0.32f, 0.35f, 1f);
        colors.pressedColor = new Color(0.4f, 0.16f, 0.06f, 1f);
        button.colors = colors;
        button.onClick.AddListener(action);

        var text = CreateRewardText(label, rect, Vector2.zero, rect.sizeDelta - new Vector2(18f, 10f), 22);
        text.color = Color.white;
        return button;
    }

    private void ArmoredRedeployAfterReward()
    {
        if (rewardUsedThisFailure)
            return;

        if (TankAdService.Ensure().TryShowRewarded(() =>
        {
            PlayerPrefs.SetInt("TreadShredArmorCache", 1);
            PlayerPrefs.Save();
            ReloadCurrentMission();
        }))
            rewardUsedThisFailure = true;
    }

    private void ReloadCurrentMission()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public bool isrung;

    public void Rung()
    {
        if (isrung == false)
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                Handheld.Vibrate();
            }
        }


    }



}
