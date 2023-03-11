using Godot;
using System;

namespace Cowball
{
    // Paradigm and implementation taken from Fornclake / Los Alamos Steam Lab
    // https://lasteamlab.com/documentation/game-design/godot/RPG/lessons/10-heart-ui.html
    // https://www.youtube.com/watch?v=F_5BrQzIUdc

    public partial class PlayerHealth : CanvasLayer
    {
        private const int HeartRowSize = 6;
        private const int HeartPixelOffset = 18;

        private Player _player;
        private Sprite2D _baseHeart;
        private Node _hearts;

        public override void _Ready()
        {
            _player = GetNode<Player>("../Player");
            _hearts = GetNode<Node>("Hearts");
            _baseHeart = GetNode<Sprite2D>("baseHeart");
            _baseHeart.Visible = false;

            for (var i = 0; i < _player.MaxHealth; i++)
            {
                AddHeart();
            }
        }

        public override void _Process(double delta)
        {
            var lastHeart = Mathf.Floor(_player.CurrentHealth);
            foreach (var node in _hearts.GetChildren())
            {
                var heart = (Sprite2D)node;
                var index = heart.GetIndex(); // What heart after the first it is

                var xPos = (index % HeartRowSize) * HeartPixelOffset + HeartPixelOffset / 2;
                var yPos = (index / HeartRowSize) * HeartPixelOffset + HeartPixelOffset / 2;
                heart.GlobalPosition = new Vector2(xPos, yPos);
                if (index > lastHeart)
                    heart.Frame = 0;
                else if (Math.Abs(index - lastHeart) < 0.001)
                    heart.Frame = (int)((_player.CurrentHealth - lastHeart) * 2);
                else
                {
                    heart.Frame = 2;
                }
            }
        }

        public void AddHeart()
        {
            var newHeart = new Sprite2D();
            newHeart.Texture = _baseHeart.Texture;
            newHeart.Hframes = _baseHeart.Hframes;
            _hearts.AddChild(newHeart);
        }
    }
}