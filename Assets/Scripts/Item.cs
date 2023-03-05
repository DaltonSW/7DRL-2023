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
        private string _spriteFilename;
        private Sprite2D _sprite;

        public Item(string itemName, string spriteFilename, StatToChange statToChange, double amountToChange)
        {
            StatToChange = statToChange;
            AmountToChange = amountToChange;
            _spriteFilename = spriteFilename;
            _itemName = itemName;
        }

        // IDK how to do what David wants to do with this
        // TODO: This
        // public Item(string itemName, string spriteFilename, Func<T> function)
        // {
        //
        // }

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            var spriteTexture =
                ImageTexture.CreateFromImage(Image.LoadFromFile($"res://Assets/Sprites/Items/{_spriteFilename}.png"));
            _sprite = GetNode<Sprite2D>("Sprite");
            _sprite.Texture = spriteTexture;

        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(double delta)
        {
        }
    }
}

