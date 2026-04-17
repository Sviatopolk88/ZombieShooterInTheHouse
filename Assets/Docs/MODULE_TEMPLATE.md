# Module Template

## Module Name

<ModuleName>

Examples:
PlayerController
HealthSystem
WeaponSystem
EnemyAI_Base

---

## Purpose

// Назначение модуля

Краткое описание функциональности модуля.

---

## Responsibilities

// Что делает модуль

- основная логика
- обработка событий
- взаимодействие с другими системами

---

## Dependencies

// Минимальные зависимости

Allowed:

- Unity components
- simple interfaces

Avoid:

- direct dependencies on other modules
- project-specific references

---

## Public API

// Явный интерфейс модуля

Use clear public methods:

- TakeDamage(int damage)
- Heal(int amount)
- Shoot()

Avoid:

- дублирование Unity lifecycle методов (например Update)

---

## Data

// Основные данные

- HP
- Speed
- Damage
- Ammo

---

## Events (optional)

- OnDeath
- OnDamage
- OnShoot

---

## Interfaces

// Использовать интерфейсы для взаимодействия

Example:

IDamageable:
- TakeDamage(int damage)

---

## Notes

// Важно

- простой
- читаемый
- переносимый
- независимый

---

## Coding Rules

- использовать MonoBehaviour
- комментарии писать на русском
- избегать сложных паттернов
- писать читаемый код
- размер скрипта: 100–300 строк

---

## README

Each module must include README.md with:

- Purpose
- Public API
- Usage example

---

## Example Prompt for Codex

Создай Unity C# модуль <ModuleName>.

Используй:

- PROJECT_CONTEXT.md
- MODULE_TEMPLATE.md

Требования:

- Unity 6.3
- комментарии на русском
- автономный модуль
- минимальные зависимости
- простой и читаемый код

---

## Folder Structure

Assets/Modules/<ModuleName>/

Example:

<ModuleName>/
├ Scripts/
├ Prefabs/
├ <ModuleName>.asmdef
└ README.md

---

## Reusability

Модуль должен:

- легко копироваться в другой проект
- не зависеть от сцены
- не зависеть от других модулей напрямую

---

## Done Criteria

Модуль считается готовым если:

- работает изолированно
- легко подключается к сцене
- не требует сложной настройки
- соответствует PROJECT_CONTEXT.md