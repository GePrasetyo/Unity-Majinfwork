using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net;
using UnityEngine;

namespace Majingari.Network {
    public static class NetworkUtility  {
        internal static ushort GetAvailablePort() {
            // Evaluate current system TCP connections. This is the same information provided
            // by the netstat command line application, just in .Net strongly-typed object
            // form.  We will look through the list, and if our port we would like to use
            // in our TcpClient is occupied, we will set isAvailable to false.
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            Dictionary<int, IPEndPoint> udpConnInfoArray = new Dictionary<int, IPEndPoint>();

            foreach (IPEndPoint ie in ipGlobalProperties.GetActiveUdpListeners()) {
                if (!udpConnInfoArray.ContainsKey(ie.Port))
                    udpConnInfoArray.Add(ie.Port, ie);
            }

            ushort port = (ushort) UnityEngine.Random.Range(7000, 8000);
            var maxAttempts = 1000;
            int i = 0;


            while (i < maxAttempts) {
                if (udpConnInfoArray.ContainsKey(port)) {
                    Debug.Log("Port used");
                    port = (ushort)UnityEngine.Random.Range(7000, 8000);
                }
                else {
                    Debug.Log("Port available");
                    break;
                }

                i++;
            }

            return port;
        }
    }
}
