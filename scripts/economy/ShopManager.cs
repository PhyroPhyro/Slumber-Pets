using Godot;
using System;
using System.Collections.Generic;
public partial class ShopManager : Node
{
[Signal] public delegate void OnItemPurchasedEventHandler(string itemId);
private List<ShopItem> _shopItems = new();
public List<ShopItem> ShopItems => _shopItems;
public override void _Ready()
{
LoadShopItems();
GD.Print("[ShopManager] Loaded ", _shopItems.Count, " items in shop");
}
private void LoadShopItems()
{
string path = "res://data/items.json";
if (!FileAccess.FileExists(path))
{
GD.PrintErr("[ShopManager] items.json not found");
return;
}
try
{
using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
string json = file.GetAsText();
var data = JsonSerialization.FromJson<ItemsData>(json);
if (data != null && data.Items != null)
{
_shopItems.Clear();
foreach (var item in data.Items)
{
if (item.ShopPrice > 0)
{
                _shopItems.Add(new ShopItem
                {
                    Id = item.Id,
                    Name = item.Name,
                    Rarity = item.Rarity,
                    Type = item.Type,
                    Price = item.ShopPrice,
                    HungerRestore = item.HungerRestore,
                    HappinessRestore = item.HappinessRestore,
                    EnergyRestore = item.EnergyRestore,
                    EffectType = item.EffectType,
                    EffectDuration = item.EffectDuration,
                    EffectValue = item.EffectValue,
                    Sprite = ItemSpriteGenerator.GenerateSprite(item.Id, item.Type, item.Rarity)
                });
}
}
}
}
catch (Exception e)
{
GD.PrintErr("[ShopManager] Error loading items.json: ", e.Message);
}
}
public bool PurchaseItem(string itemId)
{
var item = _shopItems.Find(i => i.Id == itemId);
if (item == null)
{
GD.PrintErr("[ShopManager] Item not found in shop: ", itemId);
return false;
}
var resourceManager = GetNode<ResourceManager>("/root/ResourceManager");
if (!resourceManager.HasEnough(item.Price))
{
GD.Print("[ShopManager] Not enough coins for ", item.Name);
return false;
}
if (resourceManager.SpendCoins(item.Price))
{
var inventoryManager = GetNode<InventoryManager>("/root/InventoryManager");
inventoryManager.AddItem(itemId, 1);
EmitSignal(SignalName.OnItemPurchased, itemId);
GD.Print("[ShopManager] Purchased: ", item.Name, " for ", item.Price, " coins");
return true;
}
return false;
}
public ShopItem GetItem(string itemId)
{
return _shopItems.Find(i => i.Id == itemId);
}
}
public class ShopItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Rarity { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Price { get; set; } = 0;
    public int HungerRestore { get; set; } = 0;
    public int HappinessRestore { get; set; } = 0;
    public int EnergyRestore { get; set; } = 0;
    public string EffectType { get; set; } = "none";
    public int EffectDuration { get; set; } = 0;
    public int EffectValue { get; set; } = 0;
    public Texture2D Sprite { get; set; }
}
public class ItemsData
{
    [System.Text.Json.Serialization.JsonPropertyName("items")]
    public List<ItemEntry> Items { get; set; } = new();
}
public class ItemEntry
{
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("rarity")]
    public string Rarity { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("hungerRestore")]
    public int HungerRestore { get; set; } = 0;
    [System.Text.Json.Serialization.JsonPropertyName("happinessRestore")]
    public int HappinessRestore { get; set; } = 0;
    [System.Text.Json.Serialization.JsonPropertyName("energyRestore")]
    public int EnergyRestore { get; set; } = 0;
    [System.Text.Json.Serialization.JsonPropertyName("effectType")]
    public string EffectType { get; set; } = "none";
    [System.Text.Json.Serialization.JsonPropertyName("effectDuration")]
    public int EffectDuration { get; set; } = 0;
    [System.Text.Json.Serialization.JsonPropertyName("effectValue")]
    public int EffectValue { get; set; } = 0;
    [System.Text.Json.Serialization.JsonPropertyName("shopPrice")]
    public int ShopPrice { get; set; } = 0;
}