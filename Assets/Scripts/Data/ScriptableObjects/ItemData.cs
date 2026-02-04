using UnityEngine;
using InProcess.Gameplay;

[CreateAssetMenu(menuName = "In Progress/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemId;
    public ItemType type;
    public Sprite sprite;
}
