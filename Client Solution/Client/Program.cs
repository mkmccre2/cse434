using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace Client
{
    class Program
    {

        static void Main(string[] args)
        {
            ExecuteClient();
        }

        public static string EncryptPayload(string payload)
        {
            string EK = "VmYq3t6w9z$C&F)J";
            using (AesCryptoServiceProvider encryptor = new AesCryptoServiceProvider())
            {
                encryptor.KeySize = 128;
                encryptor.Key = Encoding.UTF8.GetBytes(EK);
                encryptor.GenerateIV();
                encryptor.Padding = PaddingMode.Zeros;
                sendHeader.IV = encryptor.IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform e = encryptor.CreateEncryptor(encryptor.Key, encryptor.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, e, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(payload, 0, payload.Length);
                        }
                        payload = Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
           
            return payload;
        }

        public static Header recvHeader = new Header();
        public static Header sendHeader = new Header();

        static void ExecuteClient()
        {
            int token = 0;
            try
            {

                IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddr = ipHost.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 11111);

                Socket sender = new Socket(ipAddr.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);

                try
                {

  
                    sender.Connect(localEndPoint);

                    Console.WriteLine("Socket connected to -> {0} ",
                          sender.RemoteEndPoint.ToString());



                    while (true)
                    {


                        string clientInput = Console.ReadLine();
                        string prefix = clientInput.Split('#')[0];
                        clientInput.Trim();
                        prefix.Trim();
                        byte[] bytes = new Byte[1024];
                        
                        byte[] bt = null;
                        int byteSent;
                        switch (prefix)
                        {
                            case "login":
                                sendHeader.opcode = Opcode.OPCODE_LOGIN;
                                sendHeader.msg_id = 0;
                                sendHeader.token = token;
                                sendHeader.payload_len = clientInput.Split('#')[1].Length;
                                
                                sendHeader.payload = clientInput.Split('#')[1];
                                sendHeader.payload = EncryptPayload(sendHeader.payload);
                                sendHeader.payload = sendHeader.payload + "<EOF>";
                                bt = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                                byteSent = sender.Send(bt);
                                sendHeader.IV = null;
                                bt = null;
                                break;
                            case "subscribe":
                                Header sendHeader2 = new Header();
                                sendHeader2.opcode = Opcode.OPCODE_SUBSCRIBE;
                                sendHeader2.msg_id = 0;
                                sendHeader2.token = token;
                                sendHeader2.payload_len = clientInput.Split('#')[1].Length;
                                sendHeader2.payload = clientInput.Split('#')[1] + "<EOF>";
                                bt = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader2));
                                byteSent = sender.Send(bt);
                                bt = null;
                                break;
                            case "unsubscribe":
                                sendHeader.opcode = Opcode.OPCODE_UNSUBSCRIBE;
                                sendHeader.msg_id = 0;
                                sendHeader.token = token;
                                sendHeader.payload_len = clientInput.Split('#')[1].Length;
                                sendHeader.payload = clientInput.Split('#')[1] + "<EOF>";
                                bt = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                                byteSent = sender.Send(bt);
                                break;
                            case "post":
                                sendHeader.opcode = Opcode.OPCODE_POST;
                                sendHeader.msg_id = 0;
                                sendHeader.token = token;
                                sendHeader.payload_len = clientInput.Split('#')[1].Length;
                                sendHeader.payload = clientInput.Split('#')[1] + "<EOF>";
                                bt = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                                byteSent = sender.Send(bt);
                                break;
                            case "logout":
                                sendHeader.opcode = Opcode.OPCODE_LOGOUT;
                                sendHeader.msg_id = 0;
                                sendHeader.token = token;
                                sendHeader.payload_len = clientInput.Split('#')[1].Length;
                                sendHeader.payload = clientInput.Split('#')[1] + "<EOF>";
                                bt = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                                byteSent = sender.Send(bt);
                                break;
                            case "reset":
                                sendHeader.opcode = Opcode.RESET;
                                sendHeader.msg_id = 0;
                                sendHeader.token = token;
                                sendHeader.payload_len = clientInput.Split('#')[1].Length;
                                sendHeader.payload = clientInput.Split('#')[1] + "<EOF>";
                                bt = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                                byteSent = sender.Send(bt);

                                break;
                            case "retrieve":
                                sendHeader.opcode = Opcode.OPCODE_RETRIEVE;
                                sendHeader.msg_id = 0;
                                sendHeader.token = token;
                                sendHeader.payload_len = clientInput.Split('#')[1].Length;
                                sendHeader.payload = "num" + clientInput.Split('#')[1] + "<EOF>";
                                bt = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                                byteSent = sender.Send(bt);
                                break;
                            default:
                                break;
                        }

                        string data = null;
                        while (true)
                        {
                            int numByte = sender.Receive(bytes);
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
                            case Opcode.OPCODE_SUCCESSFUL_LOGIN_ACK:
                                token = recvHeader.token;
                                Console.WriteLine("login#successful_ack");
                                break;
                            case Opcode.OPCODE_FAILED_LOGIN_ACK:
                                Console.WriteLine("login_ack#failed");
                                break;
                            case Opcode.OPCODE_SUCCESSFUL_UNSUBSCRIBE_ACK:
                                Console.WriteLine("unsubscribe_ack#successful");
                                break;
                            case Opcode.OPCODE_FORWARD:
                                Console.WriteLine(recvHeader.payload);
                                break;
                            case Opcode.OPCODE_POST_ACK:
                                Console.WriteLine("post_ack#successful");
                                break;
                            case Opcode.OPCODE_LOGOUT_ACK:
                                token = 0;
                                Console.WriteLine("logout_ack#successful");
                                break;
                            case Opcode.OPCODE_END_RETRIEVE_ACK:
                                Console.WriteLine("retrieve_ack#successful");
                                break;
                            case Opcode.OPCODE_RETRIEVE_ACK:
                                while (recvHeader.opcode != Opcode.OPCODE_END_RETRIEVE_ACK)
                                {
                                    Console.WriteLine(recvHeader.payload);
                                    sendHeader.opcode = Opcode.OPCODE_RETRIEVE;
                                    sendHeader.msg_id = 0;
                                    sendHeader.token = 0;
                                    sendHeader.payload = "<EOF>";
                                    bt = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(sendHeader));
                                    byteSent = sender.Send(bt);
                                    data = null;
                                    while (true)
                                    {
                                        int numByte = sender.Receive(bytes);
                                        data += Encoding.ASCII.GetString(bytes, 0, numByte);
                                        if (data.IndexOf("<EOF>") > -1)
                                        {
                                            break;
                                        }

                                    }
                                    recvHeader = JsonConvert.DeserializeObject<Header>(data);
                                    Console.WriteLine("retrieve_ack#successful");
                                }

                                break;
                            case Opcode.OPCODE_FAILED_SUBSCRIBE_ACK:
                                Console.WriteLine("subscribe_ack#failed");
                                break;
                            case Opcode.OPCODE_SUCCESSFUL_SUBSCRIBE_ACK:
                                Console.WriteLine("subscribe_ack#successful");
                                break;
                            case Opcode.RESET:
                                token = 0;
                                Console.WriteLine("reset#successful");
                                break;
                            default:
                                break;
                        }


                    }


                }


                catch (ArgumentNullException ane)
                {

                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }

                catch (SocketException se)
                {

                    Console.WriteLine("SocketException : {0}", se.ToString());
                }

                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }

            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }
        }
    }
}
