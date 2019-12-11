using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace ServerEF
{
    class Program
    {
        public static Dictionary<String, Socket> ClientSocketList = new Dictionary<String, Socket>();

        static void Main(string[] args)
        {
            ExecuteServer();
        }

        public static void ExecuteServer()
        {
            IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 11111);


            Socket listener = new Socket(ipAddr.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);

            try
            {
                Socket clientSocket;
                listener.Bind(localEndPoint);
                listener.Listen(10);


                int n = 0;
                while (true)
                {
                    Console.WriteLine("Waiting connection ... ");
                    clientSocket = listener.Accept();
                    Task t = Task.Factory.StartNew(() => clientThread(clientSocket));

                    n++;

                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void clientThread(Socket clientSocket)
        {
            byte[] bytes = new Byte[1024];
            Header recvHeader = new Header();
            Header sendHeader = new Header();
            string data = null;
            while (true)
            {
                while (true)
                {
                    int numByte = clientSocket.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, numByte);
                    if (data.IndexOf("<EOF>") > -1)
                    {
                        break;
                    }
                }
                recvHeader = JsonConvert.DeserializeObject<Header>(data);
                data = null;

                recvHeader.payload = recvHeader.payload.Split("<EOF>")[0];

                switch (recvHeader.opcode)
                {
                    case Opcode.OPCODE_LOGIN:

                        Console.WriteLine("Encrypted Password: {0}", recvHeader.payload);
                        recvHeader.payload = DecryptPayload(recvHeader.payload, recvHeader.IV);


                        string username = recvHeader.payload.Split('&')[0];
                        string password = recvHeader.payload.Split('&')[1];
                        password = password.Replace("\0", String.Empty).Trim();
                        ClientSocketList.Add(username, clientSocket);

                        var loginContext = new SessionContext();

                        if (loginContext.Clients.Any(x => x.Username == username))
                        {

                            Console.WriteLine(loginContext.Clients.FirstOrDefault(x => x.Username == username).Password);
                            var client = loginContext.Sessions.First(x => x.Client.Username == username);

                            if (client.Client.Password == password)
                            {
                                //Console.WriteLine("Password Breakpoint");
                                Random rand = new Random();
                                int token = rand.Next(1000000);
                                client.token = token;
                                client.state = 1;
                                loginContext.SaveChanges();
                                sendHeader.opcode = Opcode.OPCODE_SUCCESSFUL_LOGIN_ACK;
                                sendHeader.payload = "<EOF>";
                                sendHeader.token = token;
                                bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                                clientSocket.Send(bytes);

                            }
                            else
                            {

                                sendHeader.opcode = Opcode.OPCODE_FAILED_LOGIN_ACK;
                                sendHeader.payload = "<EOF>";
                                bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                                clientSocket.Send(bytes);
                            }

                        }
                        else
                        {
                            sendHeader.opcode = Opcode.OPCODE_FAILED_LOGIN_ACK;
                            sendHeader.payload = "<EOF>";
                            bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                            clientSocket.Send(bytes);
                        }
                        break;
                    case Opcode.OPCODE_SUBSCRIBE:
                        using (var subcxt = new SessionContext())
                        {
                            var getClient = subcxt.Clients.First(x => x.Username == recvHeader.payload);
                            string subscriber = subcxt.Clients.First(x => x.Session.token == recvHeader.token).Username;
                            getClient.Subscribers.Add(new Subscriber { SubscriberName = subscriber });
                            subcxt.SaveChanges();
                        }
                        sendHeader.opcode = Opcode.OPCODE_SUCCESSFUL_SUBSCRIBE_ACK;
                        sendHeader.payload = "<EOF>";
                        bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                        clientSocket.Send(bytes);
                        break;
                    case Opcode.OPCODE_UNSUBSCRIBE:
                        using (var unsubcxt = new SessionContext())
                        {
                            var getClient = unsubcxt.Clients.First(x => x.Username == recvHeader.payload);
                            string subscriber = unsubcxt.Clients.First(x => x.Session.token == recvHeader.token).Username;
                            var unsubscribe = unsubcxt.Subscribers.First(x => x.SubscriberName == subscriber);
                            getClient.Subscribers.Remove(unsubscribe);
                            unsubcxt.SaveChanges();
                        }
                        sendHeader.opcode = Opcode.OPCODE_SUCCESSFUL_UNSUBSCRIBE_ACK;
                        sendHeader.payload = "<EOF>";
                        bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                        clientSocket.Send(bytes);
                        break;
                    case Opcode.OPCODE_POST:
                        Post newPost = new Post();
                        newPost.Content = recvHeader.payload;
                        using (var postcxt = new SessionContext())
                        {
                            foreach (var s in postcxt.Clients.First(x => x.Session.token == recvHeader.token).Subscribers)
                            {
                                string sub = s.SubscriberName;
                                Task p = Task.Factory.StartNew(() => sendPost(recvHeader.payload, sub));
                            }
                            postcxt.Clients.First(x => x.Session.token == recvHeader.token).Posts.Add(newPost);
                            postcxt.SaveChanges();
                        }

                        sendHeader.opcode = Opcode.OPCODE_POST_ACK;
                        sendHeader.payload = "<EOF>";
                        bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                        clientSocket.Send(bytes);
                        break;
                    case Opcode.OPCODE_LOGOUT:
                        using (var lgct = new SessionContext())
                        {
                            var lgentity = lgct.Sessions.FirstOrDefault(user => user.token == recvHeader.token);
                            lgentity.token = 0;
                            lgentity.state = 0;
                            lgct.SaveChanges();
                        }
                        sendHeader.opcode = Opcode.OPCODE_LOGOUT_ACK;
                        sendHeader.payload = "<EOF>";
                        bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                        clientSocket.Send(bytes);
                        break;
                    case Opcode.OPCODE_RETRIEVE:
                        var context = new SessionContext();
                        var postList = context.Clients.First(x => x.Session.token == recvHeader.token).Posts;
                        recvHeader.payload = recvHeader.payload.Split("num")[1];
                        int n = Int32.Parse(recvHeader.payload);
                        foreach (var post in postList)
                        {
                            sendHeader.opcode = Opcode.OPCODE_RETRIEVE_ACK;

                            sendHeader.payload = post.Content + "<EOF>";
                            bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                            clientSocket.Send(bytes);

                            // wait for response
                            while (true)
                            {
                                int numByte = clientSocket.Receive(bytes);
                                data += Encoding.ASCII.GetString(bytes, 0, numByte);
                                if (data.IndexOf("<EOF>") > -1)
                                {
                                    break;
                                }
                            }

                            recvHeader = JsonConvert.DeserializeObject<Header>(data);
                            data = null;
                            recvHeader.payload = recvHeader.payload.Split("<EOF>")[0];
                        }
                        bytes = null;
                        sendHeader.opcode = Opcode.OPCODE_END_RETRIEVE_ACK;
                        sendHeader.payload = "<EOF>";
                        bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                        clientSocket.Send(bytes);
                        break;
                    case Opcode.RESET:
                        using (var ct = new SessionContext())
                        {
                            var entity = ct.Sessions.FirstOrDefault(user => user.token == recvHeader.token);
                            entity.token = 0;
                            entity.state = 0;
                        }
                        sendHeader.opcode = Opcode.RESET;
                        sendHeader.payload = "<EOF>";
                        bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                        clientSocket.Send(bytes);
                        break;
                    default:
                        break;
                }

            }
        }

        public static void sendPost(string post, string subscriber)
        {
            byte[] bytes = new Byte[1024];
            Header postHeader = new Header();
            postHeader.payload = post;
            postHeader.payload_len = post.Length;
            postHeader.opcode = Opcode.OPCODE_FORWARD;
            bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(postHeader));
            Socket subSocket = ClientSocketList[subscriber];
            subSocket.Send(bytes);
        }

        public static string DecryptPayload(string encryptedPayload, byte[] IV)
        {
            string EK = "VmYq3t6w9z$C&F)J";
            string cleartext = "";
            using (AesCryptoServiceProvider decryptor = new AesCryptoServiceProvider())
            {
                decryptor.KeySize = 128;
                decryptor.Key = Encoding.UTF8.GetBytes(EK);
                decryptor.IV = IV;
                decryptor.Padding = PaddingMode.Zeros;
                byte[] b = Convert.FromBase64String(encryptedPayload);
                
                ICryptoTransform d = decryptor.CreateDecryptor(decryptor.Key, decryptor.IV);

                using (MemoryStream msDecrypt = new MemoryStream(b))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, d, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            try
                            {
                                cleartext = srDecrypt.ReadToEnd();
                                return cleartext;
                            }
                            catch(Exception e)
                            {
                                Console.WriteLine(e);
                            }
                            
                        }
                    }
                }
            }
            
            return cleartext;
        }

    }
}

