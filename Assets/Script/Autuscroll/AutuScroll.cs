using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class AutuScroll : MonoBehaviour {
    public static AutuScroll instance;
    public ScrollRect myScrollRect;
    public Scrollbar newScrollBar;
    public bool ismove;
    int levels;
    public void Start()
    {
        instance = this;
        levels = (int)PlayerPrefs.GetFloat("OpenLv");
        myScrollRect.verticalNormalizedPosition = 1f;
        //Change the current vertical scroll position.
        if (levels > 3)
        {
            StartCoroutine(Delay());
        }
        
    }
  
    IEnumerator Delay()
    {
        yield return new WaitForSeconds(0.5f);

        if (levels < 20)
        {
            for (int i = 0; i < (int)((levels - 2) / (40 * 0.01f)); i++)
            {
                yield return new WaitForSeconds(0.03f);
                if (ismove == false)
                {
                    myScrollRect.verticalNormalizedPosition -= 0.01f;
                }

            }
        }
        else
        {
            for (int i = 0; i < (int)((levels) / (40 * 0.01f)); i++)
            {
                yield return new WaitForSeconds(0.03f);
                if (ismove == false)
                {
                    myScrollRect.verticalNormalizedPosition -= 0.01f;
                }

            }
        }
       
      
    }
}
