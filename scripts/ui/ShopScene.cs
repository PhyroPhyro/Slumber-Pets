using Godot;
using System;
using System.Collections.Generic;
public partial class ShopScene : Control
{
private VBoxContainer _itemList;
private Label _coinsLabel;
private Label _feedbackLabel;
private ShopManager _shopManager;
private ResourceManager _resourceManager;
private Dictionary<string, Button> _itemButtons = new();

public override void _Ready()
{
_itemList = GetNode<VBoxContainer>("MarginContainer/VBox/ItemList");
_coinsLabel = GetNode<Label>("MarginContainer/VBox/Header/CoinsLabel");
_feedbackLabel = GetNode<Label>("MarginContainer/VBox/FeedbackLabel");
_feedbackLabel.Text = "";

_resourceManager = GetNode<ResourceManager>("/root/ResourceManager");
_resourceManager.OnStarCoinsChanged += UpdateCoins;
UpdateCoins(_resourceManager.StarCoins);

_shopManager = GetNode<ShopManager>("/root/ShopManager");
_shopManager.OnItemPurchased += OnItemPurchased;
foreach (var item in _shopManager.ShopItems)
{
AddShopItem(item);
}
var backButton = GetNode<Button>("MarginContainer/VBox/BackButton");
backButton.Pressed += OnBackButtonPressed;
}

public override void _ExitTree()
{
if (_resourceManager != null)
_resourceManager.OnStarCoinsChanged -= UpdateCoins;
if (_shopManager != null)
_shopManager.OnItemPurchased -= OnItemPurchased;
}

private void AddShopItem(ShopItem item)
{
var hBox = new HBoxContainer();
hBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

var button = new Button();
button.Text = $"{item.Name}";
button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
button.Pressed += () => OnItemPressed(item.Id);
hBox.AddChild(button);

var priceLabel = new Label();
priceLabel.Text = $"{item.Price}";
priceLabel.SizeFlagsHorizontal = 0;
priceLabel.HorizontalAlignment = HorizontalAlignment.Right;
priceLabel.CustomMinimumSize = new Vector2(60, 0);
hBox.AddChild(priceLabel);

var descLabel = new Label();
string desc = "";
if (item.HungerRestore > 0) desc += $"Fome +{item.HungerRestore}";
if (item.HappinessRestore > 0)
{
if (desc.Length > 0) desc += " | ";
desc += $"Feliz +{item.HappinessRestore}";
}
descLabel.Text = desc;
descLabel.SizeFlagsHorizontal = 0;
descLabel.HorizontalAlignment = HorizontalAlignment.Right;
descLabel.CustomMinimumSize = new Vector2(100, 0);
descLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
hBox.AddChild(descLabel);

_itemList.AddChild(hBox);
_itemButtons[item.Id] = button;
UpdateButtonState(item.Id);
}

private void UpdateButtonState(string itemId)
{
if (!_itemButtons.ContainsKey(itemId)) return;
var item = _shopManager.GetItem(itemId);
var button = _itemButtons[itemId];
bool canAfford = _resourceManager.HasEnough(item.Price);
button.Disabled = !canAfford;
if (!canAfford)
{
button.Modulate = new Color(0.4f, 0.4f, 0.4f);
}
else
{
button.Modulate = new Color(1, 1, 1);
}
}

private void OnItemPressed(string itemId)
{
var item = _shopManager.GetItem(itemId);
if (!_resourceManager.HasEnough(item.Price))
{
ShowFeedback("Moedas insuficientes!", new Color(1, 0.3f, 0.3f));
return;
}
_shopManager.PurchaseItem(itemId);
}

private void OnItemPurchased(string itemId)
{
if (!IsInstanceValid(_feedbackLabel)) return;
var item = _shopManager.GetItem(itemId);
ShowFeedback($"Comprou {item.Name}!", new Color(0.3f, 1, 0.3f));
foreach (var kvp in _itemButtons)
{
UpdateButtonState(kvp.Key);
}
}

private void ShowFeedback(string message, Color color)
{
_feedbackLabel.Text = message;
_feedbackLabel.AddThemeColorOverride("font_color", color);
var timer = GetTree().CreateTimer(2f);
timer.Timeout += () =>
{
if (IsInstanceValid(_feedbackLabel))
_feedbackLabel.Text = "";
};
}

private void UpdateCoins(int amount)
{
if (!IsInstanceValid(_coinsLabel)) return;
_coinsLabel.Text = "Moedas: " + amount;
foreach (var kvp in _itemButtons)
{
UpdateButtonState(kvp.Key);
}
}

private void OnBackButtonPressed()
{
GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
}
}