using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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

        public ObservableCollection<string> Items { get; set; }

        public Chat(string username, TcpClient client)
        {

            lockObject = new object();
            Items = new ObservableCollection<string>() { };
            BindingOperations.EnableCollectionSynchronization(Items, lockObject);

            //Affichage interface
            //DataContext = new ViewModel();
            DataContext = this;
            InitializeComponent();
            lblWelcome.Content = "Bienvenue " + username;
            tbDiscussion.AppendText("Vous avez joint la discussion.");

            //Connexion au serveur
            this.client = client;
            SendMessage("Connexion;" + username);
            listener = new Thread(() => ServerListener());
            listener.Start();



            
        }

        private void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            //Person message;
            //message = (Person) listUsers.SelectedItems[0];
            //tbDiscussion.AppendText(message.Name + "\n");
            //tbDiscussion.AppendText(client.Client.ToString() + "\n");
            //foreach (string hello in listUsers) { }

            string list = "";
            foreach (string dest in listUsers.SelectedItems)
                list += dest + ":";

            if(list == "")
            {
                string errorMessage = "Aucun destinataire n'est sélectionné.";
                tbMessage.Dispatcher.Invoke(new UpdateTextCallback(UpdateText), new object[] { Environment.NewLine + errorMessage, Brushes.Purple });
                return;
            }
                
            SendMessage("Message;" + list.Remove(list.Length - 1) + ";" + tbMessage.Text);
            tbMessage.Dispatcher.Invoke(new UpdateTextCallback(UpdateText), new object[] { "À " + listUsers.SelectedItems[0] + " : ", Brushes.Red });
            tbMessage.Dispatcher.Invoke(new UpdateTextCallback(UpdateText), new object[] { tbMessage.Text + "\n", Brushes.Black });
            tbMessage.Text = "";
        }

        private void ServerListener()
        {
            while (true)
            {
                //try
                //{
                    StreamReader sr = new StreamReader(client.GetStream());
                    string data = sr.ReadLine();
                    string[] elements = data.Split(';');
                    switch (elements[0])
                    {
                        case "Message":
                            tbMessage.Dispatcher.Invoke(new UpdateTextCallback(UpdateText), new object[] { "De " + elements[1] + " : ", Brushes.Blue});
                            tbMessage.Dispatcher.Invoke(new UpdateTextCallback(UpdateText), new object[] { elements[2] + "\n", Brushes.Black });
                        break;

                        case "Connexion":
                            lock (lockObject)
                                Items.Add(elements[1]); 
                            break;

                        case "List":
                        string[] users = elements[1].Split(':');
                        lock (lockObject)
                            foreach(string user in users)
                                Items.Add(user);
                            break;

                        case "Deconnexion":
                            lock (lockObject)
                                Items.Remove(elements[1]);
                            break;

                        default:
                                break;
                    }
                /*}
                catch (Exception e)
                {
                    tbMessage.Dispatcher.Invoke(new UpdateTextCallback(UpdateText), new object[] { "Error" });
                    client.Close();
                    return;
                }*/

            }
        }


        public delegate void UpdateTextCallback(string message, SolidColorBrush color);

        private void UpdateText(string message, SolidColorBrush color) //=> //tbDiscussion.AppendText(message + "\n");
        {
            /*int length = tbDiscussion.Text;  // at end of text
            tbDiscussion.AppendText(mystring);
            tbDiscussion.SelectionStart = length;
            tbDiscussion.SelectionLength = mystring.Length;
            tbDiscussion.SelectionColor = Color.Red;*/
            TextRange tr = new TextRange(tbDiscussion.Document.ContentEnd, tbDiscussion.Document.ContentEnd);
            tr.Text = message;
            //tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
            //Brushes.Red;
            tr.ApplyPropertyValue(TextElement.ForegroundProperty, color);
            //tr.ApplyPropertyValue(TextElement.­ForegroundProperty, Brushes.Red);
        }

        private void btnSelectAll_Click(object sender, RoutedEventArgs e) => listUsers.SelectAll();

        private void btnDeconnect_Click(object sender, RoutedEventArgs e) => Close();

        private void ChatClosing(object sender, CancelEventArgs e)
        {
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

        /*public static void AppendText(this RichTextBox box, string text, string color)
        {
            BrushConverter bc = new BrushConverter();
            TextRange tr = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd);
            tr.Text = text;
            try
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty,
                    bc.ConvertFromString(color));
            }
            catch (FormatException) { }
        }*/

    }




    /*public class ViewModel
    {


        public List<String> Items
        {
            get { return new List<String> { "One", "Two", "Three" }; }
        }

        /*public List<Person> Users;

        public List<Person> Items
        {
            get
            {
                return new List<Person>
            {
                new Person { Name = "P1", Age = 1 },
                new Person { Name = "P2", Age = 2 }
            };
            }
        }*/
    /*}

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }*/

}
