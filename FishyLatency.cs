using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Utility.Performance;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeardedMonkeys
{
    public class FishyLatency : Transport
    {
        #region Serialized
        [SerializeField] Transport m_transport;

        [Header("Settings")]
        [Tooltip("Enable or disable the simulation")]
        [SerializeField] bool m_enabled = true;

        [Tooltip("Additional amount of latency for all packets")]
        [Range(0, 1)]
        [SerializeField] float m_latency = 0f;

        [Tooltip("Additional amount of latency for reliable packets only when a packet get's lossed!")]
        [Range(0, 1)]
        [SerializeField] float m_reliableLatency = 0.02f;

        [Tooltip("How many % should be a packet loss")]
        [Range(0, 1)]
        [SerializeField] double m_packetloss = 0;
        #endregion

        #region Private
        private List<Message> m_clientToServerMessages;
        private List<Message> m_serverToClientMessages;

        private struct Message
        {
            public byte channelId;
            public int connectionId;
            public byte[] message;
            public int length;
            public float time;

            public Message(byte channelId, int connectionId, ArraySegment<byte> segment, float latency)
            {
                this.channelId = channelId;
                this.connectionId = connectionId;
                this.time = Time.time + latency;
                this.length = segment.Count;
                this.message = ByteArrayPool.Retrieve(this.length);
                Buffer.BlockCopy(segment.Array, segment.Offset, this.message, 0, this.length);                
            }

            public ArraySegment<byte> GetSegment()
            {
                return new ArraySegment<byte>(message, 0, length);
            }

            public void AddLatency(float latency)
            {
                this.time += latency;
            }
        }

        private readonly System.Random m_random = new System.Random();
        #endregion

        #region Initialization and Unity
        public override void Initialize(NetworkManager networkManager)
        {
            m_transport.Initialize(networkManager);
            m_clientToServerMessages = new List<Message>();
            m_serverToClientMessages = new List<Message>();

            m_transport.OnClientConnectionState += OnClientConnectionState;
            m_transport.OnServerConnectionState += OnServerConnectionState;
            m_transport.OnRemoteConnectionState += OnRemoteConnectionState;
            m_transport.OnClientReceivedData += OnClientReceivedData;
            m_transport.OnServerReceivedData += OnServerReceivedData;
        }

        private void OnDestroy()
        {
            m_transport.Shutdown();
            Shutdown();
        }
        #endregion

        #region ConnectionStates
        /// <summary>
        /// Called when a connection state changes for the local client.
        /// </summary>
        public override event Action<ClientConnectionStateArgs> OnClientConnectionState;
        /// <summary>
        /// Called when a connection state changes for the local server.
        /// </summary>
        public override event Action<ServerConnectionStateArgs> OnServerConnectionState;
        /// <summary>
        /// Called when a connection state changes for a remote client.
        /// </summary>
        public override event Action<RemoteConnectionStateArgs> OnRemoteConnectionState;
        /// <summary>
        /// Gets the current local ConnectionState.
        /// </summary>
        /// <param name="server">True if getting ConnectionState for the server.</param>
        public override LocalConnectionStates GetConnectionState(bool server)
        {
            return m_transport.GetConnectionState(server);
        }
        /// <summary>
        /// Gets the current ConnectionState of a remote client on the server.
        /// </summary>
        /// <param name="connectionId">ConnectionId to get ConnectionState for.</param>
        public override RemoteConnectionStates GetConnectionState(int connectionId)
        {
            return m_transport.GetConnectionState(connectionId);
        }
        /// <summary>
        /// Handles a ConnectionStateArgs for the local client.
        /// </summary>
        /// <param name="connectionStateArgs"></param>
        public override void HandleClientConnectionState(ClientConnectionStateArgs connectionStateArgs)
        {
            m_transport.HandleClientConnectionState(connectionStateArgs);            
        }
        /// <summary>
        /// Handles a ConnectionStateArgs for the local server.
        /// </summary>
        /// <param name="connectionStateArgs"></param>
        public override void HandleServerConnectionState(ServerConnectionStateArgs connectionStateArgs)
        {
            m_transport.HandleServerConnectionState(connectionStateArgs);
        }
        /// <summary>
        /// Handles a ConnectionStateArgs for a remote client.
        /// </summary>
        /// <param name="connectionStateArgs"></param>
        public override void HandleRemoteConnectionState(RemoteConnectionStateArgs connectionStateArgs)
        {
            m_transport.HandleRemoteConnectionState(connectionStateArgs);
        }
        #endregion

        #region Iterating
        /// <summary>
        /// Processes data received by the socket.
        /// </summary>
        /// <param name="server">True to process data received on the server.</param>
        public override void IterateIncoming(bool server)
        {
            m_transport.IterateIncoming(server);
        }

        /// <summary>
        /// Processes data to be sent by the socket.
        /// </summary>
        /// <param name="server">True to process data received on the server.</param>
        public override void IterateOutgoing(bool server)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Simulation(server);            
#else
            m_transport.IterateOutgoing(server);
#endif
        }
        #endregion

        #region ReceivedData
        /// <summary>
        /// Called when client receives data.
        /// </summary>
        public override event Action<ClientReceivedDataArgs> OnClientReceivedData;
        /// <summary>
        /// Handles a ClientReceivedDataArgs.
        /// </summary>
        /// <param name="receivedDataArgs"></param>
        public override void HandleClientReceivedDataArgs(ClientReceivedDataArgs receivedDataArgs)
        {
            m_transport.HandleClientReceivedDataArgs(receivedDataArgs);
        }
        /// <summary>
        /// Called when server receives data.
        /// </summary>
        public override event Action<ServerReceivedDataArgs> OnServerReceivedData;
        /// <summary>
        /// Handles a ClientReceivedDataArgs.
        /// </summary>
        /// <param name="receivedDataArgs"></param>
        public override void HandleServerReceivedDataArgs(ServerReceivedDataArgs receivedDataArgs)
        {
            m_transport.HandleServerReceivedDataArgs(receivedDataArgs);
        }
        #endregion

        #region Sending
        /// <summary>
        /// Sends to the server or all clients.
        /// </summary>
        /// <param name="channelId">Channel to use.</param>
        /// /// <param name="segment">Data to send.</param>
        public override void SendToServer(byte channelId, ArraySegment<byte> segment)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(m_enabled)
                Add(channelId, segment);
            else
                m_transport.SendToServer(channelId, segment);
#else
            m_transport.SendToServer(channelId, segment);
#endif
        }
        /// <summary>
        /// Sends data to a client.
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="segment"></param>
        /// <param name="connectionId"></param>
        public override void SendToClient(byte channelId, ArraySegment<byte> segment, int connectionId)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (m_enabled)
                Add(channelId, segment, true, connectionId);
            else
                m_transport.SendToClient(channelId, segment, connectionId);
