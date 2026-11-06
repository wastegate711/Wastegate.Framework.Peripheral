# PCF8575

Кроссплатформенная библиотека для работы с расширителем портов PCF8575 через I2C. Для работы используется пакет `System.Device.Gpio`.

Инициализация по умолчанию использует I2C bus 1:

```csharp
IPcf8575 pcf8575 = new Pcf8575();
await pcf8575.Initialization(0x20);
```

Чтобы выбрать шину явно, передайте ее номер в строке, например `i2c1` или `/dev/i2c-1`:

```csharp
await pcf8575.Initialization("i2c1", 0x20);
```

Для обработки прерываний передайте номер GPIO и, при необходимости, интервал подавления дребезга:

```csharp
await pcf8575.Initialization("i2c1", 0x20, pinInterrupt: 17, timeout: TimeSpan.FromMilliseconds(10));
pcf8575.PinChanged += pins => Console.WriteLine(string.Join(", ", pins));
```

`SetPinState(ushort pin)` изменяет состояние выводов, `ReadPinState()` считывает два байта состояния, а `ResetState()` устанавливает все выводы в высокий уровень. После работы вызовите `Dispose()`.

Номера шин и GPIO задаются согласно конфигурации конкретной платы и ОС. Библиотека не меняет частоту I2C: она определяется системным драйвером.
