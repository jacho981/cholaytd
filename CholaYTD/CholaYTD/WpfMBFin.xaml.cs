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
    public partial class WpfMBFin : Window
    {
        private List<string> listaEnlaces;

        public WpfMBFin(List<string> failedList)
        {
            listaEnlaces = failedList;
            
            InitializeComponent();
            // declaramos la venata principal como padre de esta ventana
            this.Owner = App.Current.MainWindow;

            crearEnlaces();
        }

        private void crearEnlaces()
        {
            
            string textoFinalEnlaces = "Sin embargo, ";
            if (listaEnlaces.Count < 2)
            {
                string urlRdy = convertirIDenURL(listaEnlaces.ElementAt(0));
                textoFinalEnlaces += "el siguiente video no estaba disponible:";
                Hyperlink hLink = new Hyperlink();
                hLink.NavigateUri = new Uri(urlRdy);
                hLink.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(Hyperlink_RequestNavigate);
                hLink.Inlines.Add(urlRdy);
                err_label.Content = textoFinalEnlaces;
                textBox_enlaces.Inlines.Add(hLink);

            }
            else
            {
                textoFinalEnlaces += "los siguientes videos no estaban disponibles:";
                err_label.Content = textoFinalEnlaces;
                foreach (string id in listaEnlaces)
                {
                    string urlRdy = convertirIDenURL(id);
                    textBox_enlaces.Inlines.Add(crearHyperlink(urlRdy));
                }
            }            
        }

        // HANDLER de Hyperlinks
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        private string convertirIDenURL(string id)
        {
            string youtubeURLStarting = "https://www.youtube.com/watch?v=";
            return youtubeURLStarting + id;
        }

        private Hyperlink crearHyperlink(string url)
        {
            Hyperlink hLink = new Hyperlink();
            hLink.NavigateUri = new Uri(url);
            hLink.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(Hyperlink_RequestNavigate);
            hLink.Inlines.Add(url + "\n");
            return hLink;
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
