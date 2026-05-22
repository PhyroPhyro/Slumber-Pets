using Godot;
using System;
using System.Collections.Generic;

public partial class WalkReportScene : Control
{
    private static WalkReportData _pendingReport;

    private Label _stepsLabel;
    private Label _milestonesLabel;
    private VBoxContainer _itemsList;
    private Label _noItemsLabel;
    private Button _returnButton;

    public static void SetPendingReport(WalkReportData report)
    {
        _pendingReport = report;
    }

    public override void _Ready()
    {
        _stepsLabel = GetNode<Label>("VBox/Summary/StepsLabel");
        _milestonesLabel = GetNode<Label>("VBox/Summary/MilestonesLabel");
        _itemsList = GetNode<VBoxContainer>("VBox/ItemsList");
        _noItemsLabel = GetNode<Label>("VBox/NoItemsLabel");
        _returnButton = GetNode<Button>("VBox/ReturnButton");

        _returnButton.Pressed += OnReturnPressed;

        if (_pendingReport == null)
        {
            GD.PrintErr("[WalkReportScene] No pending report, returning to main");
            GetTree().ChangeSceneToFile("res://scenes/main_mobile.tscn");
            return;
        }

        PopulateReport(_pendingReport);
        _pendingReport = null;

        GD.Print("[WalkReportScene] Report displayed");
    }

    private void PopulateReport(WalkReportData report)
    {
        _stepsLabel.Text = report.Steps + " passos";
        _milestonesLabel.Text = report.MilestonesReached + " marco(s) atingido(s)";

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
                string rarity = GetRarityLabel(itemData.Id);
                Color rarityColor = GetRarityColor(rarity);

                var hbox = new HBoxContainer();
                hbox.AddThemeConstantOverride("separation", 4);

                var nameLabel = new Label();
                nameLabel.Text = itemData.Id;
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

    private string GetRarityLabel(string itemId)
    {
        string path = "res://data/items.json";
        if (!FileAccess.FileExists(path)) return "";

        try
        {
            using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            var data = JsonSerialization.FromJson<ItemsData>(file.GetAsText());
            if (data != null && data.Items != null)
            {
                var item = data.Items.Find(i => i.Id == itemId);
                if (item != null) return FormatRarity(item.Rarity);
            }
        }
        catch { }

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
}
