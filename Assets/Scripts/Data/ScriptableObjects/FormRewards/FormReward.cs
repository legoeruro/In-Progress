using UnityEngine;

public abstract class FormReward : ScriptableObject
{
    public abstract void Apply(FormRewardContext context);
}
