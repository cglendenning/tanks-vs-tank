using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TreadShred.Progression;

public class Memory : MonoBehaviour {

    public static Memory instance;

    public GameObject GetButtonLev;
    public Text HightScoreTextButton;
    public Text LevelTextButton;
    public Text RankingTextButton;

    public List<GameObject> ListButton = new List<GameObject>();

    public List<GameObject> ListImageWhite = new List<GameObject>();
    public List<GameObject> ListImageBlack = new List<GameObject>();

    public int LevelChoosed;

    private const float LabelInset = 40f;
    private const float IconGap = 20f;
    // D-041: the play arrow in the selected row; 110 units is about 44 points on the smallest supported phone.
    private const float RowPlaySize = 110f;
    private const float RowPlayMargin = 8f;
    private const float RowPlayGap = 12f;

    // The medal or lock sits at the right end of each mission button; the label fills the rest and
    // shrinks to fit, so the icon never covers it and it never runs past the button.
    private static void KeepLabelClearOfMedal(Transform button)
    {
        var label = button.Find("Text") as RectTransform;
        var medal = button.Find("Medel");
        if (label == null || medal == null)
            return;
        var iconLeft = float.MaxValue;
        foreach (RectTransform icon in medal)
            iconLeft = Mathf.Min(iconLeft, icon.anchoredPosition.x - icon.rect.width * icon.pivot.x);
        if (iconLeft == float.MaxValue)
            return;
        // Medel is centred on the button, so icon positions are measured from the button's centre.
        var halfWidth = ((RectTransform)button).rect.width / 2f;
        label.anchorMin = Vector2.zero;
        label.anchorMax = Vector2.one;
        label.offsetMin = new Vector2(LabelInset, 0f);
        label.offsetMax = new Vector2(iconLeft - halfWidth - IconGap, 0f);
    }

    // D-041: every row label on one line at one size: the largest at which the longest label fits beside its medal.
    private void FitRowLabels()
    {
        var labels = new List<Text>();
        foreach (var row in ListButton)
        {
            var label = row.transform.Find("Text");
            if (label != null)
                labels.Add(label.GetComponent<Text>());
        }
        var size = int.MaxValue;
        foreach (var label in labels)
        {
            label.resizeTextForBestFit = false;
            var width = label.preferredWidth;
            var fits = width <= label.rectTransform.rect.width ? label.fontSize
                : Mathf.FloorToInt(label.fontSize * label.rectTransform.rect.width / width * 0.98f);
            size = Mathf.Min(size, fits);
        }
        foreach (var label in labels)
        {
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.fontSize = size;
        }
    }

    // D-041: the play arrow takes the right end of the selected row, so every row's medal or lock sits just left of
    // that slot and the rows line up whichever one is selected.
    private static void MakeRoomForPlay(Transform button)
    {
        var medal = button.Find("Medel");
        if (medal == null)
            return;
        var playLeft = RowPlayCentre(button) - RowPlaySize / 2f;
        foreach (RectTransform icon in medal)
            icon.anchoredPosition = new Vector2(playLeft - RowPlayGap - icon.rect.width * (1f - icon.pivot.x), icon.anchoredPosition.y);
    }

    // Medel and the arrow are centred on the button, so x is measured from the button's centre.
    private static float RowPlayCentre(Transform button) =>
        ((RectTransform)button).rect.width / 2f - RowPlayMargin - RowPlaySize / 2f;

