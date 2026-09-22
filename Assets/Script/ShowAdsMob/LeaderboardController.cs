using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class LeaderboardController : MonoBehaviour {

	private static LeaderboardController instance;
    public bool useLeaderboard;
    public GameObject btnLeaderboard;
    public static LeaderboardController Instance{
		get{ 
			return instance;
		}
	}

	//private const string LEADERBOARD_Star = "Super_Jungle_star";
	public string LEADERBOARD_HighScore = "Tank_Heroes_score";
	public  string LEADERBOARD_Level = "Tank_Heroes_level";

	void Awake(){
		instance = this;
	}

	void Start() {
        if (useLeaderboard)
        {
            StartCoroutine(checkInternetConnection((isConnected) => {
                if (isConnected)
                {
                    Social.localUser.Authenticate((bool success) => {
                        float TotalHighScore = 0, levels = 0;
                        for (int i = 1; i <= (int)PlayerPrefs.GetFloat("OpenLv"); i++)
                        {
                            TotalHighScore += PlayerPrefs.GetFloat("Lvs" + i);
                            //TotalStar += PlayerPrefs.GetInt("starlv" + i);
                        }
                        levels = PlayerPrefs.GetFloat("OpenLv");

                        submitLevel((int)levels);
                        submitHighScore((int)TotalHighScore);
                        //submitStar (TotalStar);
                    });

                }
            }));

        }
        btnLeaderboard.SetActive(useLeaderboard);
    }		

	IEnumerator checkInternetConnection(Action<bool> action){
		WWW www = new WWW("http://google.com");
		yield return www;
		if (www.error != null) {
			action (false);
		} else {
			action (true);
		}
	} 

	public void showLeaderBoardsUI() {
		Debug.Log ("adfasdf");
		Social.ShowLeaderboardUI ();
		float TotalHighScore = 0, levels = 0 ;
		for (int i = 1; i <= PlayerPrefs.GetFloat ("OpenLv"); i++) {
			TotalHighScore += PlayerPrefs.GetFloat ("Lvs" + i);
			//TotalStar += PlayerPrefs.GetInt("starlv" + i);
		}

        levels = PlayerPrefs.GetFloat ("OpenLv");
        Debug.Log("score  " + levels);

        submitLevel((int)PlayerPrefs.GetFloat("OpenLv"));
		submitHighScore ((int)TotalHighScore);
		//submitStar (TotalStar);
		Debug.Log (levels + " " + TotalHighScore + " " );
	}				

	public void submitStar(int level) {		
	//	Social.ReportScore (level, LEADERBOARD_Star, success => {});
	}						

	public void submitHighScore(int score) {
        Social.ReportScore(score, LEADERBOARD_HighScore, success => { });
    }

	public void submitLevel(int level) {		
		Social.ReportScore (level, LEADERBOARD_Level, success => {});
	}						


}
