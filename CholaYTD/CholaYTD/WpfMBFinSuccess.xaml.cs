using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CholaYTD
{
    /// <summary>
    /// Lógica de interacción para WpfMBFin.xaml
    /// </summary>
    public partial class WpfMBFinSuccess : Window
    {
        public WpfMBFinSuccess()
        {            
            InitializeComponent();
            // declaramos la venata principal como padre de esta ventana
            this.Owner = App.Current.MainWindow;

        }
        
        // EFECTO BOTON
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            stackPanelCerrar.Height = 36;
            stackPanelCerrar.Width = 62;

            borderCerrar.BorderBrush = (Brush)(new BrushConverter().ConvertFrom("#EBF3FA"));
            borderCerrar.BorderThickness = (new Thickness(2, 2, 2, 2));
            borderCerrar.Background = (Brush)(new BrushConverter().ConvertFrom("#EBF3FA"));

            labelCerrar.Foreground = (Brush)(new BrushConverter().ConvertFrom("#2F3138"));
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            stackPanelCerrar.Height = 35;
            stackPanelCerrar.Width = 60;

            borderCerrar.BorderBrush = Brushes.Black;
            borderCerrar.BorderThickness = (new Thickness(1, 1, 1, 1));
            borderCerrar.Background = (Brush)(new BrushConverter().ConvertFrom("#9FBED1"));

            labelCerrar.Foreground = (Brush)(new BrushConverter().ConvertFrom("#2F3138"));
        }
    }
}
