using Godot;
using System;

public partial class MainMenu : Control
{
    private AudioStreamPlayer _audioPlayer;
    private AudioStreamWav _menuSong;
    private Sprite2D _creditsSprite;

    private Button _startButton;
    private Button _tutorialButton;
    private Button _creditsButton;
    private Button _quitButton;

    public override void _Ready()
    {
        _menuSong = GD.Load<AudioStreamWav>("res://Assets/Sounds/MainMenu.wav");
        _audioPlayer = GetNode<AudioStreamPlayer>("AudioPlayer");
        _creditsSprite = GetNode<Sprite2D>("Credits");
        _startButton = GetNode<Button>("StartButton");
        _tutorialButton = GetNode<Button>("TutorialButton");
        _creditsButton = GetNode<Button>("CreditsButton");
        _quitButton = GetNode<Button>("QuitButton");

        _audioPlayer.Autoplay = true;
        _audioPlayer.Stream = _menuSong;
        _audioPlayer.VolumeDb = -5f;
        _audioPlayer.Play();
    }

    public override void _Process(double delta)
    {
        if (!_creditsSprite.Visible ||
            (!Input.IsActionJustPressed("Pause") && !Input.IsActionJustPressed("PlayerShoot"))) return;
        _creditsSprite.Visible = false;
        _startButton.Disabled = false;
        _tutorialButton.Disabled = false;
        _creditsButton.Disabled = false;
        _quitButton.Disabled = false;
    }

    private void _on_StartButton_pressed()
    {
        GetTree().ChangeSceneToFile("res://Assets/Scenes/Scene.tscn");
    }

    private void _on_TutorialButton_pressed()
    {
        GetTree().ChangeSceneToFile("res://Assets/Scenes/Tutorial.tscn");
    }

    private void _on_QuitButton_pressed()
    {
        GetTree().Quit();
    }

    private void _on_CreditsButton_pressed()
    {
        _creditsSprite.Visible = true;
        _startButton.Disabled = true;
        _tutorialButton.Disabled = true;
        _creditsButton.Disabled = true;
        _quitButton.Disabled = true;
    }
}
