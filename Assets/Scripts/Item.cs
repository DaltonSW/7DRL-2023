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

    public partial class Item : Node
    {
        public StatToChange StatToChange;
        public double AmountToChange;
        private string _itemName;
        private Sprite2D _sprite;

        public Item(StatToChange statToChange, double amountToChange, string itemName, string spriteFilename)
        {
            _sprite = GetNode<Sprite2D>("Sprite");
            StatToChange = statToChange;
            AmountToChange = amountToChange;
            _itemName = itemName;
            var spriteTexture =
                ImageTexture.CreateFromImage(Image.LoadFromFile($"res://Assets/Sprites/Items/{spriteFilename}.png"));
            _sprite.Texture = spriteTexture;
        }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }
    }
}

