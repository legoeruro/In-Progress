using UnityEngine;

[CreateAssetMenu(menuName = "In Progress/Forms/Rewards/Trigger Flag")]
public class TriggerFlagReward : FormReward
{
    [SerializeField] private GameFlags flag;
    [SerializeField] private string flagNameOverride;
    [SerializeField] private bool alsoApplyByFlagName = true;
    [SerializeField] private bool state = true;

    public override void Apply(FormRewardContext context)
    {
        if (context.GameState == null)
        {
            Debug.LogWarning($"TriggerFlagReward '{name}': GameStateManager is missing.");
            return;
        }

        if (flag != null)
            context.GameState.SetFlagState(flag, state);

        if (alsoApplyByFlagName)
        {
            string flagName = !string.IsNullOrWhiteSpace(flagNameOverride)
                ? flagNameOverride
                : flag != null ? flag.flagName : string.Empty;

            if (!string.IsNullOrWhiteSpace(flagName))
                context.GameState.SetFlagState(flagName, state);
            else if (flag == null)
                Debug.LogWarning($"TriggerFlagReward '{name}': No flag asset or flag name configured.");
        }
    }
}
