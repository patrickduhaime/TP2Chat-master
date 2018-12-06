using System;
using System.IO;
using System.Net.Sockets;
using System.Windows;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TcpClient client;

        public MainWindow() => InitializeComponent();

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //Connexion au serveur
            try { client = new TcpClient("server.ca", 8080); }
            catch (Exception) { lblError.Content = "Le serveur n'est pas ouvert."; return; }

            StreamWriter sw = new StreamWriter(client.GetStream());
            sw.WriteLine("Connexion;|&|;" + tbName.Text);
            sw.Flush();

            StreamReader sr = new StreamReader(client.GetStream());
            string request = sr.ReadLine();

            if (request == "Accepte")
            {
                lblError.Content = "";
                Chat chat = new Chat(tbName.Text, client);
                Hide();
                chat.ShowDialog();
                Show();
            }
            else
            {
                lblError.Content = "Ce nom est déjà utilisé.";
            }
        }
    }
}
