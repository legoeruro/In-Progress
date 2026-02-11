public struct FormRewardContext
{
    public readonly GameStateManager GameState;
    public readonly FormManager FormManager;
    public readonly InventoryCatalog InventoryCatalog;
    public readonly FormDefinition FormDefinition;
    public readonly Form Form;

    public FormRewardContext(
        GameStateManager gameState,
        FormManager formManager,
        InventoryCatalog inventoryCatalog,
        FormDefinition formDefinition,
        Form form)
    {
        GameState = gameState;
        FormManager = formManager;
        InventoryCatalog = inventoryCatalog;
        FormDefinition = formDefinition;
        Form = form;
    }
}
