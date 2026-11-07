using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Device.Spi;

namespace MAX7219
{
    /// <summary>Реализация управления дисплеем MAX7219 через устройство SPI.</summary>
    public class Max7219 : IMax7219, IDisposable
    {
        private SpiDevice _max7219;

        /// <inheritdoc />
        public Task<bool> Initialization(int chipSelect, int clock, SpiMode spiMode = SpiMode.Mode0)
        {
            return Initialize(0, chipSelect, clock, spiMode);
        }

        /// <inheritdoc />
        public Task<bool> Initialization(string busName, int chipSelect, int clock, SpiMode spiMode = SpiMode.Mode0)
        {
            return Initialize(ParseBusId(busName), chipSelect, clock, spiMode);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _max7219?.Dispose();
            _max7219 = null;
        }

        /// <inheritdoc />
        public void Max7219Config()
        {
            _max7219.Write(new byte[] { (byte)Registers.DisplayTest, 0 });
            _max7219.Write(new byte[] { (byte)Registers.Shutdown, 1 });
            _max7219.Write(new byte[] { (byte)Registers.Intensity, 3 });
            _max7219.Write(new byte[] { (byte)Registers.ScanLimit, 7 });
            _max7219.Write(new byte[] { (byte)Registers.DecodeMode, 0xFF });
            _max7219.Write(new byte[] { (byte)Registers.Digit0, 0 });
        }

        /// <inheritdoc />
        public void SendCmd(byte register, byte data)
        {
            _max7219.Write(new byte[] { register, data });
        }

        /// <inheritdoc />
        public void DisplayNumber(int number, bool showAll = false)
        {
            if (!showAll)
            {
                if (number > 9999999)
                    SendCmd((byte)Registers.ScanLimit, 7);
                else if (number > 999999)
                    SendCmd((byte)Registers.ScanLimit, 6);
                else if (number > 99999)
                    SendCmd((byte)Registers.ScanLimit, 5);
                else if (number > 9999)
                    SendCmd((byte)Registers.ScanLimit, 4);
                else if (number > 999)
                    SendCmd((byte)Registers.ScanLimit, 3);
                else if (number > 99)
                    SendCmd((byte)Registers.ScanLimit, 2);
                else if (number > 9)
                    SendCmd((byte)Registers.ScanLimit, 1);
                else
                {
                    SendCmd((byte)Registers.ScanLimit, 0);
                    return;
                }
            }

            SendCmd((byte)Registers.Digit7, (byte)(number / 10000000));
            SendCmd((byte)Registers.Digit6, (byte)((number / 1000000) % 10));
            SendCmd((byte)Registers.Digit5, (byte)((number / 100000) % 10));
            SendCmd((byte)Registers.Digit4, (byte)((number / 10000) % 10));
            SendCmd((byte)Registers.Digit3, (byte)((number / 1000) % 10));
            SendCmd((byte)Registers.Digit2, (byte)((number / 100) % 10));
            SendCmd((byte)Registers.Digit1, (byte)((number / 10) % 10));
            SendCmd((byte)Registers.Digit0, (byte)(number % 10));
        }

        /// <summary>Создает SPI-устройство с заданными параметрами шины.</summary>
        /// <param name="busId">Числовой идентификатор SPI-шины.</param>
        /// <param name="chipSelect">Номер линии выбора устройства.</param>
        /// <param name="clock">Частота SPI в герцах.</param>
        /// <param name="spiMode">Режим синхронизации SPI.</param>
        /// <returns>Задача, результат которой указывает на успешное создание устройства.</returns>
        private Task<bool> Initialize(int busId, int chipSelect, int clock, SpiMode spiMode)
        {
            _max7219 = SpiDevice.Create(new SpiConnectionSettings(busId, chipSelect)
            {
                DataBitLength = 8,
                ClockFrequency = clock,
                Mode = spiMode
            });

            return Task.FromResult(true);
        }

        /// <summary>Извлекает числовой идентификатор шины из ее имени.</summary>
        /// <param name="busName">Имя или путь шины, содержащие ее номер.</param>
        /// <returns>Числовой идентификатор SPI-шины.</returns>
        /// <exception cref="ArgumentException">Имя шины пустое или не содержит числового идентификатора.</exception>
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