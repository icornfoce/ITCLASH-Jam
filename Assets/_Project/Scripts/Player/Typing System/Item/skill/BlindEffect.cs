using UnityEngine;
using UnityEngine.AI;
using ITCLASH.Enemies;

/// Temporary stun: freezes NavMesh movement, AI ticking, and animation playback.
/// Auto-removes when timer expires; cleans up on destroy so a mid-stun death never leaks state.
public class BlindEffect : MonoBehaviour
{
    private EnemyController enemy;
    private NavMeshAgent agent;
    private Animator anim;

    private bool prevAgentStopped;
    private float prevAnimSpeed;
    private bool prevControllerEnabled;

    private float timer;
    private bool initialized;

    public void Setup(EnemyController controller, float duration)
    {
        enemy = controller;
        agent = enemy != null ? enemy.Agent : null;
        anim = enemy != null ? enemy.Anim : null;
        timer = duration;

        if (agent != null)
        {
            prevAgentStopped = agent.isStopped;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (anim != null)
        {
            prevAnimSpeed = anim.speed;
            anim.speed = 0f;
        }

        if (enemy != null)
        {
            prevControllerEnabled = enemy.enabled;
            enemy.enabled = false;
        }

        initialized = true;
    }

    public void Refresh(float duration)
    {
        timer = Mathf.Max(timer, duration);
    }

    private void Update()
    {
        if (!initialized) return;

        timer -= Time.deltaTime;
        if (timer <= 0f) Destroy(this);
    }

    private void OnDestroy()
    {
        if (!initialized) return;

        if (enemy != null) enemy.enabled = prevControllerEnabled;
        if (anim != null) anim.speed = prevAnimSpeed;
        if (agent != null && agent.isActiveAndEnabled) agent.isStopped = prevAgentStopped;
    }
}
