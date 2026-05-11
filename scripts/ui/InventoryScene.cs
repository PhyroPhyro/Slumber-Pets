using Godot;
using System;
public partial class InventoryScene : Control
{
private VBoxContainer _itemList;
private Label _coinsLabel;
public override void _Ready()
{
_itemList = GetNode<VBoxContainer>("MarginContainer/VBox/ItemList");
_coinsLabel = GetNode<Label>("MarginContainer/VBox/TitleLabel");
var resourceManager = GetNode<ResourceManager>("/root/ResourceManager");
resourceManager.OnStarCoinsChanged += UpdateCoins;
UpdateCoins(resourceManager.StarCoins);
var backButton = GetNode<Button>("MarginContainer/VBox/BackButton");
backButton.Pressed += OnBackPressed;
var inventoryManager = GetNode<InventoryManager>("/root/InventoryManager");
foreach (var kvp in inventoryManager.GetAllItems())
{
AddItem(kvp.Key, kvp.Value);
}
}
private void AddItem(string itemId, int qty)
{
var label = new Label();
label.Text = $"{itemId}: {qty}";
_itemList.AddChild(label);
}
private void UpdateCoins(int amount)
{
_coinsLabel.Text = "Inventário - Moedas: " + amount;
}
private void OnBackPressed()
{
GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
}
}
