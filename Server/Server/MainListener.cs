﻿using System;
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
            "All activities performed on this device are logged and monitored.\n\n\n"); }

        public void Start()
        {
            //afficher le  MOTD
            motd();

            //le serveur ecoute sur 0.0.0.0 port 8080
            TcpListener listener = new TcpListener(IPAddress.Parse("0.0.0.0"), 8080);
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
                catch (Exception e) { Console.WriteLine(e);
                    client.Close();
                }
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
                    if (entry.Value.Connected)
                    {
                        StreamWriter sw = new StreamWriter(entry.Value.GetStream());
                        if (entry.Key != username)
                            sw.WriteLine("Connexion;|&|;" + username);
                        else
                            sw.WriteLine("List;|&|;" + list.Remove(list.Length - 1));
                        sw.Flush();
                    }
                    else
                        dictUsers.Remove(entry.Key);
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
                            string[] destinataires = elements[1].Split(':');
                            String sentTo = null;
                            foreach (string dest in destinataires)
                            {
                                if(sentTo != null)
                                    sentTo = sentTo + "," + dest;
                                else
                                    sentTo = dest;

                                if (dictUsers[dest] == null)
                                    continue;

                                if (dictUsers[dest].Connected)
                                {
                                    StreamWriter sw = new StreamWriter(dictUsers[dest].GetStream());
                                    sw.WriteLine("Message;|&|;" + username + ";|&|;" + elements[2]);
                                    sw.Flush();
                                }
                                else
                                    dictUsers.Remove(dest);
                            }
                            LogHelper.Log("Message sent from: " + "\"" + username + "\"" + " To: " + "\"" + sentTo + "\"");
                            LogHelper.Log("Message body: " + "\""+ elements[2] + "\"");
                            break;

                        case "Deconnexion":
                            foreach (KeyValuePair<string, TcpClient> entry in dictUsers)
                            {
                                if (entry.Value.Connected)
                                {
                                    StreamWriter sw = new StreamWriter(entry.Value.GetStream());
                                    sw.WriteLine("Deconnexion;|&|;" + username);
                                    sw.Flush();
                                }
                                else
                                    dictUsers.Remove(entry.Key);
                            }
                            LogHelper.Log("Closing communication with " + username + " from IP: " + IPAddress.Parse(((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString()));
                            dictUsers.Remove(username);
                            username = null;
                            return;

                        case "Liste":
                            string list2 = "";
                            foreach (KeyValuePair<string, TcpClient> entry in dictUsers)
                                if (entry.Key != username)
                                    list2 += entry.Key + ":";
                            if (list2 == "")
                                break;
                            StreamWriter sw2 = new StreamWriter(client.GetStream());
                            sw2.WriteLine("Liste;|&|;" + list2.Remove((list2.Length - 1)));
                            sw2.Flush();
                            Console.WriteLine("List of users sent to " + username);
                            break;

                        default:
                            break;
                    }
                }
                catch (Exception e) { Console.WriteLine(e); return; }
            }
        }
    }
}

