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

	// Use this for initialization
	void Start () {
     
        instance = this;
        foreach (Transform item in GetButtonLev.transform)
        {
            ListButton.Add(item.gameObject);
        }

        HightScoreTextButton.text = "BEST SCORE // " + (PlayerPrefs.GetFloat("Lvs" + 1.ToString())).ToString();
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


        ListImageWhite[0].SetActive(true);
        ListImageBlack[0].SetActive(false);


 
	}

    public void ChooseLevel(Text levelplay)
    {

        if (AutuScroll.instance.ismove == false)
        {
            AutuScroll.instance.ismove = true;
        }
        

        string levelcurrent = (levelplay.text).Substring(8);
        LevelChoosed = int.Parse(levelcurrent);

        if (!ListButton[LevelChoosed - 1 ].gameObject.transform.GetChild(3).gameObject.transform.GetChild(0).gameObject.activeInHierarchy)
        {
            indexsence = LevelChoosed;
            HightScoreTextButton.text = "BEST SCORE // " + (PlayerPrefs.GetFloat("Lvs" + LevelChoosed.ToString())).ToString();
            LevelTextButton.text = "MISSION: " + LevelChoosed.ToString();

            switch ((int)(PlayerPrefs.GetFloat("Lv" + LevelChoosed.ToString())))
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
                if (i == (LevelChoosed - 1))
                {
                    ListImageWhite[LevelChoosed - 1].SetActive(true);
                    ListImageBlack[LevelChoosed - 1].SetActive(false);
                }
                else if (ListImageWhite[i].gameObject.activeInHierarchy)
                {
                    ListImageWhite[i].gameObject.SetActive(false);
                    ListImageBlack[i].SetActive(true);
                }

            }
       
        }

    }

    int indexsence = 1;
    public GameObject FadeIg;
    public void PlayLevel()
    {
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
