using UnityEngine;
using System.Collections;
using TreadShred.Economy;
using TreadShred.Progression;
using TreadShred.Analytics;
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
        LogMissionStart();
        if (!activeRunStarted)
        {
            activeRunStarted = true;
            activeRunScore = 0;
        }
        campaignScore = activeRunScore;
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
        if (KillScore == null)
            return;

        campaignScore = activeRunScore;
        var liveMissionScore = scoreCommitted ? 0 : CurDieEmnemy * 100;
        KillScore.text = "SCORE // " + (campaignScore + liveMissionScore).ToString();
    }

    public float test;
    int indexmedel;
    float hightscore;
    private GameObject rewardedActionPanel;
    private bool rewardUsedThisScreen;
    private GemOffer activeOffer;
    private string activeOfferAction;
    private Text rewardBalanceText;
    private Button rewardButton;
    private Font rewardFont;
    private static int activeRunScore;
    private static bool activeRunStarted;
    private int campaignScore;
    private bool scoreCommitted;

    public static void BeginNewRun()
    {
        activeRunStarted = true;
        activeRunScore = 0;
        PlayerPrefs.DeleteKey("TreadShredCampaignScore");
        PlayerPrefs.Save();
    }

    public static void EndRun()
    {
        activeRunStarted = false;
        activeRunScore = 0;
        PlayerPrefs.DeleteKey("TreadShredCampaignScore");
        PlayerPrefs.Save();
    }


    public void CaculaterScore()
    {

        string level = Application.loadedLevelName;
        int currentLevel = int.Parse(level.Substring(2));

        int T1 = ((int)(CurDieEmnemy * 100)) + ((int)((float)CurDieEmnemy / (float)CurCountbullet) * 300) + (((int)((3 - (float)Mathf.Min(CurHeath, 3)) / 3)) * 1000) + (CurDieEmnemy * 100);
        int T2 = (SumEmnemy * 100) + 1300 + (SumEmnemy * 100);

        int LevelS = (int)(CurDieEmnemy * 100);

        var missionWon = FailureObj == null || !FailureObj.activeSelf;
        if (!scoreCommitted && missionWon)
        {
            activeRunScore += LevelS;
            campaignScore = activeRunScore;
            scoreCommitted = true;
        }

        int AccurasyR = (CurCountbullet > 0) ? (int)(((float)CurDieEmnemy / (float)CurCountbullet) * 300) : 0;

        int HealthR = (int)(((3 - (float)Mathf.Min(CurHeath, 3)) / 3) * 1000);
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
                LogMissionEnd(TreadShredProgress.Ensure().CompleteCurrentMission(true), true);
                VictoryObj.SetActive(true);
                StartCoroutine(DelayVictotory());

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
        else
            BuildVictoryRewardActions();
    }
    public void Failure()
    {



        if (wf == false)
        {
            wf = true;
            LogMissionEnd(TreadShredProgress.Ensure().CompleteCurrentMission(false), false);
            BeginNewRun();
            campaignScore = 0;
            FailureObj.SetActive(true);
            StartCoroutine(DelayVictotory());
            Debug.Log("thua");
        }
    }


    private void BuildFailureRewardActions()
    {
        BuildOfferPanel(
            GemOffer.ArmoredRedeploy,
            "ARMORED REDEPLOY",
            "REPLAY THIS MISSION WITH 1 EXTRA HIT",
            "REDEPLOY",
            RedeemArmoredRedeploy);
    }

    private void BuildVictoryRewardActions()
    {
        BuildOfferPanel(
            GemOffer.Overdrive,
            "OVERDRIVE",
            "NEXT MISSION: 2 SHOTS PER TAP",
            "ACTIVATE",
            RedeemOverdrive);
    }

    private void BuildOfferPanel(GemOffer offer, string title, string detail, string action,
        UnityEngine.Events.UnityAction onTap)
    {
        if (rewardedActionPanel != null || VictoryofFail == null)
            return;

        rewardUsedThisScreen = false;
        rewardFont = TreadShredUi.FindFont(VictoryofFail);
        activeOffer = offer;
        activeOfferAction = action;

        var rootRect = TreadShredUi.CreateRect("TreadShredOffer", (RectTransform)VictoryofFail.transform,
            new Vector2(0f, -18f), new Vector2(720f, 196f),
            typeof(RectTransform), typeof(Image), typeof(Outline));
        rewardedActionPanel = rootRect.gameObject;
        TreadShredUi.StylePanel(rewardedActionPanel);

        TreadShredUi.CreateText(title, rootRect, new Vector2(0f, 70f), new Vector2(700f, 34f), 26, rewardFont)
            .color = TreadShredUi.TitleColor;
        TreadShredUi.CreateText(detail, rootRect, new Vector2(0f, 36f), new Vector2(700f, 24f), 17, rewardFont)
            .color = Color.white;
        rewardBalanceText = TreadShredUi.CreateText(string.Empty, rootRect, new Vector2(0f, 8f),
            new Vector2(700f, 22f), 15, rewardFont);
        rewardBalanceText.color = TreadShredUi.DetailColor;

        rewardButton = TreadShredUi.CreateButton(action, rootRect, new Vector2(0f, -50f), new Vector2(520f, 72f), 22,
            rewardFont, onTap);
        RefreshOfferControls(null);
        TreadShredAnalytics.Log(GameEvents.OfferShown(OfferName(offer), TreadShredStore.Ensure().Wallet.Balance));

        // Coins, milestone gems and tier unlocks earned by this mission (D-018, D-026, D-019).
        var reward = TreadShredProgress.Ensure().LastReward;
        if (reward.HasValue)
            TreadShredUi.CreateText(ProgressionLabels.Reward(reward.Value), (RectTransform)VictoryofFail.transform,
                new Vector2(0f, 104f), new Vector2(900f, 32f), 22, rewardFont).color = TreadShredUi.TitleColor;
    }

    // Pass a status to replace the balance line, or null to show the balance.
    private void RefreshOfferControls(string status)
    {
        if (rewardButton == null)
            return;

        var store = TreadShredStore.Ensure();
        var plan = store.PlanFor(activeOffer);
        var price = plan.CanSpendNow ? null : store.PriceFor(plan.PackToBuy);
        TreadShredUi.SetLabel(rewardButton, OfferLabels.ActionButton(activeOfferAction, plan, price));
        rewardBalanceText.text = status ?? OfferLabels.BalanceLine(store.Wallet.Balance, plan);
    }

    private void RedeemArmoredRedeploy()
    {
        if (rewardUsedThisScreen)
            return;

        var packBought = !TreadShredStore.Ensure().PlanFor(GemOffer.ArmoredRedeploy).CanSpendNow;
        TreadShredStore.Ensure().RedeemOffer(GemOffer.ArmoredRedeploy, () =>
        {
            rewardUsedThisScreen = true;
            TreadShredAnalytics.Log(GameEvents.OfferAccepted(OfferName(GemOffer.ArmoredRedeploy), GemCatalog.Cost(GemOffer.ArmoredRedeploy), packBought));
            PlayerPrefs.SetInt("TreadShredArmorCache", 1);
            PlayerPrefs.Save();
            ReloadCurrentMission();
        }, RefreshOfferControls);
    }

    private void RedeemOverdrive()
    {
        if (rewardUsedThisScreen)
            return;

        var packBought = !TreadShredStore.Ensure().PlanFor(GemOffer.Overdrive).CanSpendNow;
        TreadShredStore.Ensure().RedeemOffer(GemOffer.Overdrive, () =>
        {
            rewardUsedThisScreen = true;
            TreadShredAnalytics.Log(GameEvents.OfferAccepted(OfferName(GemOffer.Overdrive), GemCatalog.Cost(GemOffer.Overdrive), packBought));
            PlayerPrefs.SetInt("TreadShredOverdrive", 1);
            PlayerPrefs.Save();
            rewardButton.interactable = false;
            RefreshOfferControls("OVERDRIVE READY FOR YOUR NEXT MISSION");
        }, RefreshOfferControls);
    }

    private static string OfferName(GemOffer offer)
    {
        return offer == GemOffer.ArmoredRedeploy ? "armored_redeploy" : "overdrive";
    }

    // D-021: mission analytics.
    private static void LogMissionStart()
    {
        var mission = TreadShredProgress.CurrentMission();
        if (mission > 0)
            TreadShredAnalytics.Log(GameEvents.MissionStart(mission, TreadShredProgress.Ensure().SelectedTier.ToString().ToLowerInvariant()));
    }

    private static void LogMissionEnd(MissionReward? reward, bool won)
    {
        var mission = TreadShredProgress.CurrentMission();
        if (mission > 0)
            TreadShredAnalytics.Log(GameEvents.MissionEnd(mission, TreadShredProgress.Ensure().SelectedTier.ToString().ToLowerInvariant(),
                won, Time.timeSinceLevelLoad, reward.HasValue ? reward.Value.Coins : 0));
    }

    private void ReloadCurrentMission()
    {
        BeginNewRun();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public bool isrung;

    public void Rung()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (isrung == false)
        {
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                Handheld.Vibrate();
            }
        }
#endif


    }



}
