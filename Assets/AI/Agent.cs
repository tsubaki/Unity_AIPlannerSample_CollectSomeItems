using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.AI;

public class Agent : MonoBehaviour
{
    private NavMeshAgent _agent;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    public IEnumerator MoveTo(GameObject target)
    {
        Debug.Log($"move to {target.name}");

        _agent.isStopped = false;
        _agent.SetDestination(target.transform.position);

        yield return new WaitUntil(()=> _agent.pathStatus ==  NavMeshPathStatus.PathComplete);
        yield return new WaitUntil(()=> _agent.remainingDistance < 0.1f);

        _agent.isStopped = true;
    }
}
