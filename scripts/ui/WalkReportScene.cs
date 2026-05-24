using Godot;
using System;
using System.Collections.Generic;

public partial class WalkReportScene : Control
{
    private static WalkReportData _pendingReport;

    private Label _stepsLabel;
    private Label _milestonesLabel;
    private Label _durationLabel;
    private VBoxContainer _itemsList;
    private Label _noItemsLabel;
    private Button _returnButton;
    private ItemsData _itemsCache;

    public static void SetPendingReport(WalkReportData report)
    {
        _pendingReport = report;
    }

    public override void _Ready()
    {
        _stepsLabel = GetNode<Label>("VBox/Summary/StepsLabel");
        _milestonesLabel = GetNode<Label>("VBox/Summary/MilestonesLabel");
        _durationLabel = GetNodeOrNull<Label>("VBox/Summary/DurationLabel");
        _itemsList = GetNode<VBoxContainer>("VBox/ItemsListScroll/ItemsList");
        _noItemsLabel = GetNode<Label>("VBox/NoItemsLabel");
        _returnButton = GetNode<Button>("VBox/ReturnButton");

        _returnButton.Pressed += OnReturnPressed;

        CacheItems();

        if (_pendingReport == null)
        {
            GD.PrintErr("[WalkReportScene] No pending report, returning to main");
            GetTree().ChangeSceneToFile("res://scenes/main_mobile.tscn");
            return;
        }

        PopulateReport(_pendingReport);
        _pendingReport = null;

        AnimateEntrance();

        GD.Print("[WalkReportScene] Report displayed");
    }

    private void PopulateReport(WalkReportData report)
    {
        _stepsLabel.Text = report.Steps + " passos";
        _milestonesLabel.Text = report.MilestonesReached + " marco(s) atingido(s)";

        if (_durationLabel != null && report.DurationMin > 0)
        {
            int hours = report.DurationMin / 60;
            int mins = report.DurationMin % 60;
            _durationLabel.Text = hours > 0 ? $"{hours}h {mins}min de passeio" : $"{mins}min de passeio";
            _durationLabel.Visible = true;
        }
        else if (_durationLabel != null)
        {
            _durationLabel.Visible = false;
        }

        if (report.ItemsFound == null || report.ItemsFound.Count == 0)
        {
            _noItemsLabel.Visible = true;
            _itemsList.Visible = false;
        }
        else
        {
            _noItemsLabel.Visible = false;
            _itemsList.Visible = true;

            foreach (var itemData in report.ItemsFound)
            {
                string itemName = GetItemName(itemData.Id);
                string rarity = GetRarityLabel(itemData.Id);
                Color rarityColor = GetRarityColor(rarity);

                var hbox = new HBoxContainer();
                hbox.AddThemeConstantOverride("separation", 4);

                var nameLabel = new Label();
                nameLabel.Text = itemName;
                nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

                var rarityLabel = new Label();
                rarityLabel.Text = rarity;
                rarityLabel.LabelSettings = new LabelSettings { FontColor = rarityColor };
                rarityLabel.HorizontalAlignment = HorizontalAlignment.Right;

                hbox.AddChild(nameLabel);
                hbox.AddChild(rarityLabel);

                _itemsList.AddChild(hbox);
            }
        }
    }

    private void CacheItems()
    {
        string path = "res://data/items.json";
        if (!FileAccess.FileExists(path)) return;

        try
        {
            using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            _itemsCache = JsonSerialization.FromJson<ItemsData>(file.GetAsText());
        }
        catch { }
    }

    private string GetItemName(string itemId)
    {
        if (_itemsCache?.Items != null)
        {
            var item = _itemsCache.Items.Find(i => i.Id == itemId);
            if (item != null) return item.Name;
        }
        return itemId;
    }

    private string GetRarityLabel(string itemId)
    {
        if (_itemsCache?.Items != null)
        {
            var item = _itemsCache.Items.Find(i => i.Id == itemId);
            if (item != null) return FormatRarity(item.Rarity);
        }
        return "";
    }

    private string FormatRarity(string rarity)
    {
        return rarity switch
        {
            "common" => "Comum",
            "common+" => "Comum+",
            "uncommon" => "Incomum",
            "uncommon+" => "Incomum+",
            "rare" => "Raro",
            "rare+" => "Raro+",
            "exclusive" => "Exclusivo",
            _ => rarity
        };
    }

    private Color GetRarityColor(string rarity)
    {
        return rarity switch
        {
            "Comum" => new Color(0.7f, 0.7f, 0.7f),
            "Comum+" => new Color(0.5f, 0.8f, 0.5f),
            "Incomum" => new Color(0.3f, 0.7f, 1.0f),
            "Incomum+" => new Color(0.5f, 0.5f, 1.0f),
            "Raro" => new Color(1.0f, 0.6f, 0.0f),
            "Raro+" => new Color(1.0f, 0.3f, 0.8f),
            "Exclusivo" => new Color(1.0f, 0.9f, 0.2f),
            _ => new Color(1, 1, 1)
        };
    }

    private void OnReturnPressed()
    {
        GetTree().ChangeSceneToFile("res://scenes/main_mobile.tscn");
    }

    private void AnimateEntrance()
    {
        var vbox = GetNodeOrNull<VBoxContainer>("VBox");
        if (vbox == null) return;

        vbox.Modulate = new Color(1, 1, 1, 0);
        vbox.Position = new Vector2(0, 30);

        var tween = GetTree().CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(vbox, "modulate:a", 1.0f, 0.4f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(vbox, "position:y", 0f, 0.4f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
    }
}
