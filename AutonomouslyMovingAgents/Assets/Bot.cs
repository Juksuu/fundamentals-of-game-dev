using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Bot : MonoBehaviour
{

    public GameObject target;

    private NavMeshAgent agent;
    private Vector3 wanderTarget = Vector3.zero;
    private bool cooldown = false;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Seek(Vector3 location)
    {
        agent.SetDestination(location);
    }

    void Flee(Vector3 location)
    {
        Vector3 fleeVector = location - transform.position;
        agent.SetDestination(transform.position - fleeVector);
    }

    void Pursue()
    {
        Vector3 targetDir = target.transform.position - transform.position;

        float relativeHeading = Vector3.Angle(transform.forward, transform.TransformVector(target.transform.forward));
        float toTarget = Vector3.Angle(transform.forward, transform.TransformVector(targetDir));

        if ((toTarget > 90 && relativeHeading < 20) || target.GetComponent<Drive>().currentSpeed < 0.01f)
        {
            Seek(target.transform.position);
            return;
        }

        float lookAhead = targetDir.magnitude / (agent.speed + target.GetComponent<Drive>().currentSpeed);
        Seek(target.transform.position + target.transform.forward * lookAhead);
    }

    void Evade()
    {
        Vector3 targetDir = target.transform.position - transform.position;

        float lookAhead = targetDir.magnitude / (agent.speed + target.GetComponent<Drive>().currentSpeed);
        Flee(target.transform.position + target.transform.forward * lookAhead);
    }


    void Wander()
    {
        float wanderRadius = 10;
        float wanderDistance = 10;
        float wanderJitter = 1;

        wanderTarget += new Vector3(Random.Range(-1.0f, 1.0f) * wanderJitter, 0, Random.Range(-1.0f, 1.0f) * wanderJitter);
        
        wanderTarget.Normalize();
        wanderTarget *= wanderRadius;

        Vector3 targetLocal = wanderTarget + new Vector3(0, 0, wanderDistance);
        Vector3 targetWorld = gameObject.transform.InverseTransformVector(targetLocal);

        Seek(targetWorld);
    }

    void Hide()
    {
        float dist = Mathf.Infinity;
        Vector3 chosenSpot = Vector3.zero;

        GameObject[] hidingSpots = World.Instance.GetHidingSpots();
        for (int i = 0; i < hidingSpots.Length; i++)
        {
            GameObject obstacle = hidingSpots[i];
            Vector3 hideDir = obstacle.transform.position - target.transform.position;
            Vector3 hidePos = obstacle.transform.position + hideDir.normalized * 10;

            float distToObstacle = Vector3.Distance(transform.position, hidePos);
            if (distToObstacle < dist)
            {
                chosenSpot = hidePos;
                dist = distToObstacle;
            }
        }

        Seek(chosenSpot);
    }

    void CleverHide()
    {
        float dist = Mathf.Infinity;
        Vector3 chosenSpot = Vector3.zero;
        Vector3 chosenDir = Vector3.zero;

        GameObject[] hidingSpots = World.Instance.GetHidingSpots();
        GameObject chosenGO = hidingSpots[0];

        
        for (int i = 0; i < hidingSpots.Length; i++)
        {
            GameObject obstacle = hidingSpots[i];
            Vector3 hideDir = obstacle.transform.position - target.transform.position;
            Vector3 hidePos = obstacle.transform.position + hideDir.normalized * 10;

            float distToObstacle = Vector3.Distance(transform.position, hidePos);
            if (distToObstacle < dist)
            {
                chosenSpot = hidePos;
                chosenDir = hideDir;
                chosenGO = obstacle;
                dist = distToObstacle;
            }
        }

        Collider hideCol = chosenGO.GetComponent<Collider>();
        Ray backRay = new Ray(chosenSpot, -chosenDir.normalized);

        RaycastHit info;
        float distance = 100.0f;
        hideCol.Raycast(backRay, out info, distance);

        Seek(info.point + chosenDir.normalized * 2);
    }

    bool CanSeeTarget()
    {
        RaycastHit info;
        Vector3 rayToTarget = target.transform.position - transform.position;
        if (Physics.Raycast(transform.position, rayToTarget, out info))
        {
            if (info.transform.gameObject.tag == "cop")
            {
                return true;
            }
        }
        return false;
    }

    bool TargetCanSeeMe()
    {
        Vector3 toAgent = transform.position - target.transform.position;
        float lookingAngle = Vector3.Angle(target.transform.forward, toAgent);

        return lookingAngle < 60;
    }

    bool ShouldWander()
    {
        float wanderRange = 10;
        return Vector3.Distance(transform.position, target.transform.position) > wanderRange;
    }

    void BehaviourCoolDown()
    {
        cooldown = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!cooldown)
        {
            if (ShouldWander())
            {
                Wander();
            }
            else if (CanSeeTarget() && TargetCanSeeMe())
            {
                CleverHide();
                cooldown = true;
                Invoke(nameof(BehaviourCoolDown), 5);
            }
            else
            {
                Pursue();
            }
        }
    }
}