	// Use this for initialization
	void Start () {
     
        instance = this;
        BuildCampaignRows();

        HightScoreTextButton.text = TreadShred.Progression.ScoreRules.HighScoreLine(TreadShredScores.HighScore, false);
        LevelTextButton.text = "MISSION: " + 1.ToString();
        switch ((int)(PlayerPrefs.GetFloat("Lv" + 1.ToString())))
        {

            case 3:
                RankingTextButton.text = "MEDAL: GOLD";
                break;
            case 2:
                RankingTextButton.text = "MEDAL: SILVER";
                break;
            case 1:
                RankingTextButton.text = "MEDAL: BRONZE";
                break;
            case 0:
                RankingTextButton.text = "MEDAL: UNRANKED";
                break;
            default:
                break;
        }

        for (int i = 0; i < ListButton.Count; i++)
        {
            // Gán thứ tự Level
            ListButton[i].gameObject.transform.GetChild(2).gameObject.GetComponent<Text>().text = "MISSION" + " " + (i + 1);

            //Tạo 2 list ảnh trắng đen của Button;

            ListImageWhite.Add(ListButton[i].gameObject.transform.GetChild(0).gameObject);
            ListImageBlack.Add(ListButton[i].gameObject.transform.GetChild(1).gameObject);

            //Kiểm tra xem đã có key lưu medel chưa
            if (PlayerPrefs.HasKey("Lv" + (i + 1).ToString()) == false)
            {
                PlayerPrefs.SetFloat("Lv" + (i + 1).ToString(), 0);
                PlayerPrefs.Save();
            }

            //Kiểm tra xem đã có key lưu score chưa

            if (PlayerPrefs.HasKey("Lvs" + (i + 1).ToString()) == false)
            {
                PlayerPrefs.SetFloat("Lvs" + (i + 1).ToString(), 0);
                PlayerPrefs.Save();
            }     


            // D-019: medals per campaign mission; the lock follows the in-order campaign rule.
            var medal = (int)PlayerPrefs.GetFloat("Lv" + (i + 1));
            var icons = ListButton[i].transform.GetChild(3);
            icons.GetChild(3).gameObject.SetActive(medal == 3);
            icons.GetChild(2).gameObject.SetActive(medal == 2);
            icons.GetChild(1).gameObject.SetActive(medal == 1);
            icons.GetChild(0).gameObject.SetActive(!MissionUnlocked(i + 1));
        }

        // D-041: the play arrow lives in the selected row; the big one at the bottom and its MISSION: n label go.
        var bottomPlay = LevelTextButton.transform.parent.Find("Play");
        rowPlay = Instantiate(bottomPlay.gameObject, ListButton[0].transform).GetComponent<RectTransform>();
        rowPlay.name = "Play";
        rowPlay.anchorMin = rowPlay.anchorMax = rowPlay.pivot = new Vector2(0.5f, 0.5f);
        rowPlay.sizeDelta = new Vector2(RowPlaySize, RowPlaySize);
        rowPlay.anchoredPosition = new Vector2(RowPlayCentre(ListButton[0].transform), 0f);
        rowPlay.localScale = Vector3.one;
        bottomPlay.gameObject.SetActive(false);
        LevelTextButton.gameObject.SetActive(false);
        // After the loop above numbers the rows, so the longest real label sets the size.
        FitRowLabels();
        Select(NextMission());
	}

    public void ChooseLevel(Text levelplay)
    {

        if (AutuScroll.instance.ismove == false)
        {
            AutuScroll.instance.ismove = true;
        }

        var mission = int.Parse(levelplay.text.Substring(8));
        // D-041: a second tap on the selected row plays it, as its arrow does.
        if (mission == indexsence)
        {
            PlayLevel();
            return;
        }
        Select(mission);
    }

    // D-041: selecting a mission lights its row and moves the play arrow into it; a locked mission cannot be selected.
    private void Select(int mission)
    {
        var row = ListButton[mission - 1].transform;
        if (row.Find("Medel/Lock").gameObject.activeSelf)
            return;
        LevelChoosed = indexsence = mission;
        for (int i = 0; i < ListButton.Count; i++)
        {
            ListImageWhite[i].SetActive(i == mission - 1);
            ListImageBlack[i].SetActive(i != mission - 1);
        }
        rowPlay.SetParent(row, false);
        rowPlay.SetAsLastSibling();
    }

    // D-041: the list opens on the mission to play next, of all 120 (D-019).
    private int NextMission()
    {
        var progression = TreadShredProgress.Ensure().Progression;
        return MissionList.NextToPlay(ListButton.Count,
            mission => !ListButton[mission - 1].transform.Find("Medel/Lock").gameObject.activeSelf,
            mission => progression.IsCleared(Campaign.TierOf(mission), Campaign.MapOf(mission)));
    }

