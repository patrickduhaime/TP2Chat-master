using System;
using System.Collections.Generic;
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

        private void motd()
        { Console.WriteLine(
            "\n\nWelcome to the TP2Chat server\n\n\n\nYou must use TP2Chat client to connect to this server" +
            "\n\n\n\n\nUNAUTHORIZED ACCESS TO THIS DEVICE IS PROHIBITED" +"\n" +
            "You must have explicit, authorized permission to access or configure this device\n" +
            "Unauthorized attempts and actions to access or use this system may result\n" +
            "in civiland/or criminal penalties.\n\n" +
            "All activities performed on this device are logged and monitored."); }

        public void Start()
        {
            TcpListener listener = new TcpListener(IPAddress.Parse("0.0.0.0"), 8080);

            //call MOTD
            motd();

            listener.Start();
            LogHelper.Log("Server started");

            //Listener principal gérant les connexions et créant un thread pour chaque nouveau client.
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                StreamReader sr = new StreamReader(client.GetStream());
                StreamWriter sw = new StreamWriter(client.GetStream());
                try
                {
                    //Écoute le socket.
                    string request = sr.ReadLine();
                    string[] elements = request.Split(new string[] { ";|&|;" }, StringSplitOptions.None);

                    if (elements[0] != "Connexion")
                        continue;


                    if (!dictUsers.ContainsKey(elements[1]))
                    {

                        Console.WriteLine("Connexion de " + elements[1]);
                        dictUsers.Add(elements[1], client);
                        new Thread(() => ServerListener(elements[1])).Start();
                        sw.WriteLine("Accepte");
                        sw.Flush();
                        LogHelper.Log("Access granted to " + elements[1] + " from IP: " + IPAddress.Parse(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString()));

                    }
                    else
                    {
                        sw.WriteLine("Refus");
                        sw.Flush();
                        LogHelper.Log("Access denied to " + elements[1] + " from IP: " + IPAddress.Parse(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString()));
                    }
                }
                catch (Exception e) { Console.WriteLine(e); }
            }
        }

        private void ServerListener(string username)
        {
            //Lors de la connexion, envoie au nouveau utilisateur la liste des utilisateurs déjà connectés.
            TcpClient client = dictUsers[username];
            string list = "";
            foreach (KeyValuePair<string, TcpClient> entry in dictUsers)
                if (entry.Key != username)
                    list += entry.Key + ":";

            if (list != "")
                foreach (KeyValuePair<string, TcpClient> entry in dictUsers)
                {
                    StreamWriter sw = new StreamWriter(entry.Value.GetStream());
                    if (entry.Key != username)
                        sw.WriteLine("Connexion;" + username);
                    else
                        sw.WriteLine("List;" + list.Remove(list.Length - 1));
                    sw.Flush();
                }

            //Écoute le client.
            while (true)
            {
                try
                {
                    StreamReader sr = new StreamReader(client.GetStream());
                    string request = sr.ReadLine();
                    string[] elements = request.Split(new string[] { ";|&|;" }, StringSplitOptions.None);
                    switch (elements[0])
                    {
                        case "Message":
                            Console.WriteLine("Message reçu de " + username);
                            string[] destinataires = elements[1].Split(':');
                            foreach (string dest in destinataires)
                            {
                                if (dictUsers[dest] == null)
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
                            LogHelper.Log("Closing communication with " + username + " from IP: " + IPAddress.Parse(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString()));
                            dictUsers.Remove(username);
                            Console.WriteLine(username + " déconnecté");
                            return;

                        default:
                            break;

                    }
                }
                catch (Exception e) { Console.WriteLine(e); return; }
            }
        }
    }
}

