using System;
using System.Threading.Tasks;

namespace PeripheralDevices.PortExpanders.PCF8575
{
    /// <summary>Управляет выводами расширителя портов PCF8575 через I2C.</summary>
    public interface IPcf8575 : IDisposable
    {
        /// <summary>Возникает при изменении состояния выводов, обнаруженном через вывод прерывания.</summary>
        event Action<byte[]> PinChanged;

        /// <summary>Открывает устройство на шине I2C с номером 1.</summary>
        /// <param name="address">I2C-адрес устройства.</param>
        /// <returns>Задача, результат которой указывает на успешное открытие устройства.</returns>
        Task<bool> Initialization(int address);

        /// <summary>Открывает устройство и настраивает GPIO для сигнала прерывания.</summary>
        /// <param name="address">I2C-адрес устройства.</param>
        /// <param name="pinInterrupt">Номер GPIO, подключенного к выводу прерывания.</param>
        /// <param name="timeout">Минимальный интервал между обработанными событиями GPIO.</param>
        /// <returns>Задача, результат которой указывает на успешную инициализацию.</returns>
        Task<bool> Initialization(int address, int pinInterrupt, TimeSpan timeout = default);

        /// <summary>Открывает устройство на указанной шине I2C.</summary>
        /// <param name="interfaceName">Имя или номер шины, содержащие ее числовой идентификатор, например <c>i2c1</c>.</param>
        /// <param name="address">I2C-адрес устройства.</param>
        /// <returns>Задача, результат которой указывает на успешное открытие устройства.</returns>
        Task<bool> Initialization(string interfaceName, int address);

        /// <summary>Открывает устройство на указанной шине и настраивает GPIO для сигнала прерывания.</summary>
        /// <param name="interfaceName">Имя или номер шины, содержащие ее числовой идентификатор.</param>
        /// <param name="address">I2C-адрес устройства.</param>
        /// <param name="pinInterrupt">Номер GPIO, подключенного к выводу прерывания.</param>
        /// <param name="timeout">Минимальный интервал между обработанными событиями GPIO.</param>
        /// <returns>Задача, результат которой указывает на успешную инициализацию.</returns>
        Task<bool> Initialization(string interfaceName, int address, int pinInterrupt, TimeSpan timeout = default);

        /// <summary>Инвертирует состояния выбранных выводов микросхемы.</summary>
        /// <param name="pin">16-битная маска выводов, состояние которых требуется изменить.</param>
        void SetPinState(ushort pin);

        /// <summary>Считывает состояние всех выводов микросхемы.</summary>
        /// <returns>Два байта состояния портов микросхемы.</returns>
        byte[] ReadPinState();

        /// <summary>Устанавливает все выводы в высокий уровень.</summary>
        void ResetState();
    }
}