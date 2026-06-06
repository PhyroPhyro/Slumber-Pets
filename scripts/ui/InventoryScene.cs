using Godot;
using System;
using System.Collections.Generic;
public partial class InventoryScene : Control
{
private VBoxContainer _itemList;
private Label _coinsLabel;
private Label _feedbackLabel;
private InventoryManager _inventoryManager;
private ShopManager _shopManager;
private ResourceManager _resourceManager;
private PetNeeds _petNeeds;
private ItemEffectManager _effectManager;

public override void _Ready()
{
        _itemList = GetNode<VBoxContainer>("MarginContainer/VBox/ItemListScroll/ItemList");
_coinsLabel = GetNode<Label>("MarginContainer/VBox/Header/CoinsLabel");
_feedbackLabel = GetNode<Label>("MarginContainer/VBox/FeedbackLabel");
_feedbackLabel.Text = "";

_inventoryManager = GetNode<InventoryManager>("/root/InventoryManager");
_shopManager = GetNode<ShopManager>("/root/ShopManager");
_resourceManager = GetNode<ResourceManager>("/root/ResourceManager");
_petNeeds = GetNode<PetNeeds>("/root/PetNeeds");
_effectManager = GetNode<ItemEffectManager>("/root/ItemEffectManager");

_inventoryManager.OnInventoryChanged += RefreshItemList;
_resourceManager.OnStarCoinsChanged += UpdateCoins;
UpdateCoins(_resourceManager.StarCoins);

var backButton = GetNode<Button>("MarginContainer/VBox/BackButton");
backButton.Pressed += OnBackPressed;

RefreshItemList();
}

public override void _ExitTree()
{
if (_inventoryManager != null)
_inventoryManager.OnInventoryChanged -= RefreshItemList;
if (_resourceManager != null)
_resourceManager.OnStarCoinsChanged -= UpdateCoins;
}

private void RefreshItemList()
{
if (!IsInstanceValid(_itemList)) return;
foreach (Node child in _itemList.GetChildren())
{
child.QueueFree();
}
foreach (var kvp in _inventoryManager.GetAllItems())
{
AddItem(kvp.Key, kvp.Value);
}
}

    private void AddItem(string itemId, int qty)
    {
        if (qty <= 0) return;

        var shopItem = _shopManager.GetItem(itemId);
        string displayName = shopItem != null ? shopItem.Name : itemId;

        var hBox = new HBoxContainer();
        hBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        if (shopItem?.Sprite != null)
        {
            var spriteRect = new TextureRect();
            spriteRect.Texture = shopItem.Sprite;
            spriteRect.CustomMinimumSize = new Vector2(24, 24);
            spriteRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            hBox.AddChild(spriteRect);
        }

        var nameLabel = new Label();
        nameLabel.Text = displayName;
        nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        nameLabel.ClipText = true;
        hBox.AddChild(nameLabel);

        var qtyLabel = new Label();
        qtyLabel.Text = $"x{qty}";
        qtyLabel.CustomMinimumSize = new Vector2(30, 0);
        qtyLabel.HorizontalAlignment = HorizontalAlignment.Center;
        hBox.AddChild(qtyLabel);

        string desc = "";
        if (shopItem != null)
        {
            if (shopItem.HungerRestore > 0) desc += $"F+{shopItem.HungerRestore}";
            if (shopItem.HappinessRestore > 0)
            {
                if (desc.Length > 0) desc += " | ";
                desc += $"Fel+{shopItem.HappinessRestore}";
            }
            if (shopItem.EnergyRestore > 0)
            {
                if (desc.Length > 0) desc += " | ";
                desc += $"E+{shopItem.EnergyRestore}";
            }
            if (shopItem.EffectType != "none")
            {
                if (desc.Length > 0) desc += " | ";
                var effectType = ItemEffectManager.ParseEffectType(shopItem.EffectType);
                desc += _effectManager.GetEffectDescription(effectType);
            }
        }
        var descLabel = new Label();
        descLabel.Text = desc;
        descLabel.CustomMinimumSize = new Vector2(50, 0);
        descLabel.HorizontalAlignment = HorizontalAlignment.Right;
        descLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        hBox.AddChild(descLabel);

        var useButton = new Button();
        useButton.Text = "Usar";
        useButton.CustomMinimumSize = new Vector2(45, 0);
        useButton.Pressed += () => OnUseItemPressed(itemId);
        hBox.AddChild(useButton);

        _itemList.AddChild(hBox);
    }

private void OnUseItemPressed(string itemId)
{
var shopItem = _shopManager.GetItem(itemId);
if (shopItem == null)
{
ShowFeedback("Item desconhecido!", new Color(1, 0.3f, 0.3f));
return;
}

bool used = _inventoryManager.UseItem(itemId);
if (!used)
{
ShowFeedback("Sem itens no inventário!", new Color(1, 0.3f, 0.3f));
return;
}

if (shopItem.HungerRestore > 0 || shopItem.HappinessRestore > 0 || shopItem.EnergyRestore > 0)
{
_petNeeds.Feed(shopItem.HungerRestore, shopItem.HappinessRestore, shopItem.EnergyRestore);
}

if (shopItem.EffectType != "none" && shopItem.EffectDuration > 0)
{
var effectType = ItemEffectManager.ParseEffectType(shopItem.EffectType);
_effectManager.ApplyEffect(effectType, shopItem.EffectValue, shopItem.EffectDuration, itemId);
}

string feedbackMsg = $"Usou {shopItem.Name}!";
if (shopItem.EffectType != "none")
{
var effectType = ItemEffectManager.ParseEffectType(shopItem.EffectType);
feedbackMsg += $" ({_effectManager.GetEffectDescription(effectType)})";
}
ShowFeedback(feedbackMsg, new Color(0.3f, 1, 0.3f));
RefreshItemList();
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
    }

private void OnBackPressed()
{
GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
}
}
