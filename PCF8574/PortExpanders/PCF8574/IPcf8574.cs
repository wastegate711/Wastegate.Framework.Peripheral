using System;
using System.Threading.Tasks;

namespace PeripheralDevices.PortExpanders.PCF8574
{
    /// <summary>Управляет выводами расширителя портов PCF8574 через I2C.</summary>
    public interface IPcf8574 : IDisposable
    {
        /// <summary>Возникает при изменении состояния выводов, обнаруженном через вывод прерывания.</summary>
        event Action<byte> PinChanged;

        /// <summary>Открывает устройство на шине I2C с номером 1.</summary>
        /// <param name="address">I2C-адрес устройства.</param>
        /// <returns>Задача, результат которой указывает на успешное открытие устройства.</returns>
        Task<bool> Initialization(int address);

        /// <summary>Открывает устройство и настраивает GPIO для сигнала прерывания.</summary>
        /// <param name="address">I2C-адрес устройства.</param>
        /// <param name="pinInterrupt">Номер GPIO, подключенного к выводу прерывания.</param>
        /// <returns>Задача, результат которой указывает на успешную инициализацию.</returns>
        Task<bool> Initialization(int address, int pinInterrupt);

        /// <summary>Открывает устройство на указанной шине I2C.</summary>
        /// <param name="interfaceName">Имя или номер шины, содержащие ее числовой идентификатор, например <c>i2c1</c>.</param>
        /// <param name="address">I2C-адрес устройства.</param>
        /// <returns>Задача, результат которой указывает на успешное открытие устройства.</returns>
        Task<bool> Initialization(string interfaceName, int address);

        /// <summary>Открывает устройство на указанной шине и настраивает GPIO для сигнала прерывания.</summary>
        /// <param name="interfaceName">Имя или номер шины, содержащие ее числовой идентификатор.</param>
        /// <param name="address">I2C-адрес устройства.</param>
        /// <param name="pinInterrupt">Номер GPIO, подключенного к выводу прерывания.</param>
        /// <returns>Задача, результат которой указывает на успешную инициализацию.</returns>
        Task<bool> Initialization(string interfaceName, int address, int pinInterrupt);

        /// <summary>Инвертирует состояния выбранных выводов микросхемы.</summary>
        /// <param name="pin">Маска выводов, состояние которых требуется изменить.</param>
        void SetPinState(byte pin);

        /// <summary>Считывает состояние всех выводов микросхемы.</summary>
        /// <returns>Байт, содержащий состояние выводов.</returns>
        byte ReadPinState();
    }
}