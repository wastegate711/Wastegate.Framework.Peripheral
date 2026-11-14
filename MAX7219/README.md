# MAX7219

Библиотека управляет 7-сегментными индикаторами и матрицами 8x8 на контроллере MAX7219 через SPI. Один драйвер поддерживает одно устройство или последовательную цепочку нескольких MAX7219.

## Установка и инициализация

Установите NuGet-пакет `Wastegate.Framework.Max7219`. Включите SPI на плате и укажите номер chip select и частоту в герцах:

```csharp
using System.Device.Spi;
using MAX7219;

using var display = new Max7219();
await display.Initialization(chipSelect: 0, clock: 10_000_000);
display.Max7219Config();
display.SetIntensity(8);
```

Для другой SPI-шины передайте ее имя или номер, например `Initialization("SPI0", chipSelect: 0, clock: 10_000_000)`. Режим SPI по умолчанию — `SpiMode.Mode0`.

## 7-сегментный дисплей

```csharp
display.DisplayNumber(-42);
display.DisplayNumber(12.34m);
display.DisplayText("HELP");
display.DisplayText("00000042", showAll: true);
```

`DisplayText` поддерживает цифры, пробел, `-`, `E`, `H`, `L`, `P` и десятичную точку после символа. Один MAX7219 отображает до восьми разрядов. `SetIntensity` принимает значения от 0 до 15, `SetScanLimit` — от 1 до 8. `Clear`, `Shutdown`, `WakeUp` и `SetDisplayTest` управляют состоянием индикатора.

## Цепочка устройств

Количество последовательно соединенных контроллеров указывается при инициализации:

```csharp
await display.Initialization(
    chipSelect: 0,
    clock: 10_000_000,
    spiMode: SpiMode.Mode0,
    deviceCount: 4);
display.Max7219Config();
display.DisplayTextAcrossChain("HELP 123");
await display.ScrollTextAsync("HELP 123", TimeSpan.FromMilliseconds(200));
```

## Матрица 8x8

Передайте восемь байтов, по одному на строку матрицы:

```csharp
byte[] bitmap =
{
    0b00111100, 0b01000010, 0b10100101, 0b10000001,
    0b10000001, 0b10100101, 0b01000010, 0b00111100
};

display.DisplayBitmap(bitmap);
display.SetMatrixRow(row: 0, value: 0b11111111);
display.ClearMatrix();
```

Сегментный дисплей и матрица используют один MAX7219 и переключают режим его декодирования. После работы освободите SPI-устройство через `Dispose()` или `using`.
