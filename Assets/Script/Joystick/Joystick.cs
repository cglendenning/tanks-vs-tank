using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class Joystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler {

   

    Image bgImg;
    Image joystickImg;
    Vector3 inputvector;
    bool _isdragging;
    private Vector3 _startPoint;

    void Start()
    {

        bgImg = GetComponent<Image>();
        joystickImg = transform.GetChild(0).GetComponent<Image>();
        _startPoint = joystickImg.rectTransform.position;
    }

    public void OnDrag(PointerEventData ped)
    {
      
        Vector2 pos;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bgImg.rectTransform,ped.position,ped.pressEventCamera,out pos))
        {
            pos.x = (pos.x/bgImg.rectTransform.sizeDelta.x);
            pos.y = (pos.y/bgImg.rectTransform.sizeDelta.y);

            //Debug.Log(pos);

            inputvector = new Vector3(pos.x,0,pos.y);

            inputvector = (inputvector.magnitude > 1.0f) ? inputvector.normalized : inputvector;

           // Debug.Log(inputvector);

            // Move Joysick Img;
            joystickImg.rectTransform.anchoredPosition =
                new Vector3(inputvector.x * (bgImg.rectTransform.sizeDelta.x/2), inputvector.z * (bgImg.rectTransform.sizeDelta.y/2));

        }
    }

    public void OnPointerUp(PointerEventData ped)
    {
        _isdragging = false;
        inputvector = Vector3.zero;
        joystickImg.rectTransform.anchoredPosition = Vector3.zero;
    }

    public void OnPointerDown(PointerEventData ped)
    {
        _isdragging = true;
        OnDrag(ped);
    }

    public float Horizontal()
    {
        if (inputvector.x != 0)
            return inputvector.x;
        else
            return Input.GetAxis("Horizontal");

    }

    public float Vertical()
    {
        if (inputvector.z != 0)
            return inputvector.z;
        else
            return Input.GetAxis("Vertical");
    }

    public void RotateTarget(Transform t)
    {
         if (_isdragging)
     
        Rotate(t, joystickImg.rectTransform.position, _startPoint);

    }


    public void Rotate(Transform t, Vector3 position, Vector3 startPoint)
    {
        Vector3 r = position - startPoint;
        r.z = r.y;
        r.y = 0;
        Quaternion newRotaion = Quaternion.LookRotation(r);
        t.localRotation = newRotaion;
    }

}
