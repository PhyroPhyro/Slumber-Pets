using Godot;
using System;
using System.Collections.Generic;
public partial class ShopScene : Control
{
private VBoxContainer _itemList;
private Label _coinsLabel;
public override void _Ready()
{
_itemList = GetNode<VBoxContainer>("MarginContainer/VBox/ItemList");
_coinsLabel = GetNode<Label>("MarginContainer/VBox/Header/CoinsLabel");
var resourceManager = GetNode<ResourceManager>("/root/ResourceManager");
resourceManager.OnStarCoinsChanged += UpdateCoins;
UpdateCoins(resourceManager.StarCoins);
var shopManager = GetNode<ShopManager>("/root/ShopManager");
foreach (var item in shopManager.ShopItems)
{
AddShopItem(item);
}
var backButton = GetNode<Button>("MarginContainer/VBox/BackButton");
backButton.Pressed += OnBackButtonPressed;
}
private void AddShopItem(ShopItem item)
{
var button = new Button();
button.Text = $"{item.Name} - {item.Price} moedas";
button.Pressed += () => OnItemPressed(item.Id);
_itemList.AddChild(button);
}
private void OnItemPressed(string itemId)
{
var shopManager = GetNode<ShopManager>("/root/ShopManager");
shopManager.PurchaseItem(itemId);
}
private void UpdateCoins(int amount)
{
_coinsLabel.Text = "Moedas: " + amount;
}
private void OnBackButtonPressed()
{
GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
}
}