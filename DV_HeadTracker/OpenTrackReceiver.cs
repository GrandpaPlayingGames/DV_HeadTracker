using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DV_HeadTracker
{
    public static class OpenTrackReceiver
    {
        private static UdpClient _udpClient;
        private static Thread _receiveThread;
        private static bool _running;

        private static readonly object _lock = new object();

        private static double _yaw;
        private static double _pitch;

        private static bool _hasReceivedData;
        private static DateTime _lastPacketUtc;

        public static double Yaw
        {
            get
            {
                lock (_lock)
                    return _yaw;
            }
        }

        public static double Pitch
        {
            get
            {
                lock (_lock)
                    return _pitch;
            }
        }

        public static bool HasReceivedData
        {
            get
            {
                lock (_lock)
                    return _hasReceivedData;
            }
        }

        public static double SecondsSinceLastPacket
        {
            get
            {
                lock (_lock)
                {
                    if (!_hasReceivedData)
                        return double.PositiveInfinity;

                    return (DateTime.UtcNow - _lastPacketUtc).TotalSeconds;
                }
            }
        }

        public static bool SignalAvailable =>
            HasReceivedData && SecondsSinceLastPacket < 1.0;

        public static void Start()
        {
            if (_running)
                return;

            try
            {

                lock (_lock)
                {
                    _hasReceivedData = false;
                    _lastPacketUtc = default;
                }

                _udpClient = new UdpClient(4242);

                _running = true;

                _receiveThread = new Thread(ReceiveLoop);
                _receiveThread.IsBackground = true;
                _receiveThread.Start();

                Main.Log("[DV_HeadTracker] OpenTrack UDP receiver started on port 4242.");
            }
            catch (Exception ex)
            {
                Main.Log("[DV_HeadTracker] Failed to start OpenTrack UDP receiver: " + ex);
            }
        }

        public static void Stop()
        {
            _running = false;

            try
            {
                _udpClient?.Close();
            }
            catch
            {
            }

            _udpClient = null;
            _receiveThread = null;
        }

        private static void ReceiveLoop()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (_running)
            {
                try
                {
                    byte[] data = _udpClient.Receive(ref remoteEndPoint);

                    if (data == null || data.Length < 48)
                        continue;

                    double x = BitConverter.ToDouble(data, 0);
                    double y = BitConverter.ToDouble(data, 8);
                    double z = BitConverter.ToDouble(data, 16);

                    double yaw = BitConverter.ToDouble(data, 24);
                    double pitch = BitConverter.ToDouble(data, 32);
                    double roll = BitConverter.ToDouble(data, 40);

                    lock (_lock)
                    {
                        _yaw = yaw;
                        _pitch = pitch;
                        _hasReceivedData = true;
                        _lastPacketUtc = DateTime.UtcNow;
                    }
                }
                catch (SocketException)
                {
                    if (!_running)
                        return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    Main.Log("[DV_HeadTracker] OpenTrack receive error: " + ex);
                }
            }
        }
    }
}
