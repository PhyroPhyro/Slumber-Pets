using Godot;
using System;
using System.Collections.Generic;
public partial class InventoryManager : Node
{
[Signal] public delegate void OnInventoryChangedEventHandler();
[Signal] public delegate void OnItemUsedEventHandler(string itemId);
private Dictionary<string, int> _items = new();
public override void _Ready()
{
LoadFromSave();
GD.Print("[InventoryManager] Initialized with ", _items.Count, " item types");
}
private void LoadFromSave()
{
var gameManager = GetNode<GameManager>("/root/GameManager");
var save = gameManager.CurrentSave;
_items.Clear();
foreach (var itemData in save.Inventory.Items)
{
_items[itemData.Id] = itemData.Qty;
}
}
public void AddItem(string itemId, int quantity = 1)
{
if (!_items.ContainsKey(itemId))
{
_items[itemId] = 0;
}
_items[itemId] += quantity;
SaveToSave();
EmitSignal(SignalName.OnInventoryChanged);
GD.Print("[InventoryManager] Added ", quantity, "x ", itemId);
}
public bool UseItem(string itemId)
{
if (!_items.ContainsKey(itemId) || _items[itemId] <= 0)
{
GD.Print("[InventoryManager] Item not found or quantity is 0: ", itemId);
return false;
}
_items[itemId]--;
if (_items[itemId] <= 0)
{
_items.Remove(itemId);
}
SaveToSave();
EmitSignal(SignalName.OnItemUsed, itemId);
EmitSignal(SignalName.OnInventoryChanged);
GD.Print("[InventoryManager] Used 1x ", itemId);
return true;
}
public int GetItemQuantity(string itemId)
{
return _items.ContainsKey(itemId) ? _items[itemId] : 0;
}
public Dictionary<string, int> GetAllItems()
{
return new Dictionary<string, int>(_items);
}
public bool HasItem(string itemId, int quantity = 1)
{
return _items.ContainsKey(itemId) && _items[itemId] >= quantity;
}
private void SaveToSave()
{
var gameManager = GetNode<GameManager>("/root/GameManager");
var save = gameManager.CurrentSave;
save.Inventory.Items.Clear();
foreach (var kvp in _items)
{
if (kvp.Value > 0)
{
save.Inventory.Items.Add(new ItemData { Id = kvp.Key, Qty = kvp.Value });
}
}
gameManager.SaveGame();
}
}