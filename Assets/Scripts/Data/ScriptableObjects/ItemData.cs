using UnityEngine;
using InProcess.Gameplay;

[CreateAssetMenu(menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemId;
    public ItemType type;
    public Sprite sprite;
}
