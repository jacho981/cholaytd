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
    public partial class WpfMBFinConfirm : Window
    {
        public WpfMBFinConfirm()
        {            
            InitializeComponent();
            // declaramos la venata principal como padre de esta ventana
            this.Owner = App.Current.MainWindow;

        }
        
        // EFECTO BOTON
        private void borderOK_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void borderOK_MouseEnter(object sender, MouseEventArgs e)
        {
            borderOK.Height = 37;
            borderOK.Width = 102;

            borderOK.BorderBrush = (Brush)(new BrushConverter().ConvertFrom("#EBF3FA"));
            borderOK.BorderThickness = (new Thickness(2, 2, 2, 2));
            borderOK.Background = (Brush)(new BrushConverter().ConvertFrom("#EBF3FA"));

            labelOK.Foreground = (Brush)(new BrushConverter().ConvertFrom("#2F3138"));
        }

        private void borderOK_MouseLeave(object sender, MouseEventArgs e)
        {
            borderOK.Height = 35;
            borderOK.Width = 100;

            borderOK.BorderBrush = Brushes.Black;
            borderOK.BorderThickness = (new Thickness(1, 1, 1, 1));
            borderOK.Background = (Brush)(new BrushConverter().ConvertFrom("#9FBED1"));

            labelOK.Foreground = (Brush)(new BrushConverter().ConvertFrom("#2F3138"));
        }

        private void borderCancel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void borderCancel_MouseEnter(object sender, MouseEventArgs e)
        {
            borderCancel.Height = 37;
            borderCancel.Width = 102;

            borderCancel.BorderBrush = (Brush)(new BrushConverter().ConvertFrom("#EBF3FA"));
            borderCancel.BorderThickness = (new Thickness(2, 2, 2, 2));
            borderCancel.Background = (Brush)(new BrushConverter().ConvertFrom("#EBF3FA"));

            labelCancel.Foreground = (Brush)(new BrushConverter().ConvertFrom("#2F3138"));
        }

        private void borderCancel_MouseLeave(object sender, MouseEventArgs e)
        {
            borderCancel.Height = 35;
            borderCancel.Width = 100;

            borderCancel.BorderBrush = Brushes.Black;
            borderCancel.BorderThickness = (new Thickness(1, 1, 1, 1));
            borderCancel.Background = (Brush)(new BrushConverter().ConvertFrom("#9FBED1"));

            labelCancel.Foreground = (Brush)(new BrushConverter().ConvertFrom("#2F3138"));
        }
    }
}
