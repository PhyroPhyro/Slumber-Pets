using Godot;
using System;
public partial class GameManager : Node
{
private SaveManager _saveManager;
private PetLocationManager _petLocationManager;
private SaveData _currentSave;
public SaveData CurrentSave => _currentSave;
public override void _Ready()
{
GD.Print("[GameManager] Initializing Slumber Pets...");
_saveManager = GetNode<SaveManager>("/root/SaveManager");
_petLocationManager = GetNode<PetLocationManager>("/root/PetLocationManager");
LoadGame();
GD.Print("[GameManager] Game initialized. Pet location: ", _currentSave.PetLocation);
}
    private void LoadGame()
    {
        _currentSave = _saveManager.LoadSave();
        _saveManager.ApplyOfflineDecay(_currentSave);

        bool isNewSave = string.IsNullOrEmpty(_currentSave.LastTransferId) && _currentSave.PetLocation == "pc";

        if (isNewSave && PlatformUtils.IsAndroidBuild())
        {
            PetLocation location = PetLocation.PC;
            _petLocationManager.SetLocation(location);
            GD.Print("[GameManager] Android build detected - pet starts at PC (waiting for transfer)");
        }
        else
        {
            PetLocation location = _currentSave.PetLocation.ToLower() == "mobile" ? PetLocation.Mobile : PetLocation.PC;
            _petLocationManager.SetLocation(location);
        }

        _saveManager.SaveGame(_currentSave);
    }
public void SaveGame()
{
_currentSave.PetLocation = _petLocationManager.CurrentLocation.ToString().ToLower();
_saveManager.SaveGame(_currentSave);
}
public PetState GetPetState()
{
return _currentSave.Pet;
}
public void UpdatePetState(PetState newState)
{
_currentSave.Pet = newState;
SaveGame();
}
public int GetStarCoins()
{
return _currentSave.Inventory.StarCoins;
}
public void AddStarCoins(int amount)
{
_currentSave.Inventory.StarCoins += amount;
SaveGame();
}
public void SpendStarCoins(int amount)
{
if (_currentSave.Inventory.StarCoins >= amount)
{
_currentSave.Inventory.StarCoins -= amount;
SaveGame();
}
else
{
GD.PrintErr("[GameManager] Not enough star coins to spend");
}
}
public bool HasEnoughCoins(int amount)
{
return _currentSave.Inventory.StarCoins >= amount;
}
}