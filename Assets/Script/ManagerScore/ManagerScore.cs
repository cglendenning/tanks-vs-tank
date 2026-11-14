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

    // D-032-AC-01: each enemy's kill counts once, however many rounds or colliders report it.
    private readonly TreadShred.Progression.MissionKills kills = new TreadShred.Progression.MissionKills();

    /// <summary>True for an enemy's first reported death; false when it was already counted this frame or earlier.</summary>
    /// <summary>
    /// An enemy was destroyed, whichever shell or kill path did it: the next wave comes first when it is due (D-046-AC-01),
    /// then the victory count drops.
    /// </summary>
    public void EnemyKilled()
    {
        TreadShredWaves.EnemyDestroyed();
        Victory();
    }

    /// <summary>D-046-AC-01: a wave of reinforcements joins the enemies the victory waits on.</summary>
    public void ReinforcementsArrived(int count)
    {
        CountEmnemy += count;
        SumEmnemy += count;
    }

    public bool ClaimKill(GameObject enemy)
    {
        return kills.Claim(enemy.GetInstanceID());
    }

    public GameObject Medel;

    void Start()
    {


        string level = Application.loadedLevelName;
 

        Medel = VictoryofFail.transform.GetChild(14).gameObject;
        // The original game's medal disc sits behind the offer card, half showing (D-013-AC-02): kept hidden.
        Medel.SetActive(false);

        instance = this;
        RelabelFreeRetry();
        // D-033: a mission today, and its active time, count toward real play.
        TreadShredReviewPrompt.MissionStarted();
        SumEmnemy = CountEmnemy;
        // D-046-AC-01: Veteran and Elite bring the map's enemies back in waves.
        TreadShredWaves.Begin(this);
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

        KillScore.text = ScoreRules.ScoreLine(LiveRunScore);
    }

    /// <summary>SCORE: the run so far plus this mission's kills, or the run as the mission ended (D-032-AC-01).</summary>
    public int LiveRunScore => scoreCommitted
        ? lastMission.RunScore
        : TreadShredScores.Run.Live(CurDieEmnemy, TreadShredProgress.Ensure().SelectedTier);

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
    // The victory card's upgrade offer while Overdrive carries over (D-009-AC-03, D-036).
    private UpgradeTrack? upgradeCardTrack;
    private UpgradePurchase cardShop;
    private Text cardPreviewText;
    private Button cardUndoButton;
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

        // D-019: medals are kept per campaign mission (1-120); Recruit keeps the original keys.
        int currentLevel = TreadShredProgress.Ensure().CurrentNumber();

        int T1 = ((int)(CurDieEmnemy * 100)) + ((int)((float)CurDieEmnemy / (float)CurCountbullet) * 300) + (((int)((3 - (float)Mathf.Min(CurHeath, 3)) / 3)) * 1000) + (CurDieEmnemy * 100);
        int T2 = (SumEmnemy * 100) + 1300 + (SumEmnemy * 100);

        int LevelS = (int)(CurDieEmnemy * 100);

        // A won mission banks its kills and its clear into SCORE; a lost one ends the run (D-032-AC-01).
        if (!scoreCommitted)
        {
            scoreCommitted = true;
            lastMission = TreadShredScores.Run.Complete(won, CurDieEmnemy, TreadShredProgress.Ensure().SelectedTier);
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
                TreadShredOverdriveBadge.Hide();
                TreadShredReviewPrompt.MissionEnded(true);



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
                // D-032-AC-01: the clear points show on the final kill, before the result screen opens over them.
                TreadShredImpact.MissionClear(ScoreRules.ClearValue(TreadShredProgress.Ensure().SelectedTier));
                TreadShredFirstMinute.MissionWon(TreadShredProgress.CurrentMission());
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
        // D-019-AC-05: the first clear of Mission 40 or 80 plays the promotion over the victory screen; its march stands
        // in for the victory music, and the Starter Pack waits until it ends.
        var promotion = FailureObj != null && FailureObj.activeSelf ? null : TreadShredCampaign.TakePromotion();
        if (promotion == null)
            MusicManager.instance.Victory();
        else
            MusicManager.instance.StopBattleMusic();
        CaculaterScore(true);
        TreadShredImpact.MissionOver();
        VictoryofFail.SetActive(true);
        TreadShredWeaponToggle.Hide();
        TreadShredMissionLabel.Hide();
        if (FailureObj != null && FailureObj.activeSelf)
            BuildFailureRewardActions();
        else
        {
            BuildVictoryRewardActions();
            HighlightNextMission();
        }
        // D-037-AC-04: while the Starter Pack is on sale its interstitial opens over every result screen.
        if (promotion is DifficultyTier tier)
            TreadShredPromotion.Show(tier, TreadShredUi.FindFont(VictoryofFail), () => TreadShredStarterOffer.ShowIfOnSale(TreadShredUi.FindFont(VictoryofFail)));
        else
            TreadShredStarterOffer.ShowIfOnSale(TreadShredUi.FindFont(VictoryofFail));
        // D-047-AC-04: a lost mission is where the War Chest is offered, once a session, after the Starter Pack's window.
        if (FailureObj != null && FailureObj.activeSelf)
            TreadShredWarChestOffer.ShowAfterLoss(TreadShredUi.FindFont(VictoryofFail));
    }
    public void Failure()
    {



        if (wf == false)
        {
            TankLost();
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
            TreadShredScores.Run.Resumable > 0
                ? OfferLabels.RedeployKeeps(TreadShredScores.Run.Resumable)
                : CombatBoosts.RedeployExtraHits + " EXTRA HIT. " + SalesPitches.RunContinues,
            "REDEPLOY",
            () => ConfirmThen(GemOffer.ArmoredRedeploy, RedeemArmoredRedeploy));
        ArrangeRetry(false);
    }

    private void BuildVictoryRewardActions()
    {
        // D-009-AC-03, P-8: Overdrive still running from an earlier mission leaves nothing to sell here, so the card offers
        // the cheapest next upgrade instead; with every track maxed it only says Overdrive is on.
        var track = TreadShredBoosts.Current.OverdriveActive
            ? TreadShredProgress.Ensure().Progression.CheapestNextUpgrade()
            : null;
        if (track.HasValue)
            BuildUpgradeOfferPanel(track.Value);
        else
            BuildOfferPanel(
                GemOffer.Overdrive,
                TreadShredFirstMinute.FirstPlay ? OfferLabels.KeepOverdrive : "OVERDRIVE",
                OfferLabels.OverdriveEffect,
                "ACTIVATE",
                () => ConfirmThen(GemOffer.Overdrive, RedeemOverdrive));
        ArrangeRetry(true);
        // D-033-AC-03: only a victory screen may ask for a store rating, after real play.
        TreadShredReviewPrompt.VictoryScreenOpened();
    }

    // D-009-AC-04: after a victory the way forward is the most prominent control on the screen.
    private void HighlightNextMission()
    {
        var plate = VictoryObj != null ? VictoryObj.transform.Find("ButtonNext/LabelPlate") : null;
        var image = plate != null ? plate.GetComponent<Image>() : null;
        if (image == null)
        {
            Debug.LogError("[Results] NEXT MISSION's label plate not found; it keeps its plain style.");
            return;
        }
        image.color = TreadShredUi.PrimaryFill;
        var outline = plate.GetComponent<Outline>();
        if (outline != null)
            outline.effectColor = TreadShredUi.TitleColor;
    }

    private void BuildOfferPanel(GemOffer offer, string title, string detail, string action,
        UnityEngine.Events.UnityAction onTap)
    {
        var rootRect = CreateOfferCard();
        if (rootRect == null)
            return;
        activeOffer = offer;
        activeOfferAction = action;

        // D-034: the boost working, looping beside the offer; tapping it opens the full sales screen.
        TreadShredSalesClip.Create(rootRect, new Vector2(-222f, 0f), new Vector2(284f, 160f),
            SalesPitches.ForOffer(offer), rewardFont, () => OpenSalesScreen(onTap));

        const float column = 150f;
        TreadShredUi.CreateText(title, rootRect, new Vector2(column, 70f), new Vector2(430f, 34f), 26, rewardFont)
            .color = TreadShredUi.TitleColor;
        var detailText = TreadShredUi.CreateText(detail, rootRect, new Vector2(column, 36f), new Vector2(430f, 24f), 17, rewardFont);
        detailText.color = Color.white;
        TreadShredUi.ShrinkToFit(detailText, 12);
        rewardBalanceText = TreadShredUi.CreateText(string.Empty, rootRect, new Vector2(column, 8f),
            new Vector2(430f, 22f), 15, rewardFont);
        rewardBalanceText.color = TreadShredUi.DetailColor;
        TreadShredUi.ShrinkToFit(rewardBalanceText, 11);

        rewardButton = TreadShredUi.CreateButton(action, rootRect, new Vector2(column, -50f), new Vector2(420f, 72f), 22,
            rewardFont, onTap);
        TreadShredUi.ShrinkToFit(rewardButton.GetComponentInChildren<Text>(), 14);
        if (offer == GemOffer.Overdrive && TreadShredBoosts.Current.OverdriveActive)
        {
            // Still running from an earlier mission (D-009-AC-03): say so instead of selling it again.
            rewardUsedThisScreen = true;
            ShowOverdriveActive(0);
        }
        else
        {
            RefreshOfferControls(null);
            TreadShredAnalytics.Log(GameEvents.OfferShown(OfferName(offer), TreadShredStore.Ensure().Wallet.Balance));
        }

        BuildResultExtras();
    }

    // The result screen's one offer card (D-009, D-034); null when it is already built or there is no result screen.
    private RectTransform CreateOfferCard()
    {
        if (rewardedActionPanel != null || VictoryofFail == null)
            return null;

        rewardUsedThisScreen = false;
        rewardFont = TreadShredUi.FindFont(VictoryofFail);
        var rootRect = TreadShredUi.CreateRect("TreadShredOffer", (RectTransform)VictoryofFail.transform,
            new Vector2(0f, -18f), new Vector2(760f, 196f),
            typeof(RectTransform), typeof(Image), typeof(Outline));
        rewardedActionPanel = rootRect.gameObject;
        TreadShredUi.StylePanel(rewardedActionPanel);
        return rootRect;
    }

    // D-009-AC-03, D-036: the cheapest next upgrade on the victory card, with D-036's one price and rules: coins apply on the
    // tap (UNDO on the card reverses it), gems the player holds are confirmed once, money goes straight to the store sheet.
    private void BuildUpgradeOfferPanel(UpgradeTrack track)
    {
        var rootRect = CreateOfferCard();
        if (rootRect == null)
            return;
        upgradeCardTrack = track;
        var store = TreadShredStore.Ensure();
        var progression = TreadShredProgress.Ensure().Progression;
        cardShop = new UpgradePurchase(progression, store.Wallet, store.Purchases,
            (upgraded, quote) => RefreshUpgradeCard(ProgressionLabels.Track(upgraded) + " UPGRADED TO LEVEL " + progression.Level(upgraded)),
            RefreshOfferControls);

        TreadShredSalesClip.Create(rootRect, new Vector2(-222f, 0f), new Vector2(284f, 160f),
            SalesPitches.ForTrack(track), rewardFont, OpenUpgradeSalesScreen);

        const float column = 150f;
        TreadShredUi.CreateText("UPGRADE " + ProgressionLabels.Track(track), rootRect, new Vector2(column, 70f),
            new Vector2(430f, 34f), 26, rewardFont).color = TreadShredUi.TitleColor;
        cardPreviewText = TreadShredUi.CreateText(string.Empty, rootRect, new Vector2(column, 36f), new Vector2(430f, 24f), 17, rewardFont);
        cardPreviewText.color = Color.white;
        TreadShredUi.ShrinkToFit(cardPreviewText, 12);
        rewardBalanceText = TreadShredUi.CreateText(string.Empty, rootRect, new Vector2(column, 8f), new Vector2(430f, 22f), 15, rewardFont);
        TreadShredUi.ShrinkToFit(rewardBalanceText, 11);

        rewardButton = TreadShredUi.CreateButton(string.Empty, rootRect, new Vector2(90f, -50f), new Vector2(300f, 72f), 22,
            rewardFont, UpgradeFromCard);
        TreadShredUi.ShrinkToFit(rewardButton.GetComponentInChildren<Text>(), 14);
        cardUndoButton = TreadShredUi.CreateButton(ProgressionLabels.UndoShort, rootRect, new Vector2(312f, -50f),
            new Vector2(120f, 72f), 22, rewardFont, UndoCardUpgrade);
        progression.Changed += RefreshCardOnProgress;
        RefreshUpgradeCard(null);
        TreadShredAnalytics.Log(GameEvents.OfferShown("upgrade_" + track.ToString().ToLowerInvariant(), store.Wallet.Balance));
        BuildResultExtras();
    }

    private void UpgradeFromCard()
    {
        var track = upgradeCardTrack.Value;
        var progression = TreadShredProgress.Ensure().Progression;
        var quote = progression.Quote(track);
        var balance = TreadShredStore.Ensure().Wallet.Balance;
        if (UpgradePurchase.Checkout(quote, balance) != UpgradeCheckout.ConfirmGems)
        {
            cardShop.Buy(track);
            return;
        }
        TreadShredConfirmDialog.Show(ProgressionLabels.UpgradeConfirmation(track, progression.NextLevel(track), quote, balance),
            rewardFont, () => cardShop.Buy(track));
    }

    private void UndoCardUpgrade()
    {
        var receipt = cardShop.LastUpgrade;
        RefreshUpgradeCard(receipt.HasValue && cardShop.Undo() ? ProgressionLabels.Undone(receipt.Value) : null);
    }

    private void RefreshCardOnProgress() => RefreshUpgradeCard(null);

    // Pass a status to replace the Overdrive line, or null to show it.
    private void RefreshUpgradeCard(string status)
    {
        if (rewardButton == null || !upgradeCardTrack.HasValue)
            return;
        var track = upgradeCardTrack.Value;
        var progression = TreadShredProgress.Ensure().Progression;
        var store = TreadShredStore.Ensure();
        // D-045: nothing is ever maxed; armor and a track at level 10 sell supply.
        TreadShredUi.SetLabel(rewardButton, TreadShredUpgradePanel.PricedLabel(track, false));
        rewardButton.interactable = !store.PurchaseInProgress;
        cardPreviewText.text = ProgressionLabels.Preview(track, progression.Level(track));
        rewardBalanceText.text = status ?? OfferLabels.OverdriveStatus(0);
        rewardBalanceText.color = status == null ? TreadShredUi.TitleColor : TreadShredUi.DetailColor;
        cardUndoButton.gameObject.SetActive(cardShop.CanUndo);
    }

    // D-034: the upgrade's sales screen sells with the card's own button.
    private void OpenUpgradeSalesScreen()
    {
        var track = upgradeCardTrack.Value;
        TreadShredSalesScreen.Show(SalesPitches.ForTrack(track), rewardFont, rewardButton.GetComponentInChildren<Text>().text,
            cardPreviewText.text, rewardButton.interactable, UpgradeFromCard);
    }

    // What every result screen shows around its offer card.
    private void BuildResultExtras()
    {
        // Coins, milestone gems and tier unlocks earned by this mission (D-018, D-026, D-019).
        var reward = TreadShredProgress.Ensure().LastReward;
        if (reward.HasValue)
            // Currency, labelled apart from SCORE.
            TreadShredUi.CreateText("EARNED: " + ProgressionLabels.Reward(reward.Value), (RectTransform)VictoryofFail.transform,
                new Vector2(0f, 104f), new Vector2(900f, 32f), 22, rewardFont).color = TreadShredUi.TitleColor;

        // D-017-AC-07: upgrade between missions without leaving the mission or the run; what is bought applies
        // from the next mission, retry or redeploy. Gems spent there refresh the offer's balance line.
        TreadShredUi.CreateButton(ProgressionLabels.UpgradesButton, (RectTransform)VictoryofFail.transform,
            new Vector2(-530f, -18f), new Vector2(280f, 80f), 24, rewardFont, () => TreadShredUpgradePanel.Show(rewardFont));
        TreadShredStore.Ensure().Wallet.BalanceChanged += OnBalanceChanged;

        // Where the player stands right after the mission (D-032-AC-02): the rank rides on the button once reported.
        if (TreadShredLeaderboard.Available)
        {
            var board = TreadShredUi.CreateButton(LeaderboardRules.Title, (RectTransform)VictoryofFail.transform,
                new Vector2(530f, -18f), new Vector2(280f, 80f), 24, rewardFont,
                () => TreadShredLeaderboardPanel.OpenFrom(rewardFont));
            TreadShredUi.ShrinkToFit(board.GetComponentInChildren<Text>(), 16);
            TreadShredLeaderboard.RequestRank(rank =>
            {
                if (board != null)
                    TreadShredUi.SetLabel(board, LeaderboardRules.ButtonLabel(rank));
            });
        }

        // SCORE and HIGH SCORE (D-032-AC-01), kept to one line above what the mission earned.
        var results = (RectTransform)VictoryofFail.transform;
        TreadShredUi.CreateText(ScoreRules.ScoreLine(lastMission.RunScore), results, new Vector2(-240f, 150f),
            new Vector2(460f, 32f), 22, rewardFont).color = Color.white;
        TreadShredUi.CreateText(ScoreRules.HighScoreLine(TreadShredScores.HighScore, newHighScore), results,
            new Vector2(240f, 150f), new Vector2(460f, 32f), 22, rewardFont).color =
            newHighScore ? TreadShredUi.TitleColor : Color.white;
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

    // The scene's free retry reads RETRY, so only the paid offer is called a redeploy.
    private static void RelabelFreeRetry()
    {
        foreach (var label in FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (label.text == OfferLabels.SceneRetryLabel)
                label.text = OfferLabels.Retry;
        }
    }

    // The tank is lost: the mission counts toward real play as a loss (D-033). Overdrive supply stays (D-045).
    private static void TankLost()
    {
        TreadShredReviewPrompt.MissionEnded(false);
        TreadShredOverdriveBadge.Hide();
    }

    // D-032-AC-06: a retry ends the SCORE run, so the victory screen offers none (COMMAND and NEXT MISSION share
    // the bar), and the failure screen says what RETRY costs.
    private void ArrangeRetry(bool won)
    {
        var retry = VictoryofFail.transform.Find("ButtonRetry") as RectTransform;
        if (retry == null)
        {
            Debug.LogWarning("[Results] ButtonRetry not found; retry left as the scene has it.");
            return;
        }
        if (won)
        {
            retry.gameObject.SetActive(false);
            PlaceAcross(VictoryofFail.transform.Find("ButtonMenu"), 1f / 3f);
            PlaceAcross(VictoryObj.transform.Find("ButtonNext"), 2f / 3f);
            HideDividers(VictoryObj.transform.Find("CommandStrip"));
            return;
        }
        // Above the RETRY icon, in the open space under the offer: legible, and read before tapping.
        var caption = TreadShredUi.CreateText(OfferLabels.RetryCaptionFor(TreadShredScores.Run.Resumable), retry,
            new Vector2(0f, 104f), new Vector2(460f, 36f), 26, rewardFont);
        caption.color = TreadShredUi.TitleColor;
        caption.raycastTarget = false;
    }

    private static void PlaceAcross(Transform button, float x)
    {
        if (!(button is RectTransform rect))
            return;
        rect.anchorMin = new Vector2(x, rect.anchorMin.y);
        rect.anchorMax = new Vector2(x, rect.anchorMax.y);
    }

    // The bar's thin dividers split it into three slots; with two commands they would cut through them.
    private static void HideDividers(Transform strip)
    {
        if (strip == null)
            return;
        foreach (RectTransform part in strip)
        {
            if (part.rect.width <= 12f && part.rect.height > part.rect.width * 4f)
                part.gameObject.SetActive(false);
        }
    }

    private void OnBalanceChanged(int balance) => RefreshOfferControls(null);

    private void OnDestroy()
    {
        if (TreadShredStore.Instance != null)
            TreadShredStore.Instance.Wallet.BalanceChanged -= OnBalanceChanged;
        if (cardShop == null)
            return;
        // Upgrades become final when the screen closes; only those kept are reported (as on the upgrade screen).
        foreach (var receipt in cardShop.Kept)
            TreadShredUpgradePanel.LogPurchase(receipt);
        cardShop.Release();
        if (TreadShredProgress.Instance != null)
            TreadShredProgress.Instance.Progression.Changed -= RefreshCardOnProgress;
    }

    // D-034: the sales screen's button is this offer's own, with the same label and the same purchase.
    private void OpenSalesScreen(UnityEngine.Events.UnityAction onTap)
    {
        TreadShredSalesScreen.Show(SalesPitches.ForOffer(activeOffer), rewardFont,
            rewardButton.GetComponentInChildren<Text>().text, rewardBalanceText.text,
            rewardButton.interactable && !rewardUsedThisScreen, onTap);
    }

    // The offer's button, for the store capture mode's confirmation shots.
    internal Button OfferButton => rewardButton;

    // Pass a status to replace the balance line, or null to show the balance.
    private void RefreshOfferControls(string status)
    {
        if (upgradeCardTrack.HasValue)
        {
            RefreshUpgradeCard(status);
            return;
        }
        // Once used, the panel keeps saying what was bought.
        if (rewardButton == null || rewardUsedThisScreen)
            return;

        var store = TreadShredStore.Ensure();
        var plan = store.PlanFor(activeOffer);
        var price = plan.CanSpendNow ? null : store.PriceFor(plan.PackToBuy);
        TreadShredUi.SetLabel(rewardButton, OfferLabels.ActionButton(activeOfferAction, plan, price));
        rewardBalanceText.text = status ?? OfferLabels.BalanceLine(store.Wallet.Balance, plan);
        rewardButton.interactable = !store.PurchaseInProgress;
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
            TreadShredProgress.Ensure().Progression.AddSupply(TreadShred.Progression.SupplyTrack.Overdrive);
            ShowOverdriveActive(GemCatalog.Cost(GemOffer.Overdrive));
        }, RefreshOfferControls);
    }

    // D-009-AC-03, P-8: a status where the button was, never a button that sells nothing.
    private void ShowOverdriveActive(int gemsSpent)
    {
        rewardButton.interactable = false;
        rewardButton.gameObject.SetActive(false);
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        var line = rewardBalanceText.rectTransform;
        line.anchoredPosition = new Vector2(150f, -46f);
        line.sizeDelta = new Vector2(430f, 60f);
        rewardBalanceText.fontSize = 24;
        rewardBalanceText.text = OfferLabels.OverdriveStatus(gemsSpent);
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
