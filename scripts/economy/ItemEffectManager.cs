using Godot;
using System;
using System.Collections.Generic;

public enum ItemEffectType
{
    None,
    DecayShield,
    LuckBoost,
    IncomeBoost,
    PlayBoost,
    SleepBoost,
    WalkBoost
}

public class ActiveEffect
{
    public ItemEffectType Type { get; set; }
    public int Value { get; set; }
    public float RemainingTime { get; set; }
    public float TotalDuration { get; set; }
    public string SourceItemId { get; set; } = string.Empty;
}

public partial class ItemEffectManager : Node
{
    [Signal] public delegate void OnEffectAppliedEventHandler(int effectType, int value, float duration);
    [Signal] public delegate void OnEffectExpiredEventHandler(int effectType);
    [Signal] public delegate void OnEffectTickEventHandler(int effectType, float remainingTime);

    private Dictionary<ItemEffectType, ActiveEffect> _activeEffects = new();
    private float _tickTimer;
    private const float TickInterval = 1.0f;

    public override void _Ready()
    {
        _tickTimer = TickInterval;
        GD.Print("[ItemEffectManager] Initialized.");
    }

    public override void _Process(double delta)
    {
        _tickTimer -= (float)delta;
        if (_tickTimer <= 0)
        {
            _tickTimer = TickInterval;
            UpdateEffects();
        }
    }

    public void ApplyEffect(ItemEffectType effectType, int value, float durationSeconds, string sourceItemId = "")
    {
        if (effectType == ItemEffectType.None) return;

        if (_activeEffects.TryGetValue(effectType, out ActiveEffect existing))
        {
            existing.RemainingTime = Math.Max(existing.RemainingTime, durationSeconds);
            existing.Value = Math.Max(existing.Value, value);
            GD.Print($"[ItemEffectManager] Refreshed effect: {effectType} (value={value}, duration={durationSeconds}s)");
        }
        else
        {
            _activeEffects[effectType] = new ActiveEffect
            {
                Type = effectType,
                Value = value,
                RemainingTime = durationSeconds,
                TotalDuration = durationSeconds,
                SourceItemId = sourceItemId
            };
            GD.Print($"[ItemEffectManager] Applied effect: {effectType} (value={value}, duration={durationSeconds}s)");
        }

    EmitSignal(SignalName.OnEffectApplied, (int)effectType, value, durationSeconds);
    }

    public bool HasEffect(ItemEffectType effectType)
    {
        return _activeEffects.ContainsKey(effectType);
    }

    public int GetEffectValue(ItemEffectType effectType)
    {
        return _activeEffects.TryGetValue(effectType, out ActiveEffect effect) ? effect.Value : 0;
    }

    public float GetEffectRemaining(ItemEffectType effectType)
    {
        return _activeEffects.TryGetValue(effectType, out ActiveEffect effect) ? effect.RemainingTime : 0f;
    }

    public float GetEffectProgress(ItemEffectType effectType)
    {
        if (!_activeEffects.TryGetValue(effectType, out ActiveEffect effect) || effect.TotalDuration <= 0)
            return 0f;
        return effect.RemainingTime / effect.TotalDuration;
    }

    public string GetEffectDescription(ItemEffectType effectType)
    {
        switch (effectType)
        {
            case ItemEffectType.DecayShield: return "Escudo contra decaimento";
            case ItemEffectType.LuckBoost: return "Sorte aumentada";
            case ItemEffectType.IncomeBoost: return "Renda passiva aumentada";
            case ItemEffectType.PlayBoost: return "Bônus de brincadeira";
            case ItemEffectType.SleepBoost: return "Bônus de sono";
            case ItemEffectType.WalkBoost: return "Bônus de caminhada";
            default: return "";
        }
    }

    public List<ActiveEffect> GetAllActiveEffects()
    {
        return new List<ActiveEffect>(_activeEffects.Values);
    }

    private void UpdateEffects()
    {
        var expired = new List<ItemEffectType>();

        foreach (var kvp in _activeEffects)
        {
            kvp.Value.RemainingTime -= TickInterval;
            if (kvp.Value.RemainingTime <= 0)
            {
                expired.Add(kvp.Key);
            }
            else
            {
                EmitSignal(SignalName.OnEffectTick, (int)kvp.Key, kvp.Value.RemainingTime);
            }
        }

        foreach (var type in expired)
        {
            GD.Print($"[ItemEffectManager] Effect expired: {type}");
            EmitSignal(SignalName.OnEffectExpired, (int)type);
            _activeEffects.Remove(type);
        }
    }

    public float GetDecayMultiplier()
    {
        if (HasEffect(ItemEffectType.DecayShield))
        {
            int shieldValue = GetEffectValue(ItemEffectType.DecayShield);
            return Mathf.Max(0.1f, 1.0f - (shieldValue / 100f));
        }
        return 1.0f;
    }

    public float GetIncomeMultiplier()
    {
        if (HasEffect(ItemEffectType.IncomeBoost))
        {
            int boostValue = GetEffectValue(ItemEffectType.IncomeBoost);
            return 1.0f + (boostValue / 100f);
        }
        return 1.0f;
    }

    public float GetLuckMultiplier()
    {
        if (HasEffect(ItemEffectType.LuckBoost))
        {
            int boostValue = GetEffectValue(ItemEffectType.LuckBoost);
            return 1.0f + (boostValue / 100f);
        }
        return 1.0f;
    }

    public float GetPlayHappinessMultiplier()
    {
        if (HasEffect(ItemEffectType.PlayBoost))
        {
            int boostValue = GetEffectValue(ItemEffectType.PlayBoost);
            return 1.0f + (boostValue / 100f);
        }
        return 1.0f;
    }

    public float GetSleepQualityMultiplier()
    {
        if (HasEffect(ItemEffectType.SleepBoost))
        {
            int boostValue = GetEffectValue(ItemEffectType.SleepBoost);
            return 1.0f + (boostValue / 100f);
        }
        return 1.0f;
    }

    public float GetWalkStepMultiplier()
    {
        if (HasEffect(ItemEffectType.WalkBoost))
        {
            int boostValue = GetEffectValue(ItemEffectType.WalkBoost);
            return 1.0f + (boostValue / 100f);
        }
        return 1.0f;
    }

    public static ItemEffectType ParseEffectType(string typeString)
    {
        return typeString.ToLower() switch
        {
            "decay_shield" => ItemEffectType.DecayShield,
            "luck_boost" => ItemEffectType.LuckBoost,
            "income_boost" => ItemEffectType.IncomeBoost,
            "play_boost" => ItemEffectType.PlayBoost,
            "sleep_boost" => ItemEffectType.SleepBoost,
            "walk_boost" => ItemEffectType.WalkBoost,
            _ => ItemEffectType.None
        };
    }
}
