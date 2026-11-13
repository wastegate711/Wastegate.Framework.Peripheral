using System;
using System.Threading;
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
        Task<bool> Initialization(int chipSelect, int clock, SpiMode spiMode = SpiMode.Mode0);

        /// <summary>Открывает цепочку MAX7219 на SPI-шине с номером 0.</summary>
        Task<bool> Initialization(int chipSelect, int clock, SpiMode spiMode, int deviceCount);

        /// <summary>Открывает устройство на указанной SPI-шине.</summary>
        /// <param name="busName">Имя или номер шины, содержащие числовой идентификатор, например <c>SPI0</c>.</param>
        /// <param name="chipSelect">Номер линии выбора устройства.</param>
        /// <param name="clock">Частота SPI в герцах.</param>
        /// <param name="spiMode">Режим синхронизации SPI.</param>
        Task<bool> Initialization(string busName, int chipSelect, int clock, SpiMode spiMode = SpiMode.Mode0);

        /// <summary>Открывает цепочку MAX7219 на указанной SPI-шине.</summary>
        Task<bool> Initialization(string busName, int chipSelect, int clock, SpiMode spiMode, int deviceCount);

        /// <summary>Записывает начальные настройки регистров всех модулей.</summary>
        void Max7219Config();

        /// <summary>Записывает значение в указанный регистр всех модулей.</summary>
        void SendCmd(byte register, byte data);

        /// <summary>Записывает значение в регистр выбранного модуля; индекс 0 соответствует модулю у контроллера.</summary>
        void SendCmd(byte register, byte data, int deviceIndex);

        /// <summary>Удаляет содержимое 7-сегментного дисплея.</summary>
        void Clear(int deviceIndex = 0);

        /// <summary>Устанавливает яркость в диапазоне от 0 до 15.</summary>
        void SetIntensity(int intensity, int deviceIndex = 0);

        /// <summary>Устанавливает число отображаемых разрядов от 1 до 8.</summary>
        void SetScanLimit(int digitCount, int deviceIndex = 0);

        /// <summary>Переводит модуль в энергосберегающий режим.</summary>
        void Shutdown(int deviceIndex = 0);

        /// <summary>Выводит модуль из энергосберегающего режима.</summary>
        void WakeUp(int deviceIndex = 0);

        /// <summary>Включает или выключает тест всех сегментов.</summary>
        void SetDisplayTest(bool enabled, int deviceIndex = 0);

        /// <summary>Записывает цифру или специальный код в разряд 0-7.</summary>
        void SetDigit(int digitIndex, byte value, bool decimalPoint = false, int deviceIndex = 0);

        /// <summary>Выводит целое число на модуль 0.</summary>
        void DisplayNumber(int number, bool showAll = false);

        /// <summary>Выводит целое число на выбранный модуль.</summary>
        void DisplayNumber(int number, bool showAll, int deviceIndex);

        /// <summary>Выводит десятичное число, используя инвариантный разделитель-точку.</summary>
        void DisplayNumber(decimal number, bool showAll = false);

        /// <summary>Выводит десятичное число на выбранный модуль.</summary>
        void DisplayNumber(decimal number, bool showAll, int deviceIndex);

        /// <summary>Выводит строку цифр и поддерживаемых символов на модуль 0.</summary>
        void DisplayText(string text, bool showAll = false);

        /// <summary>Выводит строку цифр и поддерживаемых символов на выбранный модуль.</summary>
        void DisplayText(string text, bool showAll, int deviceIndex);

        /// <summary>Выравнивает строку по правому краю цепочки модулей.</summary>
        void DisplayTextAcrossChain(string text, bool showAll = false);

        /// <summary>Прокручивает текст через всю цепочку модулей.</summary>
        Task ScrollTextAsync(string text, TimeSpan interval, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>Записывает строку матрицы 8x8 в режиме без декодирования.</summary>
        void SetMatrixRow(int row, byte value, int deviceIndex = 0);

        /// <summary>Отображает восемь строк матрицы; каждый бит задает один светодиод.</summary>
        void DisplayBitmap(byte[] rows, int deviceIndex = 0, bool rotate180 = false);

        /// <summary>Очищает матрицу 8x8.</summary>
        void ClearMatrix(int deviceIndex = 0);
    }
}
