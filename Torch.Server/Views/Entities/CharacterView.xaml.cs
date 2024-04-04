﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Sandbox.Game;
using Color = VRageMath.Color;

namespace Torch.Server.Views.Entities
{
    /// <summary>
    /// Interaction logic for GridView.xaml
    /// </summary>
    public partial class CharacterView : UserControl
    {
        public CharacterView()
        {
            InitializeComponent();

            ThemeControl.UpdateDynamicControls += UpdateResourceDict;
            UpdateResourceDict(ThemeControl.currentTheme);
        }

        public void UpdateResourceDict(ResourceDictionary dictionary)
        {
            this.Resources.MergedDictionaries.Clear();
            this.Resources.MergedDictionaries.Add(dictionary);
        }

        private void SendDM_ToPlayer(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PlayerGameID.Text)) return;
            if (string.IsNullOrWhiteSpace(DM_TextToPlayer.Text)) return;
            if (!long.TryParse(PlayerGameID.Text, out long Id)) return;
            
            MyVisualScriptLogicProvider.SendChatMessageColored(DM_TextToPlayer.Text, Color.Red, "Server", Id);
        }
    }
}
