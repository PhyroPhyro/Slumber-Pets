using Godot;
using System;
using System.Collections.Generic;

public enum PetSpriteState
{
    Idle,
    Happy,
    Sad,
    Sleeping,
    Eating,
    Playing,
    Walking,
    Traveling
}

public partial class PetSpriteAnimator : Node
{
    public enum AnimationMode
    {
        Placeholder,
        Texture,
        AnimatedSprite
    }

    private const float StateCheckInterval = 2.0f;
    private float _stateCheckTimer;
    private PetSpriteState _currentState = PetSpriteState.Idle;
    private AnimationMode _mode = AnimationMode.Placeholder;

    private Dictionary<PetSpriteState, Texture2D> _textures = new();
    private Dictionary<PetSpriteState, SpriteFrames> _animatedFrames = new();
    private Dictionary<PetSpriteState, Color> _placeholderColors = new();

    private AnimatedSprite2D _animatedSprite;
    private Sprite2D _sprite;
    private ColorRect _placeholder;

    public PetSpriteState CurrentState => _currentState;
    public AnimationMode Mode => _mode;

    public event Action<PetSpriteState> OnStateChanged;

    public override void _Ready()
    {
        InitializePlaceholderColors();
        StartStateCheckTimer();
    }

    public override void _Process(double delta)
    {
        _stateCheckTimer -= (float)delta;
        if (_stateCheckTimer <= 0)
        {
            _stateCheckTimer = StateCheckInterval;
            UpdateStateFromAttributes();
        }
    }

    private void InitializePlaceholderColors()
    {
        _placeholderColors[PetSpriteState.Idle] = new Color(0.6f, 0.6f, 0.8f);
        _placeholderColors[PetSpriteState.Happy] = new Color(0.9f, 0.8f, 0.3f);
        _placeholderColors[PetSpriteState.Sad] = new Color(0.4f, 0.4f, 0.6f);
        _placeholderColors[PetSpriteState.Sleeping] = new Color(0.3f, 0.3f, 0.5f);
        _placeholderColors[PetSpriteState.Eating] = new Color(0.8f, 0.6f, 0.3f);
        _placeholderColors[PetSpriteState.Playing] = new Color(0.9f, 0.5f, 0.3f);
        _placeholderColors[PetSpriteState.Walking] = new Color(0.5f, 0.8f, 0.5f);
        _placeholderColors[PetSpriteState.Traveling] = new Color(0.7f, 0.4f, 0.7f);
    }

    private void StartStateCheckTimer()
    {
        _stateCheckTimer = StateCheckInterval;
    }

    public void SetAnimationMode(AnimationMode mode)
    {
        _mode = mode;
        ApplyCurrentState();
    }

    public void SetAnimatedSprite(AnimatedSprite2D animatedSprite)
    {
        _animatedSprite = animatedSprite;
        _mode = AnimationMode.AnimatedSprite;
        ApplyCurrentState();
    }

    public void SetSprite(Sprite2D sprite)
    {
        _sprite = sprite;
        _mode = AnimationMode.Texture;
        ApplyCurrentState();
    }

    public void SetPlaceholder(ColorRect placeholder)
    {
        _placeholder = placeholder;
        _mode = AnimationMode.Placeholder;
        ApplyCurrentState();
    }

    public void RegisterTexture(PetSpriteState state, Texture2D texture)
    {
        _textures[state] = texture;
        if (_currentState == state && _mode == AnimationMode.Texture)
        {
            ApplyCurrentState();
        }
    }

    public void RegisterAnimatedFrames(PetSpriteState state, SpriteFrames frames)
    {
        _animatedFrames[state] = frames;
        if (_currentState == state && _mode == AnimationMode.AnimatedSprite)
        {
            ApplyCurrentState();
        }
    }

    public void ForceState(PetSpriteState state)
    {
        if (_currentState == state) return;
        _currentState = state;
        ApplyCurrentState();
        OnStateChanged?.Invoke(state);
    }

    private void UpdateStateFromAttributes()
    {
        var gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
        if (gameManager == null) return;

        var petState = gameManager.GetPetState();
        if (petState == null) return;

        PetSpriteState newState = DetermineState(petState);

        if (newState != _currentState)
        {
            _currentState = newState;
            ApplyCurrentState();
            OnStateChanged?.Invoke(newState);
        }
    }

    private PetSpriteState DetermineState(PetState petState)
    {
        if (petState.Energy < 20f)
            return PetSpriteState.Sleeping;

        if (petState.Happiness > 80f && petState.Hunger > 60f)
            return PetSpriteState.Happy;

        if (petState.Hunger < 30f || petState.Happiness < 30f)
            return PetSpriteState.Sad;

        return PetSpriteState.Idle;
    }

    private void ApplyCurrentState()
    {
        switch (_mode)
        {
            case AnimationMode.Placeholder:
                ApplyPlaceholderState();
                break;
            case AnimationMode.Texture:
                ApplyTextureState();
                break;
            case AnimationMode.AnimatedSprite:
                ApplyAnimatedSpriteState();
                break;
        }
    }

    private void ApplyPlaceholderState()
    {
        if (_placeholder == null) return;

        if (_placeholderColors.TryGetValue(_currentState, out Color color))
        {
            _placeholder.Color = color;
        }
    }

    private void ApplyTextureState()
    {
        if (_sprite == null) return;

        if (_textures.TryGetValue(_currentState, out Texture2D texture))
        {
            _sprite.Texture = texture;
        }
    }

    private void ApplyAnimatedSpriteState()
    {
        if (_animatedSprite == null) return;

        string animationName = _currentState.ToString().ToLower();

        if (_animatedSprite.SpriteFrames.HasAnimation(animationName))
        {
            _animatedSprite.Play(animationName);
        }
        else if (_animatedSprite.SpriteFrames.HasAnimation("idle"))
        {
            _animatedSprite.Play("idle");
        }
    }

    public Color GetStateColor(PetSpriteState state)
    {
        return _placeholderColors.TryGetValue(state, out Color color) ? color : Colors.White;
    }

    public string GetStateDescription(PetSpriteState state)
    {
        switch (state)
        {
            case PetSpriteState.Idle: return "Tranquilo";
            case PetSpriteState.Happy: return "Feliz!";
            case PetSpriteState.Sad: return "Triste...";
            case PetSpriteState.Sleeping: return "Dormindo";
            case PetSpriteState.Eating: return "Comendo";
            case PetSpriteState.Playing: return "Brincando";
            case PetSpriteState.Walking: return "Passeando";
            case PetSpriteState.Traveling: return "Viajando";
            default: return "";
        }
    }
}
