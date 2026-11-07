# Wastegate.Framework.Peripheral
Библиотеки для работы с дисплеями, драйверами и разными электронными девайсами.

[PCF8575](https://github.com/wastegate711/Wastegate.Framework.Peripheral/wiki/PCF8575)

[MAX7219](https://github.com/wastegate711/Wastegate.Framework.Peripheral.wiki.git)


## NuGet-пакеты

Проекты используют SDK-style и `netstandard2.0`; каждый проект с `PackageId` автоматически собирается в NuGet-пакет. Для новой библиотеки достаточно добавить SDK-style `.csproj` в solution, указать в нем `PackageId` и включить нужные зависимости — отдельный `.nuspec` и правки workflow не нужны.

Push в `develop` создает предварительный GitHub Release с пакетами версии `0.0.0-develop.N`. Push Git-тега SemVer, например `1.2.3` или `v1.2.3`, публикует стабильные пакеты в NuGet.org и создает GitHub Release. Для публикации стабильных версий в настройках репозитория должен быть задан Actions secret `NUGET_API_KEY`.

Пакеты также доступны во вкладке [Releases](https://github.com/wastegate711/Wastegate.Framework.Peripheral/releases).
