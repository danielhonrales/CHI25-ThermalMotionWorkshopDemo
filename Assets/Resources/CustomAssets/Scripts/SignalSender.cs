using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class SignalSender : MonoBehaviour
{
    public static string raspberryPiIP = "16.254.7.17";  // Replace with the Raspberry Pi's IP address
    public static int raspberryPiPort = 25567;             // Replace with the port number used on the Raspberry Pi

    public TcpClient client;
    public NetworkStream stream;
    public bool connected;

    private int heartbeat;

    public void Start() {
        connected = false;
        //EstablishConnection();
    }

    public void Update() {
        if (Input.GetKeyDown(KeyCode.C)) {
            EstablishConnection();
        }
    }

    public void EstablishConnection() {
        try
        {
            // Create a TCP/IP socket
            client = new TcpClient(raspberryPiIP, raspberryPiPort);

            // Get a client stream for reading and writing
            stream = client.GetStream();
            connected = true;

            Debug.Log("connected");
        }
        catch (SocketException e)
        {
            SocketError(e);
        }
    }

    public void SendSignal(string signal)
    {
        int packetSize = 4096;

        signal = signal + "$";
        print(signal);

        try
        {
            // Translate the signal string into bytes
            byte[] data = Encoding.ASCII.GetBytes(signal);

            // Send message count
            int packetCount = (int)Math.Ceiling((float)data.Length / packetSize);
            byte[] countData = Encoding.ASCII.GetBytes(packetCount.ToString());
            //stream.Write(countData, 0, countData.Length);

            // Send messages
            for (int i = 0; i < packetCount; i++)
            {
                stream.Write(data[(i * packetSize)..Math.Min(((i + 1) * packetSize), data.Length)], 0, data.Length);
            }

            // Close everything
            //stream.Close();
            //client.Close();
        }
        catch (Exception e)
        {
            SocketError(e);
        }


        //Add an ending symbol
        /* try
         {
             // Translate the signal string into bytes
             byte[] data = Encoding.ASCII.GetBytes(signal);

             // Define the chunk size
             int chunkSize = 1024; // Adjust this value as needed

             // Send the data in chunks
             for (int i = 0; i < data.Length; i += chunkSize)
             {
                 int remainingBytes = Math.Min(chunkSize, data.Length - i);
                 stream.Write(data, i, remainingBytes);
             }

             print("Data sent successfully");
         }
         catch (Exception e)
         {
             SocketError(e);
         }*/
    }

    private void SocketError(Exception e) {
        Debug.Log("Socket Exception: " + e);
    }
}
