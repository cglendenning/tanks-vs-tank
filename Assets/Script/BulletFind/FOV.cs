using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FOV : MonoBehaviour
{

    public float viewRadius;
    [Range(0, 360)]
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

        Collider[] targetInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        for (int i = 0; i < targetInViewRadius.Length; i++)
        {
            Transform target = targetInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngel / 2)
            {
                float disToTarget = Vector3.Distance(transform.position, target.position);
                if (!Physics.Raycast(transform.position, dirToTarget, disToTarget, obstacleMask))
                {

                    if (isspeed )
                    {
                        StartCoroutine(SpeedSlow());
                    }

                    if (gameObject.tag == "findemnemy" && choosed)
                    {
                        choosed = false;
                        gameObject.GetComponent<BulletFind>().Target = target.gameObject;
                    }
                   
                   
                }
                   
            }
        }
    }

    bool choosed = true;

    bool isspeed = true;
    IEnumerator SpeedSlow()
    {
        isspeed = false;
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForSeconds(0.1f);
            gameObject.GetComponent<Rigidbody>().velocity = gameObject.GetComponent<Rigidbody>().velocity * 0.7f; 
        }

    }

    public Vector3 DirFromAngel(float angelInDegree, bool angdelInGlobal)
    {
        if (!angdelInGlobal)
        {
            angelInDegree += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angelInDegree * Mathf.Deg2Rad), 0, Mathf.Cos(angelInDegree * Mathf.Deg2Rad));
    }


}
