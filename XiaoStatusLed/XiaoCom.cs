using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace XiaoStatusLed
{

    /// <summary>
    /// XIAO RP2040 のCOMポートを操作するためのユーティリティクラス
    /// </summary>
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

        /// <summary>
        /// XIAO RP2040のCOMポートに接続する
        /// </summary>
        /// <returns>接続に成功した場合は true、失敗した場合は false</returns>
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

        /// <summary>
        /// 指定されたCOMポートにPINGコマンドを送信して応答を確認する
        /// </summary>
        /// <param name="portName">COMポート名</param>
        /// <param name="port">接続されたSerialPortオブジェクト</param>
        /// <returns>応答が正しい場合は true、そうでない場合は false</returns>
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

        /// <summary>
        /// 指定されたSerialPortから応答を読み取る
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 指定されたパターンでLEDを点灯させる
        /// </summary>
        /// <param name="r">赤の輝度 (0-255)</param>
        /// <param name="g">緑の輝度 (0-255)</param>
        /// <param name="b">青の輝度 (0-255)</param>
        /// <param name="waveform">波形の種類</param>
        /// <param name="periodMs">周期 (ミリ秒)</param>
        /// <param name="minBrightness">最小輝度 (0-255)</param>
        /// <param name="maxBrightness">最大輝度 (0-255)</param>
        /// <exception cref="InvalidOperationException"></exception>
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

        /// <summary>
        /// LEDを消灯する
        /// </summary>
        public void Off()
        {
            if (IsConnected)
            {
                _serialPort!.WriteLine("OFF");
            }
        }

        /// <summary>
        /// XIAO RP2040のCOMポートから切断する
        /// </summary>
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