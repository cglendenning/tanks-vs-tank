using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

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
        foreach (Transform item in GetButtonLev.transform)
        {
            ListButton.Add(item.gameObject);
            MakeRoomForPlay(item);
            KeepLabelClearOfMedal(item);
        }

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


            // Gán Medel khi load Menu

            if (PlayerPrefs.GetFloat("Lv" + (i + 1).ToString()) == 3)
            {
                ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(3).gameObject.SetActive(true);
                ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(2).gameObject.SetActive(false);
                ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(1).gameObject.SetActive(false);
                ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(0).gameObject.SetActive(false);
            }
            else if (PlayerPrefs.GetFloat("Lv" + (i + 1).ToString()) == 2)
            {
                ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(3).gameObject.SetActive(false);
                ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(2).gameObject.SetActive(true);
                ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(1).gameObject.SetActive(false);
                ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(0).gameObject.SetActive(false);
            }
            else if (PlayerPrefs.GetFloat("Lv" + (i + 1).ToString()) == 1)
            {
                ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(3).gameObject.SetActive(false);
                ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(2).gameObject.SetActive(false);
                ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(1).gameObject.SetActive(true);
                ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(0).gameObject.SetActive(false);
            }
            else
            {
                if (i != 0)
                {
                    if ((int)(PlayerPrefs.GetFloat("Lv" + (i).ToString())) != 0)
                    {
                        ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(3).gameObject.SetActive(false);
                        ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(2).gameObject.SetActive(false);
                        ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(1).gameObject.SetActive(false);
                        ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(0).gameObject.SetActive(false);
                    }
                    else
                    {
                        ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(3).gameObject.SetActive(false);
                        ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(2).gameObject.SetActive(false);
                        ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(1).gameObject.SetActive(false);
                        ListButton[i].gameObject.transform.GetChild(3).gameObject.transform.GetChild(0).gameObject.SetActive(true);
                    }
                   
                }
               
            }

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

    // D-041: the list opens on the mission to play next at the selected tier.
    private int NextMission()
    {
        var progress = TreadShredProgress.Ensure();
        return TreadShred.Progression.MissionList.NextToPlay(ListButton.Count,
            mission => !ListButton[mission - 1].transform.Find("Medel/Lock").gameObject.activeSelf,
            mission => progress.Progression.IsCleared(progress.SelectedTier, mission));
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

        Application.LoadLevel(("Lv" + indexsence.ToString()));
    }

}
