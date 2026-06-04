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
    private Label _petStateLabel;
    private ColorRect _petPlaceholder;
    private Button _playButton;
    private Button _shopButton;
    private Button _inventoryButton;
    private Button _historyButton;
    private Button _sendPetButton;
    private Button _receivePetButton;
    private Button _toolsButton;
    private PetPlayManager _playManager;
    private PetNeeds _petNeeds;
    private ResourceManager _resourceManager;
    private IdleIncomeManager _idleIncomeManager;
    private FirebaseAuthManager _authManager;
    private PetSpriteAnimator _spriteAnimator;

    public override void _Ready()
    {
        _hungerLabel = GetNode<Label>("HUD/AttributesPanel/Attributes/HungerLabel");
        _happinessLabel = GetNode<Label>("HUD/AttributesPanel/Attributes/HappinessLabel");
        _energyLabel = GetNode<Label>("HUD/AttributesPanel/Attributes/EnergyLabel");
        _coinsLabel = GetNode<Label>("HUD/TopBar/CoinsLabel");
        _coinsPerMinLabel = GetNode<Label>("HUD/TopBar/CoinsPerMinLabel");
        _sessionCoinsLabel = GetNode<Label>("HUD/TopBar/SessionCoinsLabel");
        _firebaseStatusLabel = GetNode<Label>("HUD/TopBar/FirebaseStatusLabel");
        _statusLabel = GetNode<Label>("HUD/StatusLabel");
        _petStateLabel = GetNode<Label>("HUD/PetArea/PetStateLabel");
        _petPlaceholder = GetNode<ColorRect>("HUD/PetArea/PetPlaceholder");
        _playButton = GetNode<Button>("HUD/BottomBar/PlayButton");
        _shopButton = GetNode<Button>("HUD/BottomBar/ShopButton");
        _inventoryButton = GetNode<Button>("HUD/BottomBar/InventoryButton");
        _historyButton = GetNode<Button>("HUD/BottomBar/HistoryButton");
        _sendPetButton = GetNode<Button>("HUD/BottomBar/SendPetButton");
        _receivePetButton = GetNode<Button>("HUD/BottomBar/ReceivePetButton");
        _toolsButton = GetNode<Button>("HUD/BottomBar/ToolsButton");

        _spriteAnimator = GetNode<PetSpriteAnimator>("/root/PetSpriteAnimator");
        _spriteAnimator.SetAnimationMode(PetSpriteAnimator.AnimationMode.Texture);
        _spriteAnimator.OnStateChanged += OnPetStateChanged;

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

    public override void _ExitTree()
    {
        if (_petNeeds != null)
        {
            _petNeeds.OnHungerChanged -= UpdateHunger;
            _petNeeds.OnHappinessChanged -= UpdateHappiness;
            _petNeeds.OnEnergyChanged -= UpdateEnergy;
        }
        if (_resourceManager != null)
            _resourceManager.OnStarCoinsChanged -= UpdateCoins;
        if (_idleIncomeManager != null)
            _idleIncomeManager.OnCoinsPerMinuteChanged -= UpdateCoinsPerMin;
        if (_playManager != null)
            _playManager.OnCooldownChanged -= UpdatePlayButton;
        if (_authManager != null)
        {
            _authManager.OnSessionRestored -= OnSessionRestored;
            _authManager.OnLoginSuccess -= OnAuthChanged;
            _authManager.OnLogout -= OnAuthChanged;
            _authManager.OnTokenRefreshed -= OnAuthChanged;
        }
        if (_spriteAnimator != null)
            _spriteAnimator.OnStateChanged -= OnPetStateChanged;
    }

    private void OnPetStateChanged(PetSpriteState state)
    {
        if (!IsInstanceValid(_petStateLabel)) return;
        _petStateLabel.Text = _spriteAnimator.GetStateDescription(state);
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
        if (!IsInstanceValid(_playButton)) return;
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
        _petNeeds = GetNode<PetNeeds>("/root/PetNeeds");
        _resourceManager = GetNode<ResourceManager>("/root/ResourceManager");
        _idleIncomeManager = GetNode<IdleIncomeManager>("/root/IdleIncomeManager");

        _petNeeds.OnHungerChanged += UpdateHunger;
        _petNeeds.OnHappinessChanged += UpdateHappiness;
        _petNeeds.OnEnergyChanged += UpdateEnergy;
        _resourceManager.OnStarCoinsChanged += UpdateCoins;
        _idleIncomeManager.OnCoinsPerMinuteChanged += UpdateCoinsPerMin;

        var gameManager = GetNode<GameManager>("/root/GameManager");
        _petNeeds.Initialize(gameManager.GetPetState());

        UpdateHunger(_petNeeds.GetHunger());
        UpdateHappiness(_petNeeds.GetHappiness());
        UpdateEnergy(_petNeeds.GetEnergy());
        UpdateCoins(_resourceManager.StarCoins);
        UpdateCoinsPerMin(_idleIncomeManager.MoedasPorMinutoAtual);
        UpdateSessionCoins(_idleIncomeManager.SessionTotalCoins);
        _statusLabel.Text = GetPetNameText() + " — Em Casa";
    }

    private void ShowPetAwayState()
    {
        _hungerLabel.Text = "-";
        _happinessLabel.Text = "-";
        _energyLabel.Text = "-";
        _coinsLabel.Text = "0";
        _coinsPerMinLabel.Text = "0.0/min";
        _sessionCoinsLabel.Text = "Sessão: 0";
        _statusLabel.Text = GetPetNameText() + " — Passeando...";
    }

    private void UpdateHunger(float value)
    {
        if (!IsInstanceValid(_hungerLabel)) return;
        _hungerLabel.Text = Mathf.RoundToInt(value).ToString();
    }

    private void UpdateHappiness(float value)
    {
        if (!IsInstanceValid(_happinessLabel)) return;
        _happinessLabel.Text = Mathf.RoundToInt(value).ToString();
    }

    private void UpdateEnergy(float value)
    {
        if (!IsInstanceValid(_energyLabel)) return;
        _energyLabel.Text = Mathf.RoundToInt(value).ToString();
    }

    private void UpdateCoins(int amount)
    {
        if (!IsInstanceValid(_coinsLabel)) return;
        _coinsLabel.Text = amount.ToString();
    }

    private void UpdateCoinsPerMin(float rate)
    {
        if (!IsInstanceValid(_coinsPerMinLabel)) return;
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
        if (!IsInstanceValid(_sessionCoinsLabel)) return;
        _sessionCoinsLabel.Text = "Sessão: " + amount;
    }

    private void UpdateFirebaseStatus()
    {
        if (!IsInstanceValid(_firebaseStatusLabel)) return;
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

    private string GetPetNameText()
    {
        var gameManager = GetNode<GameManager>("/root/GameManager");
        return gameManager.GetPetState().Name;
    }
}
