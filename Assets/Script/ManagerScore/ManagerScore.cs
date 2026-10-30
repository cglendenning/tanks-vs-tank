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

        // SCORE: the run so far plus this mission's kills, or the run as the mission ended (D-032-AC-01).
        KillScore.text = ScoreRules.ScoreLine(scoreCommitted ? lastMission.RunScore : TreadShredScores.Run.Live(CurDieEmnemy));
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
    private bool scoreCommitted;
    // How this mission ended for SCORE, and whether it set a new HIGH SCORE (D-032-AC-01).
    private MissionScore lastMission;
    private bool newHighScore;

    public static void BeginNewRun()
    {
        TreadShredScores.Run.Start();
    }

    public static void EndRun()
    {
        TreadShredScores.Run.Start();
    }


    public void CaculaterScore(bool won)
    {

        string level = Application.loadedLevelName;
        int currentLevel = int.Parse(level.Substring(2));

        int T1 = ((int)(CurDieEmnemy * 100)) + ((int)((float)CurDieEmnemy / (float)CurCountbullet) * 300) + (((int)((3 - (float)Mathf.Min(CurHeath, 3)) / 3)) * 1000) + (CurDieEmnemy * 100);
        int T2 = (SumEmnemy * 100) + 1300 + (SumEmnemy * 100);

        int LevelS = (int)(CurDieEmnemy * 100);

        // A won mission banks kills and bonuses into SCORE; a lost one ends the run (D-032-AC-01).
        if (!scoreCommitted)
        {
            scoreCommitted = true;
            lastMission = TreadShredScores.Run.Complete(won, CurDieEmnemy, CurCountbullet, CurHeath);
            newHighScore = TreadShredScores.RecordScore(lastMission.RunScore);
            TreadShredLeaderboard.SubmitIfRaised();
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
        CaculaterScore(true);
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
            // The tank was lost: SCORE's run ends here unless Armored Redeploy resumes it.
            if (!scoreCommitted)
                CaculaterScore(false);
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
            () => ConfirmThen(GemOffer.ArmoredRedeploy, RedeemArmoredRedeploy));
    }

    private void BuildVictoryRewardActions()
    {
        BuildOfferPanel(
            GemOffer.Overdrive,
            "OVERDRIVE",
            "NEXT MISSION: 2 SHOTS PER TAP",
            "ACTIVATE",
            () => ConfirmThen(GemOffer.Overdrive, RedeemOverdrive));
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

        // Where the player stands right after the mission (D-032-AC-02).
        if (TreadShredLeaderboard.Available)
            TreadShredUi.CreateButton(LeaderboardRules.Title, (RectTransform)VictoryofFail.transform,
                new Vector2(530f, -18f), new Vector2(280f, 80f), 24, rewardFont,
                () => TreadShredLeaderboardPanel.OpenFrom(rewardFont));

        // Where the player stands on the platform leaderboard, once it reports (D-032).
        // SCORE and HIGH SCORE (D-032-AC-01), this mission's bonuses when won, and the rank once reported.
        var results = (RectTransform)VictoryofFail.transform;
        TreadShredUi.CreateText(ScoreRules.ScoreLine(lastMission.RunScore), results, new Vector2(-240f, 172f),
            new Vector2(460f, 32f), 22, rewardFont).color = Color.white;
        TreadShredUi.CreateText(ScoreRules.HighScoreLine(TreadShredScores.HighScore, newHighScore), results,
            new Vector2(240f, 172f), new Vector2(460f, 32f), 22, rewardFont).color =
            newHighScore ? TreadShredUi.TitleColor : Color.white;
        if (lastMission.Won)
            TreadShredUi.CreateText(ScoreRules.BonusLine(lastMission), results, new Vector2(0f, 138f),
                new Vector2(900f, 30f), 20, rewardFont).color = TreadShredUi.DetailColor;
        var rankText = TreadShredUi.CreateText(string.Empty, results, new Vector2(0f, 206f), new Vector2(900f, 32f), 22,
            rewardFont);
        rankText.color = Color.white;
        TreadShredLeaderboard.RequestRank(rank =>
        {
            if (rankText != null)
                rankText.text = LeaderboardRules.RankLine(rank);
        });
    }

    // D-009: gems already held are spent only after the player confirms; when
    // short, the store's purchase sheet confirms and the boost applies on delivery.
    private void ConfirmThen(GemOffer offer, System.Action redeem)
    {
        if (rewardUsedThisScreen)
            return;

        var store = TreadShredStore.Ensure();
        if (!OfferLabels.NeedsConfirmation(store.PlanFor(offer)))
        {
            redeem();
            return;
        }
        TreadShredConfirmDialog.Show(OfferLabels.Confirmation(offer, store.Wallet.Balance), rewardFont, redeem);
    }

    // The offer's button, for the store capture mode's confirmation shots.
    internal Button OfferButton => rewardButton;

    // Pass a status to replace the balance line, or null to show the balance.
    private void RefreshOfferControls(string status)
    {
        // Once used, the panel keeps saying what was bought.
        if (rewardButton == null || rewardUsedThisScreen)
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
            TreadShredBoosts.Current.ArmRedeploy();
            TreadShredScores.Run.ResumeAfterRedeploy();
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
            TreadShredBoosts.Current.ArmOverdrive();
            ShowOverdriveActive();
        }, RefreshOfferControls);
    }

    private void ShowOverdriveActive()
    {
        TreadShredUi.SetLabel(rewardButton, OfferLabels.OverdriveActive);
        // The tint multiplies the image color, so use a white image for a solid accent fill.
        rewardButton.image.color = Color.white;
        var colors = rewardButton.colors;
        colors.disabledColor = TreadShredUi.AccentColor;
        rewardButton.colors = colors;
        rewardButton.interactable = false;
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        rewardBalanceText.text = OfferLabels.OverdriveActiveDetail(GemCatalog.Cost(GemOffer.Overdrive));
        rewardBalanceText.color = TreadShredUi.TitleColor;
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
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public bool isrung;

    // D-027-AC-03: a haptic pulse when Vibration is on and the device can vibrate.
    public void Rung(HapticPulse pulse = HapticPulse.Kill)
    {
        TreadShredHaptics.Pulse(pulse, !isrung);
    }



}
