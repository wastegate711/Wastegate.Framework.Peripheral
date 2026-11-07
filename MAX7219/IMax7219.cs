using System;
using System.Threading.Tasks;
using System.Device.Spi;

namespace MAX7219
{
    /// <summary>Управляет дисплеем на микросхеме MAX7219 через SPI.</summary>
    public interface IMax7219 : IDisposable
    {
        /// <summary>Открывает устройство на SPI-шине с номером 0.</summary>
        /// <param name="chipSelect">Номер линии выбора устройства.</param>
        /// <param name="clock">Частота SPI в герцах.</param>
        /// <param name="spiMode">Режим синхронизации SPI.</param>
        /// <returns>Задача, результат которой указывает на успешную инициализацию.</returns>
        Task<bool> Initialization(int chipSelect, int clock, SpiMode spiMode = SpiMode.Mode0);

        /// <summary>Открывает устройство на указанной SPI-шине.</summary>
        /// <param name="busName">Имя или номер шины, содержащие числовой идентификатор, например <c>SPI0</c>.</param>
        /// <param name="chipSelect">Номер линии выбора устройства.</param>
        /// <param name="clock">Частота SPI в герцах.</param>
        /// <param name="spiMode">Режим синхронизации SPI.</param>
        /// <returns>Задача, результат которой указывает на успешную инициализацию.</returns>
        Task<bool> Initialization(string busName, int chipSelect, int clock, SpiMode spiMode = SpiMode.Mode0);

        /// <summary>Записывает начальные настройки регистров MAX7219.</summary>
        void Max7219Config();

        /// <summary>Записывает значение в указанный регистр дисплея.</summary>
        /// <param name="register">Адрес регистра MAX7219.</param>
        /// <param name="data">Значение для записи.</param>
        void SendCmd(byte register, byte data);

        /// <summary>Выводит целое число на дисплей.</summary>
        /// <param name="number">Отображаемое число.</param>
        /// <param name="showAll">Если <see langword="true"/>, отображает все разряды, включая ведущие нули.</param>
        void DisplayNumber(int number, bool showAll = false);
    }
}