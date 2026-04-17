# Анимации зомби

## Назначение
Animator Controller для базового поведения зомби.

Локальные копии анимаций для независимого использования в проекте

## Состояния
Idle, Walk, Attack, Hit, Death

## Параметры
Speed, IsAttacking, IsDead, Hit

## Использование
Назначить Animator Controller на модель врага (дочерний объект VisualRoot)

## Примечания
- Используется InPlace анимация (движение управляется NavMeshAgent)
- Root Motion отключён
- Оригинальные анимации находятся в Zombie_Animations и не изменяются
- Для variant-врагов допустимо делать отдельные project-side controller-файлы с заменой только movement clip
