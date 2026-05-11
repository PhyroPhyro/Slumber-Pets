using Godot;
using System;
using System.Collections.Generic;
public partial class WalkTracker : Node
{
[Signal] public delegate void OnWalkStartedEventHandler();
[Signal] public delegate void OnWalkEndedEventHandler(Variant report);
[Signal] public delegate void OnItemFoundEventHandler(Variant item);
private StepCounter _stepCounter;
private MilestoneManager _milestoneManager;
private ItemRoller _itemRoller;
private bool _isWalking = false;
private List<ItemDrop> _itemsFound = new();
public bool IsWalking => _isWalking;
public int CurrentSteps => _stepCounter.StepsToday;
public override void _Ready()
{
_stepCounter = GetNode<StepCounter>("/root/StepCounter");
_milestoneManager = GetNode<MilestoneManager>("/root/MilestoneManager");
_itemRoller = GetNode<ItemRoller>("/root/ItemRoller");
_stepCounter.OnStepDetected += OnStepDetected;
_stepCounter.OnMilestoneStep += OnMilestoneStep;
_milestoneManager.OnMilestoneReached += OnMilestoneReached;
}
public void StartWalk()
{
if (_isWalking) return;
_isWalking = true;
_itemsFound.Clear();
_stepCounter.StartCounting();
EmitSignal(SignalName.OnWalkStarted);
GD.Print("[WalkTracker] Walk mode started");
}
public void EndWalk()
{
if (!_isWalking) return;
_isWalking = false;
_stepCounter.StopCounting();
int steps = _stepCounter.StepsToday;
int milestonesReached = _milestoneManager.MilestonesReachedToday;
var report = new WalkReportData
{
Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
Steps = steps,
MilestonesReached = milestonesReached,
ItemsFound = new List<ItemData>()
};
foreach (var item in _itemsFound)
{
report.ItemsFound.Add(new ItemData { Id = item.Id, Qty = 1 });
}
EmitSignal(SignalName.OnWalkEnded, JsonSerialization.ToJson(report));
GD.Print("[WalkTracker] Walk ended. Steps: ", steps, ", Items: ", _itemsFound.Count);
}
private void OnStepDetected(int totalSteps)
{
GD.Print("[WalkTracker] Step detected: ", totalSteps);
}
private void OnMilestoneStep(int steps)
{
int milestoneIndex = _milestoneManager.GetMilestoneIndexForSteps(steps);
_milestoneManager.CheckMilestone(steps);
}
private void OnMilestoneReached(int milestoneIndex, int steps)
{
ItemDrop item = _itemRoller.Roll(milestoneIndex);
_itemsFound.Add(item);
var inventoryManager = GetNode<InventoryManager>("/root/InventoryManager");
inventoryManager.AddItem(item.Id, 1);
EmitSignal(SignalName.OnItemFound, JsonSerialization.ToJson(item));
GD.Print("[WalkTracker] Item found at milestone ", milestoneIndex, ": ", item.Name);
}
}