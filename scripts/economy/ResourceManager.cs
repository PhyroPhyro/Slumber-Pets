using Godot;
using System;
using System.Collections.Generic;
public partial class ResourceManager : Node
{
[Signal] public delegate void OnStarCoinsChangedEventHandler(int newAmount);
public int StarCoins { get; private set; }
public override void _Ready()
{
var gameManager = GetNode<GameManager>("/root/GameManager");
StarCoins = gameManager.GetStarCoins();
GD.Print("[ResourceManager] Initialized with ", StarCoins, " star coins");
}
public void AddCoins(int amount)
{
if (amount <= 0) return;
StarCoins += amount;
var gameManager = GetNode<GameManager>("/root/GameManager");
gameManager.AddStarCoins(amount);
EmitSignal(SignalName.OnStarCoinsChanged, StarCoins);
GD.Print("[ResourceManager] Added ", amount, " coins. Total: ", StarCoins);
}
public bool SpendCoins(int amount)
{
if (amount <= 0) return true;
if (StarCoins < amount)
{
GD.Print("[ResourceManager] Not enough coins. Have: ", StarCoins, ", Need: ", amount);
return false;
}
StarCoins -= amount;
var gameManager = GetNode<GameManager>("/root/GameManager");
gameManager.SpendStarCoins(amount);
EmitSignal(SignalName.OnStarCoinsChanged, StarCoins);
GD.Print("[ResourceManager] Spent ", amount, " coins. Remaining: ", StarCoins);
return true;
}
public bool HasEnough(int amount)
{
return StarCoins >= amount;
}
public void SyncFromSave()
{
var gameManager = GetNode<GameManager>("/root/GameManager");
StarCoins = gameManager.GetStarCoins();
EmitSignal(SignalName.OnStarCoinsChanged, StarCoins);
}
}