using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IKWalkWeight : StateMachineBehaviour
{
    public float walkWeight = 0.5f;
    public float idleWeight = 1f;

    private TwoBoneIKConstraint rightIK;
    private TwoBoneIKConstraint leftIK;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (rightIK == null)
        {
            rightIK = animator.GetComponentInChildren<TwoBoneIKConstraint>();
            var allIK = animator.GetComponentsInChildren<TwoBoneIKConstraint>();
            if (allIK.Length >= 2)
            {
                rightIK = allIK[0];
                leftIK = allIK[1];
            }
        }

        if (rightIK != null) rightIK.weight = walkWeight;
        if (leftIK != null) leftIK.weight = walkWeight;
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (rightIK != null) rightIK.weight = idleWeight;
        if (leftIK != null) leftIK.weight = idleWeight;
    }
}