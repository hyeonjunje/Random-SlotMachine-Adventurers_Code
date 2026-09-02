using UnityEngine;

public class ResetSpeedOnIdle : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.speed = 1.0f;
    }
}