#else
            m_transport.SendToClient(channelId, segment, connectionId);
#endif
        }
        #endregion

        #region Configuration
        /// <summary>
        /// Gets the IP address of a remote connection Id.
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public override string GetConnectionAddress(int connectionId)
        {
            return m_transport.GetConnectionAddress(connectionId);
        }
        /// <summary>
        /// Returns the maximum number of clients allowed to connect to the server. If the transport does not support this method the value -1 is returned.
        /// </summary>
        /// <returns></returns>
        public override int GetMaximumClients()
        {
            return m_transport.GetMaximumClients();
        }
        /// <summary>
        /// Sets maximum number of clients allowed to connect to the server. If applied at runtime and clients exceed this value existing clients will stay connected but new clients may not connect.
        /// </summary>
        /// <param name="value"></param>
        public override void SetMaximumClients(int value)
        {
            m_transport.SetMaximumClients(value);
        }
        /// <summary>
        /// Sets which address the client will connect to.
        /// </summary>
        /// <param name="address"></param>
        public override void SetClientAddress(string address)
        {
            m_transport.SetClientAddress(address);
        }
        /// <summary>
        /// Sets which address the server will bind to.
        /// </summary>
        /// <param name="address"></param>
        public override void SetServerBindAddress(string address)
        {
            m_transport.SetServerBindAddress(address);
        }
        /// <summary>
        /// Gets which address the server will bind to.
        /// </summary>
        /// <param name="address"></param>
        public override string GetServerBindAddress()
        {
            return m_transport.GetServerBindAddress();
        }
        /// <summary>
        /// Sets which port to use.
        /// </summary>
        /// <param name="port"></param>
        public override void SetPort(ushort port)
        {
            m_transport.SetPort(port);
        }
        /// <summary>
        /// Gets which port to use.
        /// </summary>
        /// <param name="port"></param>
        public override ushort GetPort()
        {
            return m_transport.GetPort();
        }
        /// <summary>
        /// Returns the adjusted timeout as float
        /// </summary>
        /// <param name="asServer"></param>
        public override float GetTimeout(bool asServer)
        {
            return m_transport.GetTimeout(asServer);
        }
