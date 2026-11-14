# PCF8574

Библиотека управляет 8-битным расширителем GPIO PCF8574 через I2C. Пакет `Wastegate.Framework.Peripheral` использует пространство имен `PeripheralDevices.PortExpanders.PCF8574` и `System.Device.Gpio`.

## Подключение и инициализация

Установите NuGet-пакет `Wastegate.Framework.Peripheral`, подключите микросхему к I2C и укажите адрес, заданный выводами адреса на модуле. Адрес `0x20` в примере условный.

```csharp
using PeripheralDevices.PortExpanders.PCF8574;

using IPcf8574 expander = new Pcf8574();
await expander.Initialization("i2c1", address: 0x20);
```

Если имя шины не указано, используется I2C bus 1. Можно передать имя или номер шины, например `"i2c1"`.

## Чтение и управление линиями

```csharp
byte state = expander.ReadPinState();
expander.SetPinState(0b0000_0001);
```

`ReadPinState` возвращает состояние восьми линий одним байтом. `SetPinState` инвертирует состояния линий, выбранных единичными битами маски; это не запись абсолютного уровня.

## Обработка прерываний

Подпишитесь на `PinChanged` и укажите GPIO-пин, соединенный с выходом прерывания микросхемы:

```csharp
using System;
using PeripheralDevices.PortExpanders.PCF8574;

using var expander = new Pcf8574();
expander.PinChanged += state => Console.WriteLine($"Состояние: 0x{state:X2}");
await expander.Initialization("i2c1", address: 0x20, pinInterrupt: 17);
```

PCF8574 сообщает о спадающем фронте; событие передает считанное состояние выводов. Убедитесь, что выбранный GPIO поддерживает обработку изменения уровня на вашей платформе. После работы освободите I2C-устройство и GPIO через `Dispose()` или `using`.
