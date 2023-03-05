using Godot;
using System;

namespace Cowball
{
    public enum StatToChange
    {
        None,
        Speed,
        FireRate,
        Health,
        JumpSpeed,
    }

    public readonly record struct ItemParams(string ItemName, string SpriteFilename, StatToChange StatToChange,
        double AmountToChange);

    public partial class Item : Node2D
    {
        public StatToChange StatToChange;
        public double AmountToChange;
        private string _itemName;
        private string _spriteFilename;
        private Sprite2D _sprite;


        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _sprite = GetNode<Sprite2D>("./Sprite");
            _sprite.Texture = GD.Load<Texture2D>($"res://Assets/Sprites/Items/{_spriteFilename}.png");

        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }

        public void Initialize(ItemParams itemParams)
        {
            StatToChange = itemParams.StatToChange;
            AmountToChange = itemParams.AmountToChange;
            _spriteFilename = itemParams.SpriteFilename;
            _itemName = itemParams.ItemName;
        }
    }
}

