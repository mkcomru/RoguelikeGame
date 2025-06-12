using System.Windows.Input;
using GunVault.Models;
using System;

namespace GunVault.GameEngine
{
    public class InputHandler
    {
        private Player _player;
        
        public InputHandler(Player player)
        {
            _player = player;
        }
        
        public void HandleKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.W:
                    _player.MovingUp = true;
                    Console.WriteLine("Установлен флаг MovingUp = true");
                    break;
                case Key.S:
                    _player.MovingDown = true;
                    Console.WriteLine("Установлен флаг MovingDown = true");
                    break;
                case Key.A:
                    _player.MovingLeft = true;
                    Console.WriteLine("Установлен флаг MovingLeft = true");
                    break;
                case Key.D:
                    _player.MovingRight = true;
                    Console.WriteLine("Установлен флаг MovingRight = true");
                    break;
            }
        }
        
        public void HandleKeyUp(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.W:
                    _player.MovingUp = false;
                    Console.WriteLine("Установлен флаг MovingUp = false");
                    break;
                case Key.S:
                    _player.MovingDown = false;
                    Console.WriteLine("Установлен флаг MovingDown = false");
                    break;
                case Key.A:
                    _player.MovingLeft = false;
                    Console.WriteLine("Установлен флаг MovingLeft = false");
                    break;
                case Key.D:
                    _player.MovingRight = false;
                    Console.WriteLine("Установлен флаг MovingRight = false");
                    break;
            }
        }
    }
} 