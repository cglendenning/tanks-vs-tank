using UnityEngine;
using System.Collections;

public class GoogleShare : MonoBehaviour {

   // AN_PlusButton PlusButton;
	// Use this for initialization
	void Start () {

        //string PlusUrl = "https://unionassets.com/";
        //PlusButton = new AN_PlusButton(PlusUrl, AN_PlusBtnSize.SIZE_STANDARD, AN_PlusBtnAnnotation.ANNOTATION_BUBBLE);
        ////PlusButton.SetGravity(TextAnchor.UpperLeft);
        //PlusButton.SetPosition(100, 100);
 
	}
	
	public void AnGoogle(){
       // PlusButton.Hide();
    }
    public void BatG()
    {
      //  StartCoroutine(Delay());
    }
    IEnumerator Delay()
    {
        yield return new WaitForSeconds(0);
       // PlusButton.Show();
    }
}
