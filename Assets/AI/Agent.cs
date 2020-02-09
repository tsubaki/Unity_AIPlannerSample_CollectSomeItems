using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AI;

public class Agent : MonoBehaviour
{
    public IEnumerator MoveToGoal(GameObject self, GameObject target)
    {
        Debug.Log($"move to {target.name}");

        var agent = self.GetComponent<NavMeshAgent>();
        agent.isStopped = false;
        agent.SetDestination(target.transform.position);

        yield return new WaitUntil(()=> agent.pathStatus ==  NavMeshPathStatus.PathComplete);
        yield return new WaitUntil(()=> agent.remainingDistance < 0.1f);

        agent.isStopped = true;
    }
}
