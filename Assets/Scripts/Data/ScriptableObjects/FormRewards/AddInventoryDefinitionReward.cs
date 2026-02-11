using UnityEngine;

[CreateAssetMenu(menuName = "In Progress/Forms/Rewards/Add Inventory Definition")]
public class AddInventoryDefinitionReward : FormReward
{
    [SerializeField] private WordBlockDefinition definitionToAdd;

    public override void Apply(FormRewardContext context)
    {
        if (definitionToAdd == null || context.InventoryCatalog == null)
            return;

        context.InventoryCatalog.AddDefinition(definitionToAdd);
    }
}
