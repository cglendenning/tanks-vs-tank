using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class AutuScroll : MonoBehaviour {
    public static AutuScroll instance;
    public ScrollRect myScrollRect;
    public Scrollbar newScrollBar;
    public bool ismove;
    public void Start()
    {
        instance = this;
        myScrollRect.verticalNormalizedPosition = 1f;
        StartCoroutine(Delay());
    }

    // D-041: glide to the selected mission's row (the next to play) unless the player moves the list first.
    IEnumerator Delay()
    {
        yield return new WaitForSeconds(0.5f);
        var target = Memory.instance.SelectedScrollPosition(myScrollRect);
        while (ismove == false && Mathf.Abs(myScrollRect.verticalNormalizedPosition - target) > 0.001f)
        {
            yield return new WaitForSeconds(0.03f);
            if (ismove == false)
            {
                myScrollRect.verticalNormalizedPosition = Mathf.MoveTowards(myScrollRect.verticalNormalizedPosition, target, 0.02f);
            }
        }
    }
}
