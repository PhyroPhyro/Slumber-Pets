using Godot;
using System;

public partial class ItemFoundScene : Control
{
    private Label _titleLabel;
    private Label _itemNameLabel;
    private Label _rarityLabel;
    private TextureRect _itemIcon;
    private AnimationPlayer _animationPlayer;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("VBox/TitleLabel");
        _itemNameLabel = GetNode<Label>("VBox/ItemNameLabel");
        _rarityLabel = GetNode<Label>("VBox/RarityLabel");
        _itemIcon = GetNodeOrNull<TextureRect>("VBox/ItemIcon");
        _animationPlayer = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");

        Modulate = new Color(1, 1, 1, 0);
        Visible = false;

        GD.Print("[ItemFoundScene] Initialized");
    }

    public void ShowItem(string itemId, string itemName, string rarity)
    {
        Visible = true;
        _itemNameLabel.Text = itemName;
        _rarityLabel.Text = GetRarityText(rarity);
        _rarityLabel.LabelSettings = new LabelSettings { FontColor = GetRarityColor(rarity) };

        Color bgColor = GetRarityColor(rarity);
        bgColor = new Color(bgColor.R, bgColor.G, bgColor.B, 0.3f);

        if (_animationPlayer != null && _animationPlayer.HasAnimation("show"))
        {
            _animationPlayer.Play("show");
        }
        else
        {
            ShowAnimated();
        }
    }

    private async void ShowAnimated()
    {
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += (float)GetProcessDeltaTime();
            float t = elapsed / duration;
            float alpha = EaseOut(t);
            Modulate = new Color(1, 1, 1, alpha);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        Modulate = new Color(1, 1, 1, 1);

        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
        HideAnimated();
    }

    private async void HideAnimated()
    {
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += (float)GetProcessDeltaTime();
            float t = elapsed / duration;
            float alpha = 1f - EaseIn(t);
            Modulate = new Color(1, 1, 1, alpha);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        Modulate = new Color(1, 1, 1, 0);
        Visible = false;
    }

    private float EaseIn(float t)
    {
        return t * t * t;
    }

    private float EaseOut(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private string GetRarityText(string rarity)
    {
        return rarity.ToLower() switch
        {
            "common" => "Comum",
            "common+" => "Comum+",
            "uncommon" => "Incomum",
            "uncommon+" => "Incomum+",
            "rare" => "Raro",
            "rare+" => "Raro+",
            _ => rarity
        };
    }

    private Color GetRarityColor(string rarity)
    {
        return rarity.ToLower() switch
        {
            "common" => new Color(0.7f, 0.7f, 0.7f),
            "common+" => new Color(0.6f, 0.8f, 0.6f),
            "uncommon" => new Color(0.3f, 0.7f, 0.3f),
            "uncommon+" => new Color(0.2f, 0.8f, 0.9f),
            "rare" => new Color(0.6f, 0.4f, 0.9f),
            "rare+" => new Color(1f, 0.7f, 0.2f),
            _ => Colors.White
        };
    }
}
