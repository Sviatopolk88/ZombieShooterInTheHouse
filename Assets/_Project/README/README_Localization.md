# README_Localization

## 1. Что используется

- В проекте не добавлялась отдельная система локализации поверх `PluginYourGames`.
- Источник истины по языку: `PluginYourGames`, поле `YG2.lang`.
- Для реакции UI на смену языка используется событие `YG2.onSwitchLang`.

## 2. Как выбирается RU / EN

- `PluginYourGames` уже даёт:
  - raw environment language в `YG2.envir.language`
  - нормализованный язык локализации в `YG2.lang`
- Для UI используется именно `YG2.lang`, потому что это штатная абстракция модуля локализации плагина.
- Правило проекта сейчас простое:
  - `ru` -> русский
  - всё остальное -> английский

## 3. Project-side bridge

- Project-side ключи лежат в `Assets/_Project/Scripts/Localization/ProjectLocalizationYG.cs`.
- Этот файл не заменяет локализацию `PluginYourGames`, а только даёт компактный доступ к строкам проекта через текущий язык `YG2.lang`.

## 4. Что уже локализовано

- Rescue HUD:
  - `Assets/_Project/Scripts/UI/HUD/RescueHudPresenter.cs`
  - формат `Спасено: X / Y` / `Rescued: X / Y`
- Result UI:
  - `Assets/_Project/Scripts/UI/DeathScreenController.cs`
  - заголовок death screen
  - текст кнопки повтора

## 5. Как добавлять новые строки

1. Добавить новый ключ в `ProjectTextKey`.
2. Добавить RU/EN значения в `ProjectLocalizationYG.Get(...)`.
3. В нужном presenter/controller брать текст только через `ProjectLocalizationYG`.
4. Если UI должен обновляться на лету, подписать его на `YG2.onSwitchLang`.

## 6. Что проверить руками

- При `YG2.lang = ru` rescue HUD показывает русский текст.
- При `YG2.lang = en` rescue HUD показывает английский текст.
- DeathScreen показывает русский или английский заголовок и кнопку в зависимости от языка.
- При вызове `YG2.SwitchLanguage(...)` открытый UI обновляет текст без перезапуска сцены.
