using Godot;
using System;
public class PetState
{
public float Hunger { get; set; } = 100f;
public float Happiness { get; set; } = 100f;
public float Energy { get; set; } = 100f;
public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
public PetState()
{
}
public PetState(float hunger, float happiness, float energy, DateTime lastUpdatedAt)
{
Hunger = Mathf.Clamp(hunger, 0f, 100f);
Happiness = Mathf.Clamp(happiness, 0f, 100f);
Energy = Mathf.Clamp(energy, 0f, 100f);
LastUpdatedAt = lastUpdatedAt;
}
public void ApplyOfflineDecay(TimeSpan elapsed, float hungerDecayPerHour = 10f, float happinessDecayPerHour = 5f, float energyDecayPerHour = 15f)
{
double hours = elapsed.TotalHours;
Hunger = Mathf.Clamp(Hunger - (hungerDecayPerHour * (float)hours), 0f, 100f);
Happiness = Mathf.Clamp(Happiness - (happinessDecayPerHour * (float)hours), 0f, 100f);
Energy = Mathf.Clamp(Energy - (energyDecayPerHour * (float)hours), 0f, 100f);
LastUpdatedAt = DateTime.UtcNow;
}
public PetState Clone()
{
return new PetState(Hunger, Happiness, Energy, LastUpdatedAt);
}
}