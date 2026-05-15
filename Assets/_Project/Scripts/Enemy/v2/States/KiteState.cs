using System;
using UnityEngine;

namespace ITCLASH.Enemies
{
    /// Maintain preferred distance from player. Transition out via DecideTransition delegate.
    public sealed class KiteState : EnemyState
    {
        const float BACKOFF_SPEED_MUL = 1.1f;

        public Func<EnemyState> DecideTransition;

        public KiteState(EnemyController owner) : base(owner) { }

        public override void OnEnter()
        {
            owner.DebugState("Kite → Enter");
            ResumeAgent();
        }

        public override void Tick(float dt)
        {
            var target = owner.GetCombatTarget();
            if (target == null) return;

            float dist = owner.DistanceToCombatTarget();
            float preferred = owner.Stats.preferredRange;
            float tooClose  = owner.Stats.tooCloseRange;

            Vector3 self    = owner.transform.position;
            Vector3 toTarget = target.position - self;
            toTarget.y = 0f;
            Vector3 dir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : owner.transform.forward;

            if (dist < tooClose)
            {
                // ถอยหนี (Retreat) - สั่งจุดเป้าหมายให้ไกลออกไปเพื่อให้มันเคลื่อนที่ต่อเนื่อง
                Vector3 awayPoint = self - dir * 5f; 
                if (owner.Agent != null && owner.Agent.isOnNavMesh)
                {
                    owner.Agent.speed = owner.Stats.moveSpeed * BACKOFF_SPEED_MUL;
                    owner.Agent.SetDestination(awayPoint);
                }
                if (owner.Anim != null) owner.Anim.SetBool("IsWalking", true);
            }
            else if (dist > preferred)
            {
                // Close in
                if (owner.Agent != null && owner.Agent.isOnNavMesh)
                {
                    owner.Agent.speed = owner.Stats.moveSpeed;
                    owner.Agent.SetDestination(target.position);
                }
                if (owner.Anim != null) owner.Anim.SetBool("IsWalking", true);
            }
            else
            {
                // Hold position
                if (owner.Agent != null && owner.Agent.isOnNavMesh)
                {
                    owner.Agent.SetDestination(self);
                    owner.Agent.velocity = Vector3.zero;
                }
                if (owner.Anim != null) owner.Anim.SetBool("IsWalking", false);
                owner.FacePlayer(dt);
            }

            var next = DecideTransition?.Invoke();
            if (next != null) owner.StateMachine.ChangeState(next);
        }

        public override void OnExit()
        {
            if (owner.Anim != null) owner.Anim.SetBool("IsWalking", false);
        }
    }
}
