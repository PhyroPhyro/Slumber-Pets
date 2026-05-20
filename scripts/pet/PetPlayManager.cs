using Godot;
using System;
public partial class PetPlayManager : Node
{
    [Signal] public delegate void OnPlaySessionStartedEventHandler();
    [Signal] public delegate void OnPlaySessionEndedEventHandler(int totalScore, float happinessGained);
    [Signal] public delegate void OnScoreChangedEventHandler(int newScore);
    [Signal] public delegate void OnTimeRemainingChangedEventHandler(float timeRemaining);
    [Signal] public delegate void OnCooldownChangedEventHandler(float cooldownRemaining);

    private const float SessionDurationSeconds = 15f;
    private const float CooldownSeconds = 60f;
    private const float HappinessPerPoint = 0.5f;
    private const float MinComboMultiplier = 1f;
    private const float MaxComboMultiplier = 3f;
    private const float ComboWindowSeconds = 1.5f;

    private bool _isPlaying;
    private float _timeRemaining;
    private int _score;
    private float _cooldownRemaining;
    private int _combo;
    private float _comboTimer;
    private float _lastHitTime;
    private Timer _cooldownTimer;

    public bool IsPlaying => _isPlaying;
    public bool IsOnCooldown => _cooldownRemaining > 0;
    public float CooldownRemaining => _cooldownRemaining;
    public int Score => _score;
    public int Combo => _combo;
    public float TimeRemaining => _timeRemaining;

    public override void _Ready()
    {
        _cooldownTimer = new Timer();
        _cooldownTimer.WaitTime = 0.1f;
        _cooldownTimer.OneShot = false;
        _cooldownTimer.Timeout += OnCooldownTick;
        AddChild(_cooldownTimer);
        _cooldownTimer.Start();

        GD.Print("[PetPlayManager] Initialized. Session: ", SessionDurationSeconds, "s, Cooldown: ", CooldownSeconds, "s");
    }

    public bool StartSession()
    {
        if (_isPlaying)
        {
            GD.Print("[PetPlayManager] Already playing");
            return false;
        }
        if (_cooldownRemaining > 0)
        {
            GD.Print("[PetPlayManager] On cooldown: ", _cooldownRemaining, "s remaining");
            return false;
        }
        _isPlaying = true;
        _timeRemaining = SessionDurationSeconds;
        _score = 0;
        _combo = 0;
        _comboTimer = 0;
        _lastHitTime = 0;

        EmitSignal(SignalName.OnPlaySessionStarted);
        EmitSignal(SignalName.OnScoreChanged, 0);
        EmitSignal(SignalName.OnTimeRemainingChanged, _timeRemaining);
        GD.Print("[PetPlayManager] Play session started");
        return true;
    }

    public void RegisterHit(bool isPerfect)
    {
        if (!_isPlaying) return;

        float now = Time.GetTicksMsec() / 1000.0f;
        float timeSinceLastHit = now - _lastHitTime;

        if (timeSinceLastHit < ComboWindowSeconds)
        {
            _combo++;
        }
        else
        {
            _combo = 1;
        }
        _lastHitTime = Time.GetTicksMsec() / 1000.0f;
        _comboTimer = ComboWindowSeconds;

        float comboMultiplier = Mathf.Lerp(MinComboMultiplier, MaxComboMultiplier, Mathf.Clamp(_combo / 10f, 0, 1));
        int basePoints = isPerfect ? 10 : 5;
        int points = Mathf.RoundToInt(basePoints * comboMultiplier);

        _score += points;
        EmitSignal(SignalName.OnScoreChanged, _score);
        GD.Print("[PetPlayManager] Hit! Combo: ", _combo, "x, Multiplier: ", comboMultiplier.ToString("F1"), " Points: +", points, " Total: ", _score);
    }

    public void UpdateTick(float delta)
    {
        if (!_isPlaying) return;

        _timeRemaining -= delta;
        _comboTimer -= delta;
        if (_comboTimer <= 0)
        {
            _combo = 0;
        }

        EmitSignal(SignalName.OnTimeRemainingChanged, Mathf.Max(0, _timeRemaining));

        if (_timeRemaining <= 0)
        {
            EndSession();
        }
    }

    private void EndSession()
    {
        _isPlaying = false;
        float happinessGained = _score * HappinessPerPoint;
        int finalScore = _score;

        var petNeeds = GetNode<PetNeeds>("/root/PetNeeds");
        petNeeds.Play(happinessGained);

        _cooldownRemaining = CooldownSeconds;

        EmitSignal(SignalName.OnPlaySessionEnded, finalScore, happinessGained);
        GD.Print("[PetPlayManager] Session ended. Score: ", finalScore, " Happiness: +", happinessGained.ToString("F1"), " Cooldown: ", CooldownSeconds, "s");
    }

    private void OnCooldownTick()
    {
        if (_cooldownRemaining > 0)
        {
            _cooldownRemaining = Mathf.Max(0, _cooldownRemaining - 0.1f);
            EmitSignal(SignalName.OnCooldownChanged, _cooldownRemaining);
        }
    }

    public string GetComboText()
    {
        if (_combo <= 1) return "";
        return _combo + "x COMBO!";
    }

    public string GetHitQualityText(bool isPerfect)
    {
        return isPerfect ? "PERFEITO!" : "BOM!";
    }
}
