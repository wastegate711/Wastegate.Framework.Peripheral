using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Device.Spi;

namespace MAX7219
{
    /// <summary>Реализация управления дисплеем MAX7219 через устройство SPI.</summary>
    public class Max7219 : IMax7219, IDisposable
    {
        private const int DigitsPerDevice = 8;
        private const byte BlankDigit = 0x0F;
        private SpiDevice _max7219;
        private int _deviceCount;

        /// <inheritdoc />
        public Task<bool> Initialization(int chipSelect, int clock, SpiMode spiMode = SpiMode.Mode0)
        {
            return Initialize(0, chipSelect, clock, spiMode, 1);
        }

        /// <inheritdoc />
        public Task<bool> Initialization(int chipSelect, int clock, SpiMode spiMode, int deviceCount)
        {
            return Initialize(0, chipSelect, clock, spiMode, deviceCount);
        }

        /// <inheritdoc />
        public Task<bool> Initialization(string busName, int chipSelect, int clock, SpiMode spiMode = SpiMode.Mode0)
        {
            return Initialize(ParseBusId(busName), chipSelect, clock, spiMode, 1);
        }

        /// <inheritdoc />
        public Task<bool> Initialization(string busName, int chipSelect, int clock, SpiMode spiMode, int deviceCount)
        {
            return Initialize(ParseBusId(busName), chipSelect, clock, spiMode, deviceCount);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _max7219?.Dispose();
            _max7219 = null;
            _deviceCount = 0;
        }

        /// <inheritdoc />
        public void Max7219Config()
        {
            EnsureInitialized();
            SendCmd((byte)Registers.DisplayTest, 0);
            SendCmd((byte)Registers.Shutdown, 0);
            SendCmd((byte)Registers.Intensity, 3);
            SendCmd((byte)Registers.ScanLimit, 7);
            SendCmd((byte)Registers.DecodeMode, 0xFF);

            for (var deviceIndex = 0; deviceIndex < _deviceCount; deviceIndex++)
                Clear(deviceIndex);

            SendCmd((byte)Registers.Shutdown, 1);
        }

        /// <inheritdoc />
        public void SendCmd(byte register, byte data)
        {
            EnsureInitialized();
            var packet = new byte[_deviceCount * 2];
            for (var deviceIndex = 0; deviceIndex < _deviceCount; deviceIndex++)
                SetPacketCommand(packet, deviceIndex, register, data);

            _max7219.Write(packet);
        }

        /// <inheritdoc />
        public void SendCmd(byte register, byte data, int deviceIndex)
        {
            EnsureInitialized();
            ValidateDeviceIndex(deviceIndex);
            var packet = new byte[_deviceCount * 2];
            SetPacketCommand(packet, deviceIndex, register, data);
            _max7219.Write(packet);
        }

        /// <inheritdoc />
        public void Clear(int deviceIndex = 0)
        {
            EnsureInitialized();
            ValidateDeviceIndex(deviceIndex);
            SendCmd((byte)Registers.DecodeMode, 0xFF, deviceIndex);
            for (var digit = 0; digit < DigitsPerDevice; digit++)
                SendCmd((byte)((byte)Registers.Digit0 + digit), BlankDigit, deviceIndex);
        }

        /// <inheritdoc />
        public void SetIntensity(int intensity, int deviceIndex = 0)
        {
            if (intensity < 0 || intensity > 15)
                throw new ArgumentOutOfRangeException(nameof(intensity), "Intensity must be between 0 and 15.");

            SendCmd((byte)Registers.Intensity, (byte)intensity, deviceIndex);
        }

        /// <inheritdoc />
        public void SetScanLimit(int digitCount, int deviceIndex = 0)
        {
            if (digitCount < 1 || digitCount > DigitsPerDevice)
                throw new ArgumentOutOfRangeException(nameof(digitCount), "Digit count must be between 1 and 8.");

            SendCmd((byte)Registers.ScanLimit, (byte)(digitCount - 1), deviceIndex);
        }

        /// <inheritdoc />
        public void Shutdown(int deviceIndex = 0)
        {
            SendCmd((byte)Registers.Shutdown, 0, deviceIndex);
        }

        /// <inheritdoc />
        public void WakeUp(int deviceIndex = 0)
        {
            SendCmd((byte)Registers.Shutdown, 1, deviceIndex);
        }

        /// <inheritdoc />
        public void SetDisplayTest(bool enabled, int deviceIndex = 0)
        {
            SendCmd((byte)Registers.DisplayTest, enabled ? (byte)1 : (byte)0, deviceIndex);
        }

        /// <inheritdoc />
        public void SetDigit(int digitIndex, byte value, bool decimalPoint = false, int deviceIndex = 0)
        {
            if (digitIndex < 0 || digitIndex >= DigitsPerDevice)
                throw new ArgumentOutOfRangeException(nameof(digitIndex), "Digit index must be between 0 and 7.");
            if (value > BlankDigit)
                throw new ArgumentOutOfRangeException(nameof(value), "Decoded digit value must be between 0 and 15.");

            SendCmd((byte)Registers.DecodeMode, 0xFF, deviceIndex);
            var data = decimalPoint ? (byte)(value | 0x80) : value;
            SendCmd((byte)((byte)Registers.Digit0 + digitIndex), data, deviceIndex);
        }

        /// <inheritdoc />
        public void DisplayNumber(int number, bool showAll = false)
        {
            DisplayNumber(number, showAll, 0);
        }

        /// <inheritdoc />
        public void DisplayNumber(int number, bool showAll, int deviceIndex)
        {
            DisplayText(number.ToString(CultureInfo.InvariantCulture), showAll, deviceIndex);
        }

        /// <inheritdoc />
        public void DisplayNumber(decimal number, bool showAll = false)
        {
            DisplayNumber(number, showAll, 0);
        }

        /// <inheritdoc />
        public void DisplayNumber(decimal number, bool showAll, int deviceIndex)
        {
            DisplayText(number.ToString(CultureInfo.InvariantCulture), showAll, deviceIndex);
        }

        /// <inheritdoc />
        public void DisplayText(string text, bool showAll = false)
        {
            DisplayText(text, showAll, 0);
        }

        /// <inheritdoc />
        public void DisplayText(string text, bool showAll, int deviceIndex)
        {
            EnsureInitialized();
            ValidateDeviceIndex(deviceIndex);
            var glyphs = ParseText(text);
            if (glyphs.Count > DigitsPerDevice)
                throw new ArgumentOutOfRangeException(nameof(text), "Text exceeds the 8-digit display width.");

            SendCmd((byte)Registers.DecodeMode, 0xFF, deviceIndex);
            SetScanLimit(showAll ? DigitsPerDevice : Math.Max(1, glyphs.Count), deviceIndex);
            WriteGlyphs(PadGlyphs(glyphs, DigitsPerDevice, showAll), deviceIndex);
        }

        /// <inheritdoc />
        public void DisplayTextAcrossChain(string text, bool showAll = false)
        {
            EnsureInitialized();
            var glyphs = ParseText(text);
            var width = _deviceCount * DigitsPerDevice;
            if (glyphs.Count > width)
                throw new ArgumentOutOfRangeException(nameof(text), "Text exceeds the display-chain width.");

            WriteGlyphWindow(PadGlyphs(glyphs, width, showAll));
        }

        /// <inheritdoc />
        public async Task ScrollTextAsync(string text, TimeSpan interval, CancellationToken cancellationToken = default(CancellationToken))
        {
            EnsureInitialized();
            if (interval <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be positive.");

            var glyphs = ParseText(text);
            var width = _deviceCount * DigitsPerDevice;
            var window = new byte[width];
            for (var start = -width; start <= glyphs.Count; start++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (var position = 0; position < width; position++)
                {
                    var glyphIndex = start + position;
                    window[position] = glyphIndex >= 0 && glyphIndex < glyphs.Count ? glyphs[glyphIndex] : BlankDigit;
                }

                WriteGlyphWindow(window);
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public void SetMatrixRow(int row, byte value, int deviceIndex = 0)
        {
            if (row < 0 || row >= DigitsPerDevice)
                throw new ArgumentOutOfRangeException(nameof(row), "Matrix row must be between 0 and 7.");

            SetMatrixMode(deviceIndex);
            SendCmd((byte)((byte)Registers.Digit0 + row), value, deviceIndex);
        }

        /// <inheritdoc />
        public void DisplayBitmap(byte[] rows, int deviceIndex = 0, bool rotate180 = false)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));
            if (rows.Length != DigitsPerDevice)
                throw new ArgumentException("A bitmap must contain exactly 8 rows.", nameof(rows));

            SetMatrixMode(deviceIndex);
            for (var row = 0; row < DigitsPerDevice; row++)
            {
                var value = rotate180 ? ReverseBits(rows[DigitsPerDevice - 1 - row]) : rows[row];
                SendCmd((byte)((byte)Registers.Digit0 + row), value, deviceIndex);
            }
        }

        /// <inheritdoc />
        public void ClearMatrix(int deviceIndex = 0)
        {
            SetMatrixMode(deviceIndex);
            for (var row = 0; row < DigitsPerDevice; row++)
                SendCmd((byte)((byte)Registers.Digit0 + row), 0, deviceIndex);
        }

        /// <summary>Создает SPI-устройство с заданными параметрами шины.</summary>
        private Task<bool> Initialize(int busId, int chipSelect, int clock, SpiMode spiMode, int deviceCount)
        {
            if (deviceCount < 1 || deviceCount > int.MaxValue / DigitsPerDevice)
                throw new ArgumentOutOfRangeException(nameof(deviceCount), "Device count must fit in the supported display-chain size.");

            var device = SpiDevice.Create(new SpiConnectionSettings(busId, chipSelect)
            {
                DataBitLength = 8,
                ClockFrequency = clock,
                Mode = spiMode
            });

            _max7219?.Dispose();
            _max7219 = device;
            _deviceCount = deviceCount;
            return Task.FromResult(true);
        }

        private void SetMatrixMode(int deviceIndex)
        {
            EnsureInitialized();
            ValidateDeviceIndex(deviceIndex);
            SendCmd((byte)Registers.DecodeMode, 0, deviceIndex);
            SetScanLimit(DigitsPerDevice, deviceIndex);
        }

        private void WriteGlyphs(byte[] glyphs, int deviceIndex)
        {
            for (var position = 0; position < glyphs.Length; position++)
            {
                var digitIndex = glyphs.Length - 1 - position;
                SendCmd((byte)((byte)Registers.Digit0 + digitIndex), glyphs[position], deviceIndex);
            }
        }

        private void WriteGlyphWindow(byte[] glyphs)
        {
            for (var deviceIndex = 0; deviceIndex < _deviceCount; deviceIndex++)
            {
                SendCmd((byte)Registers.DecodeMode, 0xFF, deviceIndex);
                SetScanLimit(DigitsPerDevice, deviceIndex);
            }

            for (var position = 0; position < glyphs.Length; position++)
            {
                var digitFromRight = glyphs.Length - 1 - position;
                var deviceIndex = digitFromRight / DigitsPerDevice;
                var digitIndex = digitFromRight % DigitsPerDevice;
                SendCmd((byte)((byte)Registers.Digit0 + digitIndex), glyphs[position], deviceIndex);
            }
        }

        private static byte[] PadGlyphs(IReadOnlyList<byte> glyphs, int width, bool showAll)
        {
            var result = Enumerable.Repeat(showAll ? (byte)0 : BlankDigit, width).ToArray();
            var offset = width - glyphs.Count;
            var startsWithMinus = showAll && glyphs.Count < width && glyphs.Count > 0 && glyphs[0] == (byte)SpecSymbols.Minus;
            if (startsWithMinus)
            {
                result[0] = glyphs[0];
                offset++;
                for (var i = 1; i < glyphs.Count; i++)
                    result[offset + i - 1] = glyphs[i];
            }
            else
            {
                for (var i = 0; i < glyphs.Count; i++)
                    result[offset + i] = glyphs[i];
            }

            return result;
        }

        private static List<byte> ParseText(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Text must not be null or empty.", nameof(text));

            var glyphs = new List<byte>();
            foreach (var character in text)
            {
                if (character == '.')
                {
                    if (glyphs.Count == 0 || (glyphs[glyphs.Count - 1] & 0x80) != 0)
                        throw new FormatException("A decimal point must follow a display character.");
                    glyphs[glyphs.Count - 1] |= 0x80;
                    continue;
                }

                var symbol = char.ToUpperInvariant(character);
                if (symbol >= '0' && symbol <= '9')
                    glyphs.Add((byte)(symbol - '0'));
                else if (symbol == '-')
                    glyphs.Add((byte)SpecSymbols.Minus);
                else if (symbol == 'E')
                    glyphs.Add((byte)SpecSymbols.E);
                else if (symbol == 'H')
                    glyphs.Add((byte)SpecSymbols.H);
                else if (symbol == 'L')
                    glyphs.Add((byte)SpecSymbols.L);
                else if (symbol == 'P')
                    glyphs.Add((byte)SpecSymbols.P);
                else if (symbol == ' ')
                    glyphs.Add(BlankDigit);
                else
                    throw new FormatException($"Character '{character}' is not supported by the MAX7219 decoder.");
            }

            if (glyphs.Count == 0)
                throw new ArgumentException("Text must contain at least one display character.", nameof(text));

            return glyphs;
        }

        private static byte ReverseBits(byte value)
        {
            byte reversed = 0;
            for (var bit = 0; bit < 8; bit++)
                reversed |= (byte)(((value >> bit) & 1) << (7 - bit));
            return reversed;
        }

        private void SetPacketCommand(byte[] packet, int deviceIndex, byte register, byte data)
        {
            var packetIndex = (_deviceCount - 1 - deviceIndex) * 2;
            packet[packetIndex] = register;
            packet[packetIndex + 1] = data;
        }

        private void ValidateDeviceIndex(int deviceIndex)
        {
            if (deviceIndex < 0 || deviceIndex >= _deviceCount)
                throw new ArgumentOutOfRangeException(nameof(deviceIndex), "Device index is outside the configured chain.");
        }

        private void EnsureInitialized()
        {
            if (_max7219 == null)
                throw new InvalidOperationException("The MAX7219 device has not been initialized.");
        }

        /// <summary>Извлекает числовой идентификатор шины из ее имени.</summary>
        private static int ParseBusId(string busName)
        {
            if (string.IsNullOrWhiteSpace(busName))
                throw new ArgumentException("An SPI bus name is required.", nameof(busName));

            var digits = new string(busName.Where(char.IsDigit).ToArray());
            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var busId))
                throw new ArgumentException("The SPI bus name must contain a numeric bus ID.", nameof(busName));

            return busId;
        }
    }
}
