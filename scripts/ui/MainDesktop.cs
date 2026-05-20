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
private Label _firebaseStatusLabel;
    private Button _playButton;
    private Button _shopButton;
    private Button _inventoryButton;
    private Button _historyButton;
    private Button _sendPetButton;
    private Button _receivePetButton;
    private Button _toolsButton;
    private PetPlayManager _playManager;
    private FirebaseAuthManager _authManager;
public override void _Ready()
{
_hungerLabel = GetNode<Label>("HUD/Attributes/HungerLabel");
_happinessLabel = GetNode<Label>("HUD/Attributes/HappinessLabel");
_energyLabel = GetNode<Label>("HUD/Attributes/EnergyLabel");
_coinsLabel = GetNode<Label>("HUD/TopBar/CoinsLabel");
_coinsPerMinLabel = GetNode<Label>("HUD/TopBar/CoinsPerMinLabel");
_sessionCoinsLabel = GetNode<Label>("HUD/TopBar/SessionCoinsLabel");
_firebaseStatusLabel = GetNode<Label>("HUD/TopBar/FirebaseStatusLabel");
_statusLabel = GetNode<Label>("HUD/StatusLabel");
    _playButton = GetNode<Button>("HUD/BottomBar/PlayButton");
    _shopButton = GetNode<Button>("HUD/BottomBar/ShopButton");
    _inventoryButton = GetNode<Button>("HUD/BottomBar/InventoryButton");
    _historyButton = GetNode<Button>("HUD/BottomBar/HistoryButton");
    _sendPetButton = GetNode<Button>("HUD/BottomBar/SendPetButton");
    _receivePetButton = GetNode<Button>("HUD/BottomBar/ReceivePetButton");
    _toolsButton = GetNode<Button>("HUD/BottomBar/ToolsButton");
    _playButton.Pressed += OnPlayPressed;
    _shopButton.Pressed += OnShopPressed;
    _inventoryButton.Pressed += OnInventoryPressed;
    _historyButton.Pressed += OnHistoryPressed;
    _sendPetButton.Pressed += OnSendPetPressed;
    _receivePetButton.Pressed += OnReceivePetPressed;
    _toolsButton.Pressed += OnToolsPressed;
    _playManager = GetNode<PetPlayManager>("/root/PetPlayManager");
    _playManager.OnCooldownChanged += UpdatePlayButton;
    UpdatePlayButton(_playManager.CooldownRemaining);

    _authManager = GetNode<FirebaseAuthManager>("/root/FirebaseAuthManager");
    _authManager.OnSessionRestored += OnSessionRestored;
    _authManager.OnLoginSuccess += OnAuthChanged;
    _authManager.OnLogout += OnAuthChanged;
    _authManager.OnTokenRefreshed += OnAuthChanged;
    UpdateFirebaseStatus();
    var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
    if (petLocationManager.CurrentLocation == PetLocation.Mobile)
    {
        ShowPetAwayState();
        _sendPetButton.Visible = false;
        _receivePetButton.Visible = true;
        return;
    }
    _sendPetButton.Visible = true;
    _receivePetButton.Visible = false;
    InitializeHUD();
}
private void OnPlayPressed()
{
if (_playManager.IsOnCooldown)
{
_statusLabel.Text = "Brincando... aguarde " + Mathf.CeilToInt(_playManager.CooldownRemaining) + "s";
return;
}
GetTree().ChangeSceneToFile("res://scenes/play/play_mode.tscn");
}

private void UpdatePlayButton(float cooldownRemaining)
{
if (cooldownRemaining > 0)
{
_playButton.Text = "Brincar (" + Mathf.CeilToInt(cooldownRemaining) + "s)";
_playButton.Disabled = true;
_playButton.Modulate = new Color(0.5f, 0.5f, 0.5f);
}
else
{
_playButton.Text = "Brincar";
_playButton.Disabled = false;
_playButton.Modulate = new Color(1, 1, 1);
}
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
    private void OnReceivePetPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/transfer/pet_receive_desktop.tscn");
    }

    private void OnToolsPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/ui/tools_menu.tscn");
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
if (rate <= 0f)
{
var petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
if (petLocationManager.CurrentLocation != PetLocation.PC)
{
_coinsPerMinLabel.Text = "Pausado";
return;
}
}
_coinsPerMinLabel.Text = rate.ToString("F1") + "/min";
}
private void UpdateSessionCoins(int amount)
{
_sessionCoinsLabel.Text = "Sessão: " + amount;
}
    private void UpdateFirebaseStatus()
    {
        if (!_authManager.SessionRestoreComplete)
        {
            _firebaseStatusLabel.Text = "●";
            _firebaseStatusLabel.LabelSettings = new LabelSettings { FontColor = new Color(1, 1, 0) };
            _firebaseStatusLabel.TooltipText = "Firebase: Restaurando sessão...";
        }
        else if (_authManager.IsAuthenticated)
        {
            _firebaseStatusLabel.Text = "●";
            _firebaseStatusLabel.LabelSettings = new LabelSettings { FontColor = new Color(0, 1, 0) };
            _firebaseStatusLabel.TooltipText = "Firebase: Conectado (" + _authManager.UserEmail + ")";
        }
        else
        {
            _firebaseStatusLabel.Text = "●";
            _firebaseStatusLabel.LabelSettings = new LabelSettings { FontColor = new Color(1, 0, 0) };
            _firebaseStatusLabel.TooltipText = "Firebase: Desconectado";
        }
    }
    private void OnSessionRestored(bool success)
    {
        CallDeferred(MethodName.UpdateFirebaseStatus);
    }
    private void OnAuthChanged()
    {
        CallDeferred(MethodName.UpdateFirebaseStatus);
    }
}