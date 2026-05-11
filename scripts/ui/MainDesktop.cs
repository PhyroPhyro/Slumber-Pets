using Godot;
using System;
public partial class MainDesktop : Node
{
private Label _hungerLabel;
private Label _happinessLabel;
private Label _energyLabel;
private Label _coinsLabel;
private Label _coinsPerMinLabel;
private Label _sessionCoinsLabel;
private Label _statusLabel;
private Button _shopButton;
private Button _inventoryButton;
private Button _historyButton;
private Button _sendPetButton;
public override void _Ready()
{
_hungerLabel = GetNode<Label>("HUD/Attributes/HungerLabel");
_happinessLabel = GetNode<Label>("HUD/Attributes/HappinessLabel");
_energyLabel = GetNode<Label>("HUD/Attributes/EnergyLabel");
_coinsLabel = GetNode<Label>("HUD/TopBar/CoinsLabel");
_coinsPerMinLabel = GetNode<Label>("HUD/TopBar/CoinsPerMinLabel");
_sessionCoinsLabel = GetNode<Label>("HUD/TopBar/SessionCoinsLabel");
_statusLabel = GetNode<Label>("HUD/StatusLabel");
_shopButton = GetNode<Button>("HUD/BottomBar/ShopButton");
_inventoryButton = GetNode<Button>("HUD/BottomBar/InventoryButton");
_historyButton = GetNode<Button>("HUD/BottomBar/HistoryButton");
_sendPetButton = GetNode<Button>("HUD/BottomBar/SendPetButton");
_shopButton.Pressed += OnShopPressed;
_inventoryButton.Pressed += OnInventoryPressed;
_historyButton.Pressed += OnHistoryPressed;
_sendPetButton.Pressed += OnSendPetPressed;
var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
if (petLocationManager.CurrentLocation == PetLocation.Mobile)
{
ShowPetAwayState();
return;
}
InitializeHUD();
}
private void OnShopPressed()
{
GetTree().ChangeSceneToFile("res://scenes/shop/shop.tscn");
}
private void OnInventoryPressed()
{
GetTree().ChangeSceneToFile("res://scenes/ui/inventory.tscn");
}
private void OnHistoryPressed()
{
GetTree().ChangeSceneToFile("res://scenes/ui/history.tscn");
}
private void OnSendPetPressed()
{
GetTree().ChangeSceneToFile("res://scenes/transfer/pet_send.tscn");
}
private void InitializeHUD()
{
var petNeeds = GetNode<PetNeeds>("/root/PetNeeds");
var resourceManager = GetNode<ResourceManager>("/root/ResourceManager");
var idleIncomeManager = GetNode<IdleIncomeManager>("/root/IdleIncomeManager");
petNeeds.OnHungerChanged += UpdateHunger;
petNeeds.OnHappinessChanged += UpdateHappiness;
petNeeds.OnEnergyChanged += UpdateEnergy;
resourceManager.OnStarCoinsChanged += UpdateCoins;
idleIncomeManager.OnCoinsPerMinuteChanged += UpdateCoinsPerMin;
var gameManager = GetNode<GameManager>("/root/GameManager");
petNeeds.Initialize(gameManager.GetPetState());
UpdateHunger(petNeeds.GetHunger());
UpdateHappiness(petNeeds.GetHappiness());
UpdateEnergy(petNeeds.GetEnergy());
UpdateCoins(resourceManager.StarCoins);
UpdateCoinsPerMin(idleIncomeManager.MoedasPorMinutoAtual);
UpdateSessionCoins(idleIncomeManager.SessionTotalCoins);
_statusLabel.Text = "Pet em Casa";
}
private void ShowPetAwayState()
{
_hungerLabel.Text = "-";
_happinessLabel.Text = "-";
_energyLabel.Text = "-";
_coinsLabel.Text = "0";
_coinsPerMinLabel.Text = "0.0/min";
_sessionCoinsLabel.Text = "Sessão: 0";
_statusLabel.Text = "Pet está passeando...";
}
private void UpdateHunger(float value)
{
_hungerLabel.Text = Mathf.RoundToInt(value).ToString();
}
private void UpdateHappiness(float value)
{
_happinessLabel.Text = Mathf.RoundToInt(value).ToString();
}
private void UpdateEnergy(float value)
{
_energyLabel.Text = Mathf.RoundToInt(value).ToString();
}
private void UpdateCoins(int amount)
{
_coinsLabel.Text = amount.ToString();
}
private void UpdateCoinsPerMin(float rate)
{
_coinsPerMinLabel.Text = rate.ToString("F1") + "/min";
}
private void UpdateSessionCoins(int amount)
{
_sessionCoinsLabel.Text = "Sessão: " + amount;
}
}