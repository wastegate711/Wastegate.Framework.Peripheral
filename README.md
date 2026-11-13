# Wastegate.Framework.Peripheral

Набор .NET-библиотек для управления периферийными микросхемами через SPI и I2C. Проекты нацелены на `netstandard2.0` и используют `System.Device.Gpio` для доступа к шинам и GPIO.

## Библиотеки и NuGet-пакеты

| Устройство | Возможности | NuGet-пакет |
| --- | --- | --- |
| MAX7219 | 7-сегментные дисплеи и матрицы 8x8, подключенные по SPI | `Wastegate.Framework.Max7219` |
| PCF8574 | 8-битный расширитель GPIO по I2C, чтение состояния и обнаружение изменений через вход прерывания | `Wastegate.Framework.Peripheral` |
| PCF8575 | 16-битный расширитель GPIO по I2C, чтение/сброс состояния и обнаружение изменений через вход прерывания | `Wastegate.Framework.Peripheral` или `Wastegate.Framework.Pcf8575` |

Установите нужный пакет из NuGet. Для доступа к устройству также должны быть включены соответствующие SPI/I2C-интерфейсы и разрешения ОС. I2C-адрес, шину и подключение GPIO-прерывания выбирайте согласно своей плате и схеме.

Подробные страницы: [PCF8575](https://github.com/wastegate711/Wastegate.Framework.Peripheral/wiki/PCF8575), [MAX7219](https://github.com/wastegate711/Wastegate.Framework.Peripheral/wiki/MAX7219).

## MAX7219

MAX7219 управляет либо одним 8-разрядным 7-сегментным индикатором, либо одной матрицей 8x8. Он подключается к SPI. Один драйвер библиотеки может работать с одним устройством или с последовательной цепочкой нескольких MAX7219.

### Инициализация

```csharp
using System;
using System.Device.Spi;
using MAX7219;

using var display = new Max7219();
await display.Initialization(chipSelect: 0, clock: 10_000_000);
display.Max7219Config();

display.SetIntensity(8);
display.DisplayNumber(1234);
```

`Initialization` открывает SPI-устройство и возвращает `Task<bool>`. Вариант без имени шины использует SPI-шину 0; для другой шины передайте ее имя или номер, например `Initialization("SPI0", chipSelect: 0, clock: 10_000_000)`. Частота задается в герцах, а режим SPI по умолчанию — `SpiMode.Mode0`.

`Max7219Config` настраивает все зарегистрированные модули: выключает тест дисплея, включает декодирование цифр, выбирает 8 разрядов, задает начальную яркость, очищает разряды и включает дисплей. Вызовите его после инициализации.

### 7-сегментный дисплей

- `DisplayNumber(int)` выводит целое число, включая знак минуса.
- `DisplayNumber(decimal)` выводит десятичное число; точка формируется из инвариантного представления числа, независимо от региональных настроек.
- `DisplayText(string)` выводит строку цифр и поддерживаемых декодером символов: пробел, `-`, `E`, `H`, `L`, `P`. Регистр букв не важен. Точка `.` добавляется к предыдущему символу и не занимает отдельный разряд.
- `showAll: true` включает отображение всех восьми разрядов с ведущими нулями; по умолчанию строка выравнивается по правому краю и незадействованные разряды гасятся.
- Один MAX7219 отображает не более восьми символов-разрядов. Более длинная строка, неподдерживаемые символы, пустая строка и точка без предыдущего символа приводят к исключению, а не к обрезанию текста.

```csharp
display.DisplayNumber(-42);
display.DisplayNumber(12.34m);
display.DisplayText(" 12.34");
display.DisplayText("HELP");
display.DisplayText("00000042", showAll: true);
```

Другие методы управления:

| Метод | Назначение |
| --- | --- |
| `Clear()` | Гасит все разряды 7-сегментного дисплея. |
| `SetDigit(digitIndex, value, decimalPoint)` | Записывает цифру или код декодера в выбранный разряд (индекс от 0 до 7); при необходимости включает точку. |
| `SetIntensity(intensity)` | Задает яркость от 0 до 15. |
| `SetScanLimit(digitCount)` | Задает число сканируемых разрядов от 1 до 8. |
| `Shutdown()` / `WakeUp()` | Выключает дисплей с сохранением настроек / возвращает его в работу. |
| `SetDisplayTest(enabled)` | Включает или выключает тест всех сегментов/светодиодов. |
| `SendCmd(register, data)` | Отправляет значение в регистр всех модулей цепочки. |

Диапазоны параметров проверяются библиотекой; операции с дисплеем до успешной инициализации завершаются `InvalidOperationException`.

### Цепочка MAX7219

Передайте количество последовательно соединенных драйверов при инициализации. В перегрузке с номером SPI-шины сначала задаются режим SPI и количество устройств:

```csharp
using var display = new Max7219();
await display.Initialization(
    chipSelect: 0,
    clock: 10_000_000,
    spiMode: SpiMode.Mode0,
    deviceCount: 4);
display.Max7219Config();

display.DisplayTextAcrossChain("HELP 123");
await display.ScrollTextAsync(
    "HELP 123",
    TimeSpan.FromMilliseconds(200));
```

Индекс устройства начинается с `0` у контроллера. Методы с параметром `deviceIndex` адресуют один модуль; `SendCmd(register, data)` без индекса передает одну команду всем модулям. `DisplayTextAcrossChain` размещает строку по правому краю общей ширины цепочки. `ScrollTextAsync` сдвигает строку через цепочку с указанным интервалом; передайте `CancellationToken`, чтобы отменить прокрутку.

### Матрица 8x8

Для матрицы используйте методы `DisplayBitmap`, `SetMatrixRow` и `ClearMatrix`. `DisplayBitmap` принимает ровно восемь байтов: каждый байт задает строку, отдельные биты — светодиоды этой строки. Физическое соответствие старшего/младшего бита колонкам зависит от ориентации матричного модуля. Параметр `rotate180: true` меняет порядок строк и разворачивает биты в каждой строке.

```csharp
byte[] bitmap =
{
    0b00111100,
    0b01000010,
    0b10100101,
    0b10000001,
    0b10100101,
    0b10011001,
    0b01000010,
    0b00111100
};

display.DisplayBitmap(bitmap);
display.SetMatrixRow(row: 0, value: 0b11111111);
display.ClearMatrix();
```

Матрица и 7-сегментный индикатор используют одни регистры разрядов, поэтому это альтернативные режимы одного MAX7219. Библиотека переключает режим декодирования при вызове соответствующих методов. Перед выводом текста на ранее используемую матрицу вызовите `Clear()` или заполните все разряды методом вывода, чтобы очистить старые пиксели.

После использования освобождайте SPI-устройство через `Dispose()` или `using`.

## PCF8574 и PCF8575

PCF8574 предоставляет 8 линий GPIO, а PCF8575 — 16. Обе микросхемы управляются через I2C. Для инициализации можно указать только адрес (используется I2C-шина 1) или передать имя/номер шины, например `"i2c1"`.

```csharp
using PeripheralDevices.PortExpanders.PCF8574;

using IPcf8574 expander = new Pcf8574();
await expander.Initialization("i2c1", address: 0x20);

byte state = expander.ReadPinState();
expander.SetPinState(0b0000_0001); // Переключить состояние линии, заданной маской.
```

Для PCF8575 доступны два пространства имен: `PCF8575` в пакете `Wastegate.Framework.Pcf8575` и `PeripheralDevices.PortExpanders.PCF8575` в пакете `Wastegate.Framework.Peripheral`.

```csharp
using PCF8575;

using IPcf8575 expander = new Pcf8575();
await expander.Initialization("i2c1", address: 0x20);

byte[] state = expander.ReadPinState();
expander.SetPinState(0b0000_0000_0000_0001);
expander.ResetState(); // Установить все линии в высокий уровень.
```

`SetPinState` именно инвертирует состояния линий, выбранных битами маски; это не запись абсолютного уровня. `ReadPinState` возвращает текущее состояние: один байт для PCF8574 и два байта для PCF8575. PCF8575 предоставляет `ResetState`, который записывает высокий уровень на все линии.

Обе библиотеки могут следить за входом прерывания микросхемы: подпишитесь на `PinChanged` и укажите GPIO-пин `pinInterrupt` при инициализации. PCF8574 сообщает о спадающем фронте; PCF8575 — о спадающем и нарастающем фронтах. Событие передает считанное состояние выводов. Для PCF8575 можно дополнительно задать `timeout` — минимальный интервал между обработанными событиями.

Пример для PCF8574:

```csharp
using System;
using PeripheralDevices.PortExpanders.PCF8574;

using var interruptExpander = new Pcf8574();
interruptExpander.PinChanged += state => Console.WriteLine($"Состояние: 0x{state:X2}");
await interruptExpander.Initialization("i2c1", address: 0x20, pinInterrupt: 17);
```

Для PCF8575 обработчик получает два байта:

```csharp
using System;
using PCF8575;

using var interruptExpander = new Pcf8575();
interruptExpander.PinChanged += state => Console.WriteLine(BitConverter.ToString(state));
await interruptExpander.Initialization(
    "i2c1",
    address: 0x20,
    pinInterrupt: 17,
    timeout: TimeSpan.FromMilliseconds(20));
```

Не забудьте отключить обработчик события при завершении работы объекта, если подписчик живет дольше расширителя. Освобождайте I2C-устройство и GPIO через `Dispose()` или `using`.

## Сборка и публикация NuGet-пакетов

Проекты используют SDK-style и `netstandard2.0`; каждый проект с `PackageId` автоматически собирается в NuGet-пакет. Push в `develop` создает предварительный GitHub Release с пакетами версии `0.0.0-develop.N`. Push Git-тега SemVer, например `1.2.3` или `v1.2.3`, публикует стабильные пакеты в NuGet.org и создает GitHub Release. Для публикации стабильных версий в настройках репозитория должен быть задан Actions secret `NUGET_API_KEY`.

Пакеты также доступны во вкладке [Releases](https://github.com/wastegate711/Wastegate.Framework.Peripheral/releases).
