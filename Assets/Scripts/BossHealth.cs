using Godot;
using System;

namespace Cowball
{
    public partial class BossHealth : Control
    {

        private SlimeBoss _slimeBoss;
        private TextureProgressBar _bar;
        public override void _Ready()
        {
            _slimeBoss = GetNode<SlimeBoss>("../SlimeBoss");
            _bar = GetNode<TextureProgressBar>("Bar");
        }

        public override void _Process(double delta)
        {
            _bar.Value = _slimeBoss.GetHealthPercent();
            if (_bar.Value == 0)
            {
                QueueFree();
            }
        }
    }
}
