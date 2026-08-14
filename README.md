# Adonis

Менеджер аддонов и биндов для Garry's Mod с локальным Web-интерфейсом (WebView2).

## Возможности

- Каталог аддонов (рескины, эффекты) с установкой/удалением прямо в `garrysmod/addons`
- Конструктор биндов (магазин, анимации, профессии, чат/РП) с записью в `adonis_binds.cfg`
- Оптимизация игры (сборка мусора и т.п.)
- Вход через Discord
- Автообновление через GitHub Releases

## Как устроено хранилище аддонов

- `reskins/` — каталог для публикации на GitHub:
  - `catalog.json` — описание аддонов
  - `zips/<id>.zip` — архивы аддонов
  - `previews/<id>.png` — превью
  - `avatars/` — аватары авторов
- Исходники аддонов хранятся локально в `addons-src/<id>/` (не попадают в git)
- Скрипт упаковки: `tools/build-reskins.ps1` — собирает `reskins/` из `addons-src/`

Приложение скачивает `catalog.json` и архивы с `raw.githubusercontent.com/<owner>/<repo>/<branch>/reskins/...`.

## Обновление приложения

Конфигурация — в `appsettings.json`, секция `GitHub`:

```json
"GitHub": {
  "Owner": "<owner>",
  "Repo": "<repo>",
  "Branch": "main",
  "ReleaseAsset": "Adonis.zip"
}
```

Приложение сравнивает свою версию (`InformationalVersion` из csproj) с последним релизом на GitHub. Если есть новая версия — скачивает `Adonis.zip` из релиза, распаковывает и перезапускает себя.

## Сборка

```powershell
dotnet publish ReskinManager.csproj -c Release -o publish
```

## Релиз

```powershell
# 1. Упаковать аддоны
powershell -File tools/build-reskins.ps1

# 2. Собрать и запаковать приложение (без пользовательских данных)
#    Adonis.zip = содержимое publish без auth.json, binds.json, sessions.json, settings.json

# 3. Создать релиз
gh release create v1.0.0 Adonis.zip
```
