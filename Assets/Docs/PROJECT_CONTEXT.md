# Project Context

## Engine

Unity version:
Unity 6.3

Render Pipeline:
URP

Language:
C#

Target Platform:
WebGL (Yandex Games)

---

## Project Type

Fast prototype game.

// Быстрые игровые эксперименты

Current experiment:
FPS zombie shooter.

Target gameplay length:
~10 minutes (after scaling)

Prototype length:
~2–3 minutes

---

## Development Model

// 1 неделя = 1 эксперимент

1 week = 1 experiment

Goal:
Test if the core gameplay mechanic is fun

If success:
continue development (2–3 weeks)

If fail:
abandon project

---

## Core Gameplay Loop

play → explore → fight → take damage →
search resources → rescue → boss → next level

---

## Project Structure

Reusable modules are stored in:

Assets/Modules

Game-specific code is stored in:

Assets/_Project

Modules must NOT depend on project code.

Project code CAN use modules.

---

## Modular Architecture

// Каждая механика = отдельный модуль

Each gameplay mechanic must be implemented as an independent module.

Modules must be:

- reusable
- loosely coupled
- easy to transfer between projects
- easy to modify

Example modules:

- PlayerController
- HealthSystem
- WeaponSystem
- DamageSystem
- EnemyAI_Base
- SceneLoader
- DebugMenu

---

## Module Rules

Each module must:

- work independently
- have minimal dependencies
- be plug-and-play
- not rely on scene-specific setup

Allowed dependencies:

- Unity components
- simple interfaces

Avoid:

- direct dependencies between modules
- references to project-specific code

---

## Interfaces

// Взаимодействие через интерфейсы

Modules should communicate via interfaces.

Example:

IDamageable:
- TakeDamage(int damage)

Avoid direct coupling between systems.

---

## Scene Architecture

// Используем правило 3 сцен

Project uses 3-scene architecture:

Bootstrap
Main
Level

---

### Scene Responsibilities

Bootstrap:
- initialization only
- loads Main and Level

Main:
- player
- camera
- UI
- core systems

Level:
- environment
- enemies
- items
- spawn points

---

### Scene Rules

Main scene is persistent.

Level scenes are loaded/unloaded additively.

Player MUST exist in Main scene.

Level MUST NOT contain player logic.

---

## Code Organization

Each module must:

- be located in Assets/Modules/<ModuleName>
- use namespace: Modules.<ModuleName>
- include assembly definition (.asmdef)
- include README.md

---

## Core Systems

PlayerController:
movement, camera, input

HealthSystem:
HP, damage, healing, death

WeaponSystem:
shooting, ammo, reload

DamageSystem:
damage processing

EnemyAI_Base:
movement, detection, attack

SceneLoader:
scene management

DebugMenu:
testing tools

---

## Level Design

Each building ≈ 2–3 minutes gameplay.

Typical structure:

- entrance
- small fight
- exploration
- resource room
- surprise encounter
- main fight
- preparation
- boss room

---

## Code Style

// Максимально простой код

Prefer:

- MonoBehaviour scripts
- simple logic
- readable code

Avoid:

- over-engineering
- complex patterns
- heavy frameworks

Script size:
100–300 lines

---

## Comments

// ВСЕ комментарии в коде на русском

All code comments must be written in Russian.

Example:

// Движение игрока
// Проверка дистанции

---

## AI Coding Instructions

When generating Unity scripts:

- use Unity 6.3 compatible APIs
- prefer simple solutions
- keep modules independent
- follow MODULE_TEMPLATE.md
- include Russian comments

Use:

- CharacterController for player
- NavMeshAgent for enemies
- Physics.Raycast for shooting

Avoid:

- unnecessary managers
- global singletons (unless necessary)
- complex architectures

---

## Managers Rule

Allowed systems:

- SceneLoader
- DebugMenu

These are lightweight utility modules.

Avoid creating new global managers.

---

## Debug Tools

DebugMenu must include:

- spawn enemy
- heal player
- give ammo
- kill all enemies
- reload level

---

## Performance Constraints

Target platform: WebGL.

Requirements:

- low CPU usage
- low draw calls
- simple logic
- avoid heavy physics

---

## Development Rules

// Если долго — значит слишком сложно

If a feature takes more than 1–2 days:
it is too complex.

Focus on gameplay first.

---

## Important Rule

All gameplay modules MUST follow MODULE_TEMPLATE.md.