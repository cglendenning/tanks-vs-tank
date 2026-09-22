using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FieldOfView : MonoBehaviour {

    public float viewRadius;
    [Range(0,360)]
    public float viewAngel;

    public LayerMask targetMask;
    public LayerMask obstacleMask;

    public List<Transform> visibelTarget = new List<Transform>();

    void Start()
    {
        StartCoroutine(FindTargetWithDelay(.2f));

    }


    IEnumerator FindTargetWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibalTarget();
        }

    }


    void FindVisibalTarget()
    {
        visibelTarget.Clear();

        Collider[] targetInViewRadius = Physics.OverlapSphere(transform.position,viewRadius,targetMask);

        for (int i = 0; i < targetInViewRadius.Length; i++)
        {
            Transform target = targetInViewRadius[i].transform;
            Vector3 dirToTarget =  (target.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward,dirToTarget) < viewAngel/2)
            {
                float disToTarget = Vector3.Distance(transform.position,target.position);
                if (!Physics.Raycast(transform.position,dirToTarget,disToTarget,obstacleMask))
                {
                    target.gameObject.GetComponent<BulletEmnemy>().BulletPlayer();                   
                    visibelTarget.Add(target);

                    //if (target.gameObject.tag == "emnemyred")
                    //{
                    //    if ( target.gameObject.GetComponent<ThrowSimulation>().timedelay > 5f)
                    //    {
                    //        if (Vector3.Distance(transform.position,target.position) < 20f )
                    //        {
                    //            target.gameObject.GetComponent<ThrowSimulation>().timedelay = 3f;
                    //        }
                    //        else
                    //        {
                    //            target.gameObject.GetComponent<ThrowSimulation>().timedelay = 7f;
                    //        }                                                       
                    //    }                   
                    //}
                }
                //else if (target.gameObject.tag == "emnemyred")
                //{
                //    target.gameObject.GetComponent<ThrowSimulation>().timedelay = 7f;
                //}
            }
        }
    }

    public Vector3 DirFromAngel(float angelInDegree, bool angdelInGlobal)
    {
        if (!angdelInGlobal)
        {
            angelInDegree += transform.eulerAngles.y; 
        }
        return new Vector3(Mathf.Sin(angelInDegree*Mathf.Deg2Rad),0,Mathf.Cos(angelInDegree*Mathf.Deg2Rad));
    }


}
