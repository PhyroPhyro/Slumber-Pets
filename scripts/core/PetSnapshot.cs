using Godot;
using System;

public class PetSnapshot
{
    public int Version { get; set; } = 1;
    public string TransferId { get; set; } = string.Empty;
    public string PetLocation { get; set; } = "pc";
    public string Status { get; set; } = "pending";
    public DateTime TransferredAt { get; set; } = DateTime.UtcNow;
    public PetState Pet { get; set; } = new();
    public InventoryData Inventory { get; set; } = new();
    public AccumulatedReports AccumulatedReports { get; set; } = new();
}

public class AccumulatedReports
{
    public System.Collections.Generic.List<SleepReportData> SleepReports { get; set; } = new();
    public System.Collections.Generic.List<WalkReportData> WalkReports { get; set; } = new();
}