    // D-019-AC-01: in order across all 120; a mission cleared before progress was synced still counts by its medal.
    private static bool MissionUnlocked(int number)
    {
        var progression = TreadShredProgress.Ensure().Progression;
        return Campaign.IsUnlocked(number,
            mission => progression.IsCleared(Campaign.TierOf(mission), Campaign.MapOf(mission))
                || PlayerPrefs.GetFloat("Lv" + mission) > 0f,
            progression.IsUnlocked);
    }

    // D-019-AC-03: all 120 missions, two to a row in reading order, under a full-width header for each tier. The scene
    // holds the first 40 rows; the rest are copies of the first.
    private void BuildCampaignRows()
    {
        var grid = GetButtonLev.GetComponent<GridLayoutGroup>();
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        var rows = new List<GameObject>();
        foreach (Transform item in GetButtonLev.transform)
            rows.Add(item.gameObject);
        var template = rows[0];
        while (rows.Count < Campaign.MissionTotal)
            rows.Add(Instantiate(template, GetButtonLev.transform));
        var font = template.transform.GetChild(2).GetComponent<Text>().font;
        var sibling = 0;
        for (var number = 1; number <= Campaign.MissionTotal; number++)
        {
            if (Campaign.MapOf(number) == 1)
            {
                AddHeader(Campaign.TierOf(number), font, grid, sibling);
                sibling += 2;
            }
            var row = rows[number - 1];
            row.transform.SetSiblingIndex(sibling++);
            ListButton.Add(row);
            MakeRoomForPlay(row.transform);
            KeepLabelClearOfMedal(row.transform);
        }
    }

    // A header takes both cells of its grid row: the label spans them, and an empty cell holds the second place.
    private void AddHeader(DifficultyTier tier, Font font, GridLayoutGroup grid, int sibling)
    {
        var parent = (RectTransform)GetButtonLev.transform;
        var cell = TreadShredUi.CreateRect("Header " + Campaign.TierName(tier), parent, Vector2.zero, grid.cellSize, typeof(RectTransform));
        cell.SetSiblingIndex(sibling);
        var text = TreadShredUi.CreateText(Campaign.Header(tier), cell, Vector2.zero,
            new Vector2(grid.cellSize.x * 2f + grid.spacing.x - LabelInset, grid.cellSize.y), 64, font);
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = new Vector2(0f, 0.5f);
        text.rectTransform.anchoredPosition = new Vector2(LabelInset, 0f);
        text.alignment = TextAnchor.MiddleLeft;
        text.color = TreadShredUi.AccentColor;
        text.raycastTarget = false;
        TreadShredUi.CreateRect("Header spacer", parent, Vector2.zero, grid.cellSize, typeof(RectTransform)).SetSiblingIndex(sibling + 1);
    }

    // D-041: the scroll position that brings the selected row to the middle of the list in whole-row steps.
    public float SelectedScrollPosition(ScrollRect scroll)
    {
        var grid = GetButtonLev.GetComponent<GridLayoutGroup>();
        var pitch = grid.cellSize.y + grid.spacing.y;
        var row = (RectTransform)ListButton[indexsence - 1].transform;
        var content = scroll.content;
        var rowTop = content.rect.yMax - content.InverseTransformPoint(row.TransformPoint(new Vector3(0f, row.rect.yMax))).y;
        var view = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
        return TreadShred.Progression.MissionList.ScrollPosition(Mathf.RoundToInt((rowTop - grid.padding.top) / pitch), pitch,
            content.rect.height, view.rect.height);
    }

    int indexsence = 1;
    private RectTransform rowPlay;
    private bool launching;
    public GameObject FadeIg;
    public void PlayLevel()
    {
        // D-041: the arrow and a second tap on the selected row both play; a mission launches once.
        if (launching)
            return;
        launching = true;
        ManagerScore.BeginNewRun();
        LoadLv.instance.StopMS();
        FadeIg.SetActive(true);
        StartCoroutine(DelayPlayLevel());
    }

    IEnumerator DelayPlayLevel()
    {
        yield return new WaitForSeconds(1f);

        // D-019: the mission's tier and map, with the promotion first when it is due.
        TreadShredCampaign.Launch(indexsence);
    }

}