#endregion

        #region Start and Stop
        /// <summary>
        /// Starts the local server or client using configured settings.
        /// </summary>
        /// <param name="server">True to start server.</param>
        public override bool StartConnection(bool server)
        {
            return m_transport.StartConnection(server);
        }

        /// <summary>
        /// Stops the local server or client.
        /// </summary>
        /// <param name="server">True to stop server.</param>
        public override bool StopConnection(bool server)
        {
            return m_transport.StopConnection(server);
        }

        /// <summary>
        /// Stops a remote client from the server, disconnecting the client.
        /// </summary>
        /// <param name="connectionId">ConnectionId of the client to disconnect.</param>
        /// <param name="immediately">True to abrutly stp the client socket without waiting socket thread.</param>
        public override bool StopConnection(int connectionId, bool immediately)
        {
            return m_transport.StopConnection(connectionId, immediately);
        }

        /// <summary>
        /// Stops both client and server.
        /// </summary>
        public override void Shutdown()
        {
            m_transport.OnClientConnectionState -= OnClientConnectionState;
            m_transport.OnServerConnectionState -= OnServerConnectionState;
            m_transport.OnRemoteConnectionState -= OnRemoteConnectionState;
            m_transport.OnClientReceivedData -= OnClientReceivedData;
            m_transport.OnServerReceivedData -= OnServerReceivedData;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            m_clientToServerMessages.Clear();
            m_serverToClientMessages.Clear();
#endif

            //Stops client then server connections.
            StopConnection(false);
            StopConnection(true);
        }
        #endregion

        #region Channels
        /// <summary>
        /// Returns which channel to use by default for reliable.
        /// </summary>
        public override byte GetDefaultReliableChannel()
        {
            return m_transport.GetDefaultReliableChannel();
        }
        /// <summary>
        /// Returns which channel to use by default for unreliable.
        /// </summary>
        public override byte GetDefaultUnreliableChannel()
        {
            return m_transport.GetDefaultUnreliableChannel();
        }
        /// <summary>
        /// Gets the MTU for a channel. This should take header size into consideration.
        /// For example, if MTU is 1200 and a packet header for this channel is 10 in size, this method should return 1190.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public override int GetMTU(byte channel)
        {
            return m_transport.GetMTU(channel);
        }
        #endregion

        #region Simulation

        private void Add(byte channelId, ArraySegment<byte> segment, bool server = false, int connectionId = 0)
        {
            if (server)
                m_serverToClientMessages.Add(new Message(channelId, connectionId, segment, m_latency));
            else
                m_clientToServerMessages.Add(new Message(channelId, connectionId, segment, m_latency));
        }

        private void Simulation(bool server)
        {
            if(server)
            {
                for (int i = 0; i < m_serverToClientMessages.Count; i++)
                {
                    if (CheckReliablePacketLoss(m_serverToClientMessages[i]))
                        break;

                    if (m_serverToClientMessages[i].time <= Time.time)
                    {
                        m_transport.SendToClient(m_serverToClientMessages[i].channelId, m_serverToClientMessages[i].GetSegment(), m_serverToClientMessages[i].connectionId);
                        m_serverToClientMessages.RemoveAt(i);
                    }
                }                
            }
            else
            {
                for (int i = 0; i < m_clientToServerMessages.Count; i++)
                {
                    if (CheckReliablePacketLoss(m_clientToServerMessages[i]))
                        break;

                    if (m_clientToServerMessages[i].time <= Time.time)
                    {
                        m_transport.SendToServer(m_clientToServerMessages[i].channelId, m_clientToServerMessages[i].GetSegment());
                        m_clientToServerMessages.RemoveAt(i);
                    }
                }
            }
            m_transport.IterateOutgoing(server);            
        }

        private bool CheckReliablePacketLoss(Message msg)
        {
            if (CheckPacketLoss())
            {
                if (msg.channelId == GetDefaultReliableChannel())
                {                
                    msg.AddLatency(m_reliableLatency);
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CheckPacketLoss()
        {
            return m_packetloss > 0 && m_random.NextDouble() < m_packetloss;
        }        
        #endregion
    }
}