using Godot;
using System;
using System.Collections.Generic;
public partial class ItemRoller : Node
{
private List<ItemEntry> _itemsByRarity = new();
private RandomNumberGenerator _rng = new();
public override void _Ready()
{
_rng.Randomize();
LoadItems();
GD.Print("[ItemRoller] Loaded ", _itemsByRarity.Count, " items for rolling");
}
private void LoadItems()
{
string path = "res://data/items.json";
if (!FileAccess.FileExists(path))
{
GD.PrintErr("[ItemRoller] items.json not found");
return;
}
try
{
using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
string json = file.GetAsText();
var data = JsonSerialization.FromJson<ItemsData>(json);
if (data != null && data.Items != null)
{
_itemsByRarity = data.Items;
}
}
catch (Exception e)
{
GD.PrintErr("[ItemRoller] Error loading items.json: ", e.Message);
}
}
public ItemDrop Roll(int milestoneIndex)
{
string targetRarity = GetRarityForMilestone(milestoneIndex);
int rareChance = GetRareChanceForMilestone(milestoneIndex);
var eligibleItems = _itemsByRarity.FindAll(i =>
i.Rarity == targetRarity || IsLowerRarity(i.Rarity, targetRarity));
if (eligibleItems.Count == 0)
{
eligibleItems = _itemsByRarity.FindAll(i => i.Rarity == "common");
}
    bool rolledRare = _rng.RandiRange(1, 100) <= GetRareChanceWithLuck(rareChance);
if (rolledRare)
{
var rareItems = _itemsByRarity.FindAll(i => i.Rarity == "rare+" || i.Rarity == "rare");
if (rareItems.Count > 0)
{
var item = rareItems[_rng.RandiRange(0, rareItems.Count - 1)];
return new ItemDrop { Id = item.Id, Name = item.Name, Rarity = item.Rarity };
}
}
var chosenItem = eligibleItems[_rng.RandiRange(0, eligibleItems.Count - 1)];
return new ItemDrop { Id = chosenItem.Id, Name = chosenItem.Name, Rarity = chosenItem.Rarity };
}
private string GetRarityForMilestone(int index)
{
return index switch
{
0 => "common",
1 => "common+",
2 => "uncommon",
3 => "uncommon+",
4 => "rare",
_ => "rare+"
};
}
private int GetRareChanceForMilestone(int index)
{
return index switch
{
0 => 5,
1 => 15,
2 => 30,
3 => 50,
4 => 75,
_ => 90
};
}
    private bool IsLowerRarity(string itemRarity, string targetRarity)
    {
        var order = new[] { "common", "common+", "uncommon", "uncommon+", "rare", "rare+" };
        int itemIdx = Array.IndexOf(order, itemRarity);
        int targetIdx = Array.IndexOf(order, targetRarity);
        return itemIdx >= 0 && targetIdx >= 0 && itemIdx < targetIdx;
    }

    private int GetRareChanceWithLuck(int baseChance)
    {
        var effectManager = GetNode<ItemEffectManager>("/root/ItemEffectManager");
        float multiplier = effectManager.GetLuckMultiplier();
        return Mathf.Min(Mathf.RoundToInt(baseChance * multiplier), 100);
    }
}
public class ItemDrop
{
public string Id { get; set; } = string.Empty;
public string Name { get; set; } = string.Empty;
public string Rarity { get; set; } = string.Empty;
}