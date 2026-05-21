using Godot;
using System;
public class PetState
{
public string Name { get; set; } = "Pet";
public float Hunger { get; set; } = 100f;
public float Happiness { get; set; } = 100f;
public float Energy { get; set; } = 100f;
public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
public PetState()
{
}
public PetState(string name, float hunger, float happiness, float energy, DateTime lastUpdatedAt)
{
Name = string.IsNullOrWhiteSpace(name) ? "Pet" : name;
Hunger = Mathf.Clamp(hunger, 0f, 100f);
Happiness = Mathf.Clamp(happiness, 0f, 100f);
Energy = Mathf.Clamp(energy, 0f, 100f);
LastUpdatedAt = lastUpdatedAt;
}
public void ApplyOfflineDecay(TimeSpan elapsed, float hungerDecayPerHour = 8f, float happinessDecayPerHour = 4f, float energyDecayPerHour = 12f)
{
double hours = elapsed.TotalHours;
bool isOffline = elapsed.TotalSeconds > 120f;
float mult = isOffline ? 2.0f : 1.0f;
Hunger = Mathf.Clamp(Hunger - (hungerDecayPerHour * (float)hours * mult), 0f, 100f);
Happiness = Mathf.Clamp(Happiness - (happinessDecayPerHour * (float)hours * mult), 0f, 100f);
Energy = Mathf.Clamp(Energy - (energyDecayPerHour * (float)hours * mult), 0f, 100f);
LastUpdatedAt = DateTime.UtcNow;
}
public PetState Clone()
{
return new PetState(Name, Hunger, Happiness, Energy, LastUpdatedAt);
}
}