# Wastegate.Framework.Peripheral
Библиотеки для работы с дисплеями, драйверами и разными электронными девайсами.

[PCF8575](https://github.com/wastegate711/Wastegate.Framework.Peripheral/wiki/PCF8575)

[MAX7219](https://github.com/wastegate711/Wastegate.Framework.Peripheral.wiki.git)

### MAX7219: основные возможности

```csharp
var display = new MAX7219.Max7219();
await display.Initialization(chipSelect: 0, clock: 10_000_000);
display.Max7219Config();
display.SetIntensity(8);
display.DisplayNumber(-12.34m);
display.DisplayText("12.34");
await display.ScrollTextAsync("HELLO", TimeSpan.FromMilliseconds(250));
```

Для цепочки модулей укажите число MAX7219 при инициализации:
`Initialization(chipSelect, clock, SpiMode.Mode0, deviceCount)`. Индекс модуля начинается с 0 у контроллера. `DisplayTextAcrossChain` выравнивает строку по правому краю всей цепочки; `ScrollTextAsync` прокручивает ее. Поддерживаются цифры, пробел, минус и декодируемые символы E, H, L, P; точка добавляется к предыдущему символу.

Для матрицы 8x8 используйте `DisplayBitmap` с массивом из восьми байтов (каждый бит — светодиод строки), `SetMatrixRow` или `ClearMatrix`. `rotate180: true` поворачивает изображение на 180 градусов. При переключении между матрицей и 7-сегментным режимом библиотека настраивает режим декодирования автоматически.

## NuGet-пакеты

Проекты используют SDK-style и `netstandard2.0`; каждый проект с `PackageId` автоматически собирается в NuGet-пакет. Для новой библиотеки достаточно добавить SDK-style `.csproj` в solution, указать в нем `PackageId` и включить нужные зависимости — отдельный `.nuspec` и правки workflow не нужны.

Push в `develop` создает предварительный GitHub Release с пакетами версии `0.0.0-develop.N`. Push Git-тега SemVer, например `1.2.3` или `v1.2.3`, публикует стабильные пакеты в NuGet.org и создает GitHub Release. Для публикации стабильных версий в настройках репозитория должен быть задан Actions secret `NUGET_API_KEY`.

Пакеты также доступны во вкладке [Releases](https://github.com/wastegate711/Wastegate.Framework.Peripheral/releases).
