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
        Damage,
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

        public override void _Ready()
        {
            _sprite = GetNode<Sprite2D>("./Sprite");
            _sprite.Texture = GD.Load<Texture2D>($"res://Assets/Sprites/Items/{_spriteFilename}.png");

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

// Items:
// Lead Underwear - Butt stomp does more damage
// Soylent - Health up
// Hotdog - Health up
// Bike Pump - Higher bounce
// Coffee - Move speed up
// Trigger Finger - Fire rate up
// Campfire - Flaming bullets
// Bigger bullets - Bigger bullets
// Hardhat - Can't take damage on your head

