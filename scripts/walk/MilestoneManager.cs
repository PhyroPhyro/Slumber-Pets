using Godot;
using System;
using System.Collections.Generic;
public partial class MilestoneManager : Node
{
[Signal] public delegate void OnMilestoneReachedEventHandler(int milestoneIndex, int steps);
private List<MilestoneData> _milestones = new();
private HashSet<int> _reachedMilestones = new();
private int _milestonesReachedToday = 0;
    public int MilestonesReachedToday => _milestonesReachedToday;
    public List<int> GetMilestoneSteps()
    {
        return _milestones.ConvertAll(m => m.Steps);
    }
    public override void _Ready()
{
LoadMilestones();
GD.Print("[MilestoneManager] Loaded ", _milestones.Count, " milestones");
}
private void LoadMilestones()
{
string path = "res://data/milestones.json";
if (!FileAccess.FileExists(path))
{
GD.PrintErr("[MilestoneManager] milestones.json not found");
return;
}
try
{
using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
string json = file.GetAsText();
var data = JsonSerialization.FromJson<MilestonesFileData>(json);
if (data != null && data.Milestones != null)
{
_milestones = data.Milestones;
}
}
catch (Exception e)
{
GD.PrintErr("[MilestoneManager] Error loading milestones.json: ", e.Message);
}
}
public void CheckMilestone(int steps)
{
foreach (var milestone in _milestones)
{
if (steps >= milestone.Steps && !_reachedMilestones.Contains(milestone.Index))
{
_reachedMilestones.Add(milestone.Index);
_milestonesReachedToday++;
EmitSignal(SignalName.OnMilestoneReached, milestone.Index, steps);
GD.Print("[MilestoneManager] Milestone reached: ", milestone.Index, " at ", steps, " steps");
}
}
}
public int GetMilestoneIndexForSteps(int steps)
{
int index = 0;
for (int i = 0; i < _milestones.Count; i++)
{
if (steps >= _milestones[i].Steps)
{
index = i;
}
}
return index;
}
public void ResetDailyMilestones()
{
_reachedMilestones.Clear();
_milestonesReachedToday = 0;
}
}
public class MilestoneData
{
public int Index { get; set; } = 0;
public int Steps { get; set; } = 0;
public string Rarity { get; set; } = string.Empty;
public int RareChance { get; set; } = 0;
}
public class MilestonesFileData
{
public System.Collections.Generic.List<MilestoneData> Milestones { get; set; } = new();
public int StepIncrementAfterMax { get; set; } = 1500;
}