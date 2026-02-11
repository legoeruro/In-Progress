using UnityEngine;

[CreateAssetMenu(menuName = "In Progress/Forms/Rewards/Trigger Flag")]
public class TriggerFlagReward : FormReward
{
    [SerializeField] private GameFlags flag;
    [SerializeField] private bool state = true;

    public override void Apply(FormRewardContext context)
    {
        if (context.GameState == null || flag == null)
            return;

        context.GameState.SetFlagState(flag, state);
    }
}
