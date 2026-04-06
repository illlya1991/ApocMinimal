# ApocMinimal — инструкции для Claude

## Git
- Работать и пушить **только в ветку `master`**
- После каждого коммита сразу делать `git push origin master`
- `origin` уже содержит токен — `git push origin master` работает без дополнительных параметров

## VERSION
- Файл `VERSION` в корне проекта — номер текущей итерации
- **Увеличивать на 1 при каждом изменении** и коммитить вместе с изменениями
- Текущая версия: см. файл VERSION

## Проект
- WPF-приложение на C# (.NET 8), **не собирается на Linux** (только на Windows)
- Проверку кода делать вручную — синтаксис, логику, связность типов
- База данных: SQLite через `DatabaseManager.cs`
- Глобальные using: `System`, `System.Collections.Generic`, `System.Linq` (не нужно добавлять вручную)

## Структура
- `Models/` — модели данных (Player, Npc, Quest, Location, Resource, Need, Emotion, ...)
- `Systems/` — игровые системы (ActionSystem, NeedSystem, QuestSystem, CombatSystem)
- `Database/` — DatabaseManager (SQLite CRUD)
- `GameWindow.xaml.cs` — главное окно, игровой цикл
- `StartWindow.xaml.cs` — стартовый экран

## Правила разработки
- Показывать номер действия (VERSION) при каждом изменении
- Следовать концепции игры (см. историю чата)
- Не добавлять лишних комментариев, docstring, обёрток
