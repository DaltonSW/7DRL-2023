using Godot;
using System;

public partial class MainMenu : Control
{
    private AudioStreamPlayer audioPlayer;
    //private AudioStreamWAV menuSong;

    public override void _Ready()
    {
        //menuSong = GD.Load<AudioStreamWAV>("res://Sounds/mainmenu.wav");
        audioPlayer = GetNode<AudioStreamPlayer>("AudioPlayer");
        audioPlayer.Autoplay = true;
        //audioPlayer.Stream = menuSong;
        audioPlayer.Play();
    }

    public void _on_StartButton_pressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/LevelHolder.tscn");
    }

    public void _on_QuitButton_pressed()
    {
        GetTree().Quit();
    }

    public void _on_CreditsButton_pressed()
    {
        GetTree().ChangeSceneToFile("res://Scenes/Credits.tscn");
    }
}
