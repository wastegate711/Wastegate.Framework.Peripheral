using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Device.Gpio;
using System.Device.I2c;

namespace PeripheralDevices.PortExpanders.PCF8574
{
    /// <summary>Управляет восьмиразрядным расширителем портов PCF8574.</summary>
    public class Pcf8574 : IPcf8574
    {
        private I2cDevice _pcf8574;
        private GpioController _gpioController;
        private PinChangeEventHandler _interruptHandler;
        private int? _interruptPin;
        private DateTime _lastInterruptUtc = DateTime.MinValue;
        private readonly object _interruptLock = new object();

        /// <inheritdoc />
        public event Action<byte> PinChanged;

        /// <inheritdoc />
        public Task<bool> Initialization(int address)
        {
            return Initialize(1, address, null);
        }

        /// <inheritdoc />
        public Task<bool> Initialization(int address, int pinInterrupt)
        {
            return Initialize(1, address, pinInterrupt);
        }

        /// <inheritdoc />
        public Task<bool> Initialization(string interfaceName, int address)
        {
            return Initialize(ParseBusId(interfaceName), address, null);
        }

        /// <inheritdoc />
        public Task<bool> Initialization(string interfaceName, int address, int pinInterrupt)
        {
            return Initialize(ParseBusId(interfaceName), address, pinInterrupt);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_gpioController != null)
            {
                if (_interruptPin.HasValue && _interruptHandler != null)
                    _gpioController.UnregisterCallbackForPinValueChangedEvent(_interruptPin.Value, _interruptHandler);

                _gpioController.Dispose();
                _gpioController = null;
            }

            _pcf8574?.Dispose();
            _pcf8574 = null;
        }

        /// <inheritdoc />
        public void SetPinState(byte pin)
        {
            var rxData = new byte[1];
            var txData = new byte[1];

            _pcf8574.Read(rxData);
            txData[0] = (byte)(rxData[0] ^ pin);
            _pcf8574.Write(txData);
        }

        /// <inheritdoc />
        public byte ReadPinState()
        {
            var rxData = new byte[1];
            _pcf8574.Read(rxData);
            return rxData[0];
        }

        /// <summary>Открывает I2C-устройство и при необходимости настраивает GPIO-прерывание.</summary>
        /// <param name="busId">Числовой идентификатор I2C-шины.</param>
        /// <param name="address">I2C-адрес расширителя.</param>
        /// <param name="pinInterrupt">Номер GPIO для прерывания или <see langword="null"/>.</param>
        /// <returns>Задача, результат которой указывает на успешное открытие устройства.</returns>
        private Task<bool> Initialize(int busId, int address, int? pinInterrupt)
        {
            _pcf8574 = I2cDevice.Create(new I2cConnectionSettings(busId, address));

            if (pinInterrupt.HasValue)
            {
                _gpioController = new GpioController();
                _gpioController.OpenPin(pinInterrupt.Value, PinMode.InputPullUp);
                _interruptPin = pinInterrupt.Value;
                _interruptHandler = Interrupt_ValueChanged;
                _gpioController.RegisterCallbackForPinValueChangedEvent(
                    pinInterrupt.Value,
                    PinEventTypes.Falling,
                    _interruptHandler);
            }

            return Task.FromResult(true);
        }

        /// <summary>Считывает новое состояние выводов при срабатывании входа прерывания.</summary>
        /// <param name="sender">Контроллер GPIO, вызвавший обработчик.</param>
        /// <param name="args">Параметры события изменения GPIO.</param>
        private void Interrupt_ValueChanged(object sender, PinValueChangedEventArgs args)
        {
            lock (_interruptLock)
            {
                var now = DateTime.UtcNow;
                if (now - _lastInterruptUtc < TimeSpan.FromMilliseconds(1))
                    return;

                _lastInterruptUtc = now;
            }

            var rxData = new byte[1];
            _pcf8574.Read(rxData);
            PinChanged?.Invoke(rxData[0]);
        }

        /// <summary>Извлекает числовой идентификатор шины из имени.</summary>
        /// <param name="interfaceName">Имя или путь шины, содержащие ее номер.</param>
        /// <returns>Числовой идентификатор шины.</returns>
        /// <exception cref="ArgumentException">Имя шины пустое или не содержит числового идентификатора.</exception>
        private static int ParseBusId(string interfaceName)
        {
            if (string.IsNullOrWhiteSpace(interfaceName))
                throw new ArgumentException("An I2C bus name is required.", nameof(interfaceName));

            var digits = new string(interfaceName.Where(char.IsDigit).ToArray());
            if (!int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var busId))
                throw new ArgumentException("The I2C bus name must contain a numeric bus ID.", nameof(interfaceName));

            return busId;
        }
    }
}
