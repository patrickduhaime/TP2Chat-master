using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server
{
    class MainListener
    {
        private Dictionary<String, TcpClient> dictUsers = new Dictionary<string, TcpClient>();

        public MainListener() { }

        public void Start()
        {
            TcpListener listener = new TcpListener(IPAddress.Parse("0.0.0.0"),8080);
            //TODO insert MOTD
            Console.WriteLine("Serveur ouvert.");
            listener.Start();

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                StreamReader sr = new StreamReader(client.GetStream());
                //StreamWriter sw = new StreamWriter(client.GetStream());
                //Console.WriteLine("Hello ");
                try
                {
                    string request = sr.ReadLine();
                    //Console.WriteLine(request);
                    string[] elements = request.Split(';');

                    if (elements[0] != "Connexion")
                        continue;

                    Console.WriteLine("Connexion de " + elements[1]);
                    dictUsers.Add(elements[1], client);
                    //sw.WriteLine("Connexion;Connexion de " + elements[1]);
                    //sw.Flush();
                    new Thread(() => ServerListener(elements[1])).Start();

                }
                catch (Exception e) { Console.WriteLine(e); }
            }
        }

        private void ServerListener(string username)
        {
            TcpClient client = dictUsers[username];
            string list = "";
            foreach (KeyValuePair<string, TcpClient> entry in dictUsers)
                if(entry.Key != username)
                    list += entry.Key + ":";

            if(list != "")
                foreach (KeyValuePair<string, TcpClient> entry in dictUsers)
                {
                    StreamWriter sw = new StreamWriter(entry.Value.GetStream());
                    if(entry.Key != username)
                        sw.WriteLine("Connexion;" + username);
                    else
                        sw.WriteLine("List;" + list.Remove(list.Length - 1));
                    sw.Flush();
                }

            /*StreamWriter sw2 = new StreamWriter(client.GetStream());
            string list = "";
            foreach (KeyValuePair<string, TcpClient> entry in dictUsers)
                list += entry.Key + ":";
            sw2.WriteLine("List;" + list.Remove(list.Length - 1));
            sw2.Flush();*/

            while (true)
            {
                try
                {
                    StreamReader sr = new StreamReader(client.GetStream());
                    string request = sr.ReadLine();
                    string[] elements = request.Split(';');
                    switch (elements[0])
                    {
                        case "Message":
                            Console.WriteLine("Message reçu de " + username);
                            string[] destinataires = elements[1].Split(':');
                            foreach (string dest in destinataires)
                            {
                                if(dictUsers[dest] == null)
                                    continue;

                                StreamWriter sw = new StreamWriter(dictUsers[dest].GetStream());
                                sw.WriteLine("Message;" + username + ";" + elements[2]);
                                sw.Flush();
                                Console.WriteLine("Message envoyé à " + dest);
                            }
                            break;

                        case "Deconnexion":
                            Console.WriteLine("Déconnexion de " + username);
                            foreach (KeyValuePair<string, TcpClient> entry in dictUsers)
                            {
                                StreamWriter sw = new StreamWriter(entry.Value.GetStream());
                                sw.WriteLine("Deconnexion;" + username);
                                sw.Flush();
                            }
                            dictUsers.Remove(username);
                            Console.WriteLine(username + " déconnecté");
                            return;

                        default:
                            break;

                    }

                }
                catch (Exception)
                {
                    //client.GetStream().Close();
                    //client.Close();
                    return;
                }

            }
        }
    }
}

