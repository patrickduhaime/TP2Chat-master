using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;


namespace Client
{
    /// <summary>
    /// Interaction logic for Chat.xaml
    /// </summary>
    public partial class Chat : Window
    {
        TcpClient client;
        Thread listener;
        object lockObject;
        private static bool serverDown = false;

        public ObservableCollection<string> Items { get; set; }

        public Chat(string username, TcpClient client)
        {
            lockObject = new object();
            Items = new ObservableCollection<string>() { };
            BindingOperations.EnableCollectionSynchronization(Items, lockObject);

            //Affichage interface
            DataContext = this;
            InitializeComponent();
            lblWelcome.Content = "Bienvenue " + username;
            tbDiscussion.AppendText("Vous avez joint la discussion.\n");

            //Création d'un thread écoutant le serveur
            this.client = client;
            listener = new Thread(() => ServerListener());
            listener.Start();
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                btnSendMessage_Click(sender, e);
        }

        private void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (tbMessage.Text == "list")
            {
                SendMessage("Liste");
                tbMessage.Text = "";
                return;
            }

            string list = "", listAff = "";
            foreach (string dest in listUsers.SelectedItems)
            {
                list += dest + ":";
                listAff += dest + ", ";
            }

            if (list == "")
            {
                string errorMessage = "Aucun destinataire n'est sélectionné.\n";
                tbMessage.Dispatcher.Invoke(new UpdateTextCallback(UpdateText), new object[] { errorMessage, Brushes.Purple });
                return;
            }

            SendMessage("Message;|&|;" + list.Remove(list.Length - 1) + ";|&|;" + tbMessage.Text);
            tbMessage.Dispatcher.Invoke(new UpdateTextCallback(UpdateText), new object[] { "À " + listAff.Remove(listAff.Length - 2, 2) + " : ", Brushes.Red });
            tbMessage.Dispatcher.Invoke(new UpdateTextCallback(UpdateText), new object[] { tbMessage.Text + "\n", Brushes.Black });
            tbMessage.Text = "";
        }

        private void ServerListener()
        {
            bool isOpen = true;
            while (isOpen)
            {
                StreamReader sr = new StreamReader(client.GetStream());
                try
                {
                    string data = sr.ReadLine();
                    string[] elements = data.Split(new string[] {";|&|;"}, StringSplitOptions.None);
                    switch (elements[0])
                    {
                        case "Message":
                            tbMessage.Dispatcher.Invoke(new UpdateTextCallback(UpdateText),
                                new object[] {"De " + elements[1] + " : ", Brushes.Blue});
                            tbMessage.Dispatcher.Invoke(new UpdateTextCallback(UpdateText),
                                new object[] {elements[2] + "\n", Brushes.Black});
                            break;

                        case "Connexion":
                            lock (lockObject)
                                Items.Add(elements[1]);
                            tbMessage.Dispatcher.Invoke(new UpdateTextCallback(UpdateText),
                                new object[] {"Connexion de " + elements[1] + "\n", Brushes.Green});
                            break;

                        case "List":
                            string[] users = elements[1].Split(':');
                            lock (lockObject)
                                foreach (string user in users)
                                    Items.Add(user);
                            break;

                        case "Liste":
                            tbMessage.Dispatcher.Invoke(new UpdateTextCallback(UpdateText),
                                new object[] {"Utilisateurs connectés :  " + elements[1] + "\n", Brushes.Green});
                            break;

                        case "Deconnexion":
                            lock (lockObject)
                                Items.Remove(elements[1]);
                            tbMessage.Dispatcher.Invoke(new UpdateTextCallback(UpdateText),
                                new object[] {"Déconnexion de " + elements[1] + "\n", Brushes.Green});
                            break;

                    }
                }
                catch (IOException)
                {
                    isOpen = false;
                    Dispatcher.Invoke(new ServerDownCall(ServerIsDown));
                }
            }
        }

        public delegate void ServerDownCall();

        public delegate void UpdateTextCallback(string message, SolidColorBrush color);

        private void ServerIsDown()
        {
            serverDown = true;
            MessageBox.Show("Le serveur a été fermé.");
            Close();
        }

        private void UpdateText(string message, SolidColorBrush color)
        {
            TextRange tr = new TextRange(tbDiscussion.Document.ContentEnd, tbDiscussion.Document.ContentEnd);
            tr.Text = message;
            tr.ApplyPropertyValue(TextElement.ForegroundProperty, color);
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e) => listUsers.SelectAll();

        private void btnDeconnect_Click(object sender, RoutedEventArgs e) => Close();

        private void ChatClosing(object sender, CancelEventArgs e)
        {
            if (serverDown)
                return;

            listener.Abort();
            SendMessage("Deconnexion");
            client.GetStream().Close();
            client.Close();
        }

        private void SendMessage(string message)
        {
            StreamWriter sw = new StreamWriter(client.GetStream());
            sw.WriteLine(message);
            sw.Flush();
        }
    }
}
