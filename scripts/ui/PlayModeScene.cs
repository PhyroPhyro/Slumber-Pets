using Godot;
using System;
public partial class PlayModeScene : Control
{
private PetPlayManager _playManager;
private Label _timerLabel;
private Label _scoreLabel;
private Label _comboLabel;
private Label _feedbackLabel;
private Control _playArea;
private Button _targetButton;
private Button _backButton;
private Panel _resultPanel;
private Label _resultScoreLabel;
private Label _resultHappinessLabel;
private Label _resultComboLabel;
private Button _resultBackButton;

private RandomNumberGenerator _rng;
private float _spawnTimer;
private const float SpawnIntervalSeconds = 1.2f;
private const float TargetLifetimeSeconds = 1.5f;
private float _targetLifetime;
private bool _targetActive;

public override void _Ready()
{
_rng = new RandomNumberGenerator();
_rng.Randomize();

_playManager = GetNode<PetPlayManager>("/root/PetPlayManager");
_timerLabel = GetNode<Label>("HUD/TimerLabel");
_scoreLabel = GetNode<Label>("HUD/ScoreLabel");
_comboLabel = GetNode<Label>("HUD/ComboLabel");
_feedbackLabel = GetNode<Label>("HUD/FeedbackLabel");
_playArea = GetNode<Control>("PlayArea");
_targetButton = GetNode<Button>("PlayArea/TargetButton");
_backButton = GetNode<Button>("HUD/BackButton");

_resultPanel = GetNode<Panel>("ResultPanel");
_resultScoreLabel = GetNode<Label>("ResultPanel/VBox/ScoreLabel");
_resultHappinessLabel = GetNode<Label>("ResultPanel/VBox/HappinessLabel");
_resultComboLabel = GetNode<Label>("ResultPanel/VBox/ComboLabel");
_resultBackButton = GetNode<Button>("ResultPanel/VBox/BackButton");

_targetButton.Visible = false;
_targetButton.Pressed += OnTargetPressed;
_backButton.Pressed += OnBackPressed;
_resultBackButton.Pressed += OnResultBackPressed;

_resultPanel.Visible = false;

_playManager.OnScoreChanged += OnScoreChanged;
_playManager.OnTimeRemainingChanged += OnTimeRemainingChanged;
_playManager.OnPlaySessionStarted += OnSessionStarted;
_playManager.OnPlaySessionEnded += OnSessionEnded;

if (!_playManager.IsPlaying)
{
_playManager.StartSession();
}

_spawnTimer = 0;
_targetLifetime = 0;
_targetActive = false;
}

public override void _Process(double delta)
{
_playManager.UpdateTick((float)delta);

if (_playManager.IsPlaying && !_resultPanel.Visible)
{
_spawnTimer += (float)delta;
if (_spawnTimer >= SpawnIntervalSeconds && !_targetActive)
{
SpawnTarget();
_spawnTimer = 0;
}

if (_targetActive)
{
_targetLifetime += (float)delta;
if (_targetLifetime >= TargetLifetimeSeconds)
{
HideTarget();
}
}
}
}

private void SpawnTarget()
{
float areaWidth = _playArea.Size.X;
float areaHeight = _playArea.Size.Y;
float targetWidth = _targetButton.Size.X;
float targetHeight = _targetButton.Size.Y;

float x = _rng.RandfRange(0, areaWidth - targetWidth);
float y = _rng.RandfRange(0, areaHeight - targetHeight);

_targetButton.Position = new Vector2(x, y);

bool isPerfect = _rng.Randf() < 0.3f;
if (isPerfect)
{
_targetButton.Text = "★";
_targetButton.AddThemeColorOverride("font_color", new Color(1, 0.84f, 0));
_targetButton.AddThemeColorOverride("font_outline_color", new Color(0.8f, 0.6f, 0));
_targetButton.AddThemeConstantOverride("outline_size", 2);
}
else
{
_targetButton.Text = "●";
_targetButton.AddThemeColorOverride("font_color", new Color(0.3f, 0.8f, 1));
_targetButton.AddThemeColorOverride("font_outline_color", new Color(0.2f, 0.5f, 0.8f));
_targetButton.AddThemeConstantOverride("outline_size", 1);
}

_targetButton.Visible = true;
_targetActive = true;
_targetLifetime = 0;
}

private void HideTarget()
{
_targetButton.Visible = false;
_targetActive = false;
}

private void OnTargetPressed()
{
float size = _targetButton.Size.X;
bool isPerfect = _targetButton.Text == "★";

_playManager.RegisterHit(isPerfect);

ShowFeedback(_playManager.GetHitQualityText(isPerfect), isPerfect ? new Color(1, 0.84f, 0) : new Color(0.3f, 0.8f, 1));

HideTarget();
}

private void ShowFeedback(string text, Color color)
{
_feedbackLabel.Text = text;
_feedbackLabel.AddThemeColorOverride("font_color", color);
var timer = GetTree().CreateTimer(0.5f);
timer.Timeout += () =>
{
if (IsInstanceValid(_feedbackLabel))
_feedbackLabel.Text = "";
};
}

private void OnScoreChanged(int score)
{
_scoreLabel.Text = "Pontos: " + score;
}

private void OnTimeRemainingChanged(float time)
{
_timerLabel.Text = "Tempo: " + Mathf.CeilToInt(time).ToString();
}

private void OnSessionStarted()
{
_feedbackLabel.Text = "Clique nos alvos!";
_feedbackLabel.AddThemeColorOverride("font_color", new Color(1, 1, 1));
var timer = GetTree().CreateTimer(1f);
timer.Timeout += () =>
{
if (IsInstanceValid(_feedbackLabel))
_feedbackLabel.Text = "";
};
}

private void OnSessionEnded(int totalScore, float happinessGained)
{
HideTarget();
_targetActive = false;

int maxCombo = _playManager.Combo;
_resultScoreLabel.Text = "Pontuação: " + totalScore;
_resultHappinessLabel.Text = "Felicidade: +" + happinessGained.ToString("F1");
_resultComboLabel.Text = "Combo máximo: " + maxCombo + "x";

_resultPanel.Visible = true;
}

private void OnBackPressed()
{
if (_playManager.IsPlaying) return;
GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
}

private void OnResultBackPressed()
{
GetTree().ChangeSceneToFile("res://scenes/main_desktop.tscn");
}
}
