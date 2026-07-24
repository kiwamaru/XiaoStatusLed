using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace XiaoStatusLed
{

    public sealed class XiaoCom : IDisposable
    {
        // ここを自分のXIAO RP2040のVID/PIDに合わせる
        private const string TargetVid = "2E8A";
        private const string TargetPid = "000A";

        private const int BaudRate = 115200;
        private const string DeviceResponse = "XIAO_STATUS_LED";

        private SerialPort? _serialPort;

        public bool IsConnected => _serialPort?.IsOpen == true;

        public string? PortName => _serialPort?.PortName;

        public bool Connect()
        {
            Disconnect();
            var ports = ComPortFinder.FindPorts(TargetVid, TargetPid);

            foreach (var port in ports)
            {
                Console.WriteLine($"{port.PortName}: " + $"{port.FriendlyName}");

                if (TryPing(port.PortName, out SerialPort? serialPort))
                {
                    _serialPort = serialPort;

                    Console.WriteLine(
                        $"XIAO Status LED found on {port.PortName}");

                    return true;
                }
            }

            return false;
        }


        private static bool TryPing(string portName,out SerialPort? port)
        {
            port = null;

            SerialPort? candidate = null;

            try
            {
                candidate = new SerialPort(portName, BaudRate, Parity.None, 8, StopBits.One);
                candidate.Encoding = System.Text.Encoding.ASCII;
                candidate.NewLine = "\n";
                candidate.WriteTimeout = 500;
                candidate.Handshake = Handshake.None;

                // ターミナルが接続時に DTR をアサートするのと同じ状態にする。
                // RP2040 の USB-CDC 側は DTR がアサートされていないと
                // ホスト未接続とみなして応答を返さないことがある。
                // RTS は不要なリセット信号を避けるため false のままにする。
                candidate.DtrEnable = true;
                candidate.RtsEnable = false;

                candidate.Open();

                candidate.DiscardInBuffer();
                candidate.DiscardOutBuffer();

                candidate.WriteLine("PING");

                string response = ReadResponse(candidate);

                Console.WriteLine($"received '{response}'");

                if (response == DeviceResponse)
                {
                    port = candidate;
                    return true;
                }
                candidate.Dispose();
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error pinging {portName}: {ex.Message}");

                candidate?.Dispose();
                return false;
            }
        }

        private static string ReadResponse(SerialPort port)
        {
            var buffer = new System.Collections.Generic.List<byte>();
            var start = DateTime.UtcNow;

            while ((DateTime.UtcNow - start).TotalMilliseconds < 2000)
            {
                try
                {
                    int b = port.ReadByte();
                    if (b < 0)
                    {
                        break;
                    }

                    buffer.Add((byte)b);

                    if (b == '\n')
                    {
                        break;
                    }
                }
                catch (TimeoutException)
                {
                    if (buffer.Count > 0)
                    {
                        break;
                    }
                }
            }

            return System.Text.Encoding.ASCII.GetString(buffer.ToArray()).Trim();
        }

        public void SetPattern(
            byte r,
            byte g,
            byte b,
            string waveform,
            uint periodMs,
            byte minBrightness,
            byte maxBrightness)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException(
                    "XIAO Status LED is not connected.");
            }

            string command =
                $"SET {r} {g} {b} " +
                $"{waveform} " +
                $"{periodMs} " +
                $"{minBrightness} " +
                $"{maxBrightness}";

            _serialPort!.WriteLine(command);
        }

        public void Off()
        {
            if (IsConnected)
            {
                _serialPort!.WriteLine("OFF");
            }
        }

        public void Disconnect()
        {
            if (_serialPort == null)
            {
                return;
            }

            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            _serialPort.Dispose();
            _serialPort = null;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}