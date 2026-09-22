using UnityEngine;
using System.Collections;
//using UnityEditor;

//[CustomEditor(typeof(FieldOfView))]
//public class FieldOfViewEditor : Editor
//{
public class FieldOfViewEditor : MonoBehaviour
{

    //void OnSceneGUI()
    //{
    //    FieldOfView fow = (FieldOfView)target;
    //    Handles.color = Color.white;
    //    Handles.DrawWireArc(fow.transform.position, Vector3.up, Vector3.forward, 360, fow.viewRadius);
    //    Vector3 viewAngelA = fow.DirFromAngel(-fow.viewAngel / 2, false);
    //    Vector3 viewAngelB = fow.DirFromAngel(fow.viewAngel / 2, false);

    //    Handles.DrawLine(fow.transform.position, fow.transform.position + viewAngelA * fow.viewRadius);
    //    Handles.DrawLine(fow.transform.position, fow.transform.position + viewAngelB * fow.viewRadius);

    //    Handles.color = Color.red;
    //    foreach (Transform item in fow.visibelTarget)
    //    {
    //        Handles.DrawLine(fow.transform.position, item.position);
    //    }

    //}

}
