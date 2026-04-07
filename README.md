# SWOSH — Mobile FPS 2v2 Shooter

Unity-проект мобильного FPS-шутера с раундовой системой 2v2 (игрок + союзник vs 2 врага).

## Геймплей

- **Раундовая система 2v2** — до 10 побед. Каждый раунд: фаза покупки (10 сек) → бой (180 сек) → пауза.
- **3 типа оружия**: Fast (автоматический, 1 урон), Heavy (медленный, 3 урона), Roklet (средний, 2 урона) + нож в ближнем бою.
- **Магазинная система боеприпасов** с перезарядкой.
- **Магазин оружия** — покупка оружия в фазе покупки перед раундом.
- **AI-боты** — враги с продвинутым pathfinding (обход препятствий, strafe, настраиваемая сложность), союзник помогает в бою.
- **Мобильное управление** — виртуальный джойстик, свайп-камера, кнопки стрельбы/прыжка/приседания.
- **Desktop-поддержка** — WASD + мышь в редакторе.

## Архитектура проекта

```
хуй/Assets/
├── Scripts/
│   ├── MobileShoot.cs          — Основная система стрельбы (3 оружия + нож)
│   ├── MobileMovement.cs       — Мобильное движение (джойстик, прыжок, присед)
│   ├── MobileCameraSwipe.cs    — Свайп-камера для мобильных
│   ├── FpsCameraController.cs  — Desktop мышиная камера
│   ├── PlayerHealth.cs         — HP игрока, смерть, респаун
│   ├── EnemyHealth.cs          — HP врагов, хит-флеш, звук
│   ├── AllyHealth.cs           — HP союзника
│   ├── EnemyChase.cs           — AI врагов (pathfinding, стрейф, стрельба, сложность)
│   ├── AllyChase.cs            — AI союзника (поиск цели, стрельба)
│   ├── MagicBullet.cs          — Физика пуль (Rigidbody, урон по команде, VFX)
│   ├── WeaponRecoil.cs         — Отдача камеры/оружия (kick + return)
│   ├── WeaponSway.cs           — Покачивание оружия при повороте камеры
│   ├── CrosshairUI.cs          — Динамический прицел (расширение, цвет, ADS)
│   ├── CrosshairProfile.cs     — ScriptableObject настроек прицела per-weapon
│   ├── ADSController.cs        — Aim-down-sights (FOV zoom, чувствительность)
│   ├── Round2v2Manager.cs      — State machine матча (PreRound/Round/RoundEnd/MatchOver)
│   ├── GameManager.cs          — Legacy kill-counter (30 kills = MVP)
│   ├── MatchStats.cs           — Глобальная K/D статистика для 4 акторов
│   ├── EnemyKillCounter2v2.cs  — Подсчёт убийств врагов через event
│   ├── PlayerSpawner.cs        — Спавн игрока
│   ├── EnemySpawner.cs         — Спавн врагов (Training Waves / 2v2 Rounds)
│   ├── AllySpawner.cs          — Спавн/ресет союзника
│   ├── Team.cs                 — Enum: Player, Enemy
│   ├── ActorId.cs              — Enum: None, Player, Ally, Enemy1, Enemy2
│   ├── ActorIdentity.cs        — Компонент для присвоения ActorId объекту
│   ├── PlayerAim.cs            — Позиционирование firePoint по орбите (legacy)
│   ├── CameraFollow.cs         — Top-down камера (debug)
│   ├── ScoreboardUI.cs         — Таблица K/D в раунде
│   ├── HoldToShowScoreboard.cs — Hold-to-show UX для скорборда
│   ├── MatchOverUI.cs          — Экран конца матча (ПОБЕДА/ПОРАЖЕНИЕ)
│   ├── FloatingJoystickVisual.cs — Визуал плавающего джойстика
│   ├── ShopButtons.cs          — Кнопки покупки оружия
│   ├── NavHints.cs             — AI-хелпер для навигации через вейпоинты
│   ├── TPPArmedToggle.cs       — IK-система для рук (per-weapon grip)
│   ├── CharacterBodySelector.cs — Переключение моделей (Undead/Paladin)
│   ├── CharacterSkinSwitcher.cs — Переключение материалов скина
│   ├── WeaponSlotToggle.cs     — Debug: тогл оружия в Editor
│   ├── MuzzleFlashLight.cs     — Вспышка дульного света
│   ├── VFXAutoDestroyRealtime.cs — Авто-удаление VFX
│   ├── HitTesterRay.cs         — Debug визуализация рейкастов
│   ├── IKWalkWeight.cs         — IK-веса при ходьбе/idle
│   └── MainMenu/
│       ├── MenuController.cs       — Навигация главного меню (3 табы)
│       ├── InventoryUIManager.cs   — Спавн карточек инвентаря
│       ├── ItemData.cs             — Данные предмета (имя, редкость, иконка)
│       └── ItemCardUI.cs           — UI карточка предмета
├── Scenes/
│   ├── MainMenu.unity          — Главное меню
│   └── SampleScene.unity       — Основная игровая сцена
├── Animations/                 — FPP/TPP анимации оружия и тел
├── Prefabs/                    — Префабы оружия, VFX, UI
├── Materials/                  — Материалы и скайбокс
├── ExampleAssets/              — Текстуры (металл, дерево, бетон и т.д.)
└── Settings/                   — URP render pipeline настройки
```

## Система прицела (Crosshair)

Динамический прицел, реагирующий на действия игрока:

| Поведение | Описание |
|---|---|
| **Стрельба** | Прицел расширяется с каждым выстрелом, затем плавно сужается |
| **Движение** | Прицел расширяется пропорционально скорости игрока |
| **Наведение на врага** | Цвет меняется (белый → красный) через raycast из центра экрана |
| **Смена оружия** | Прицел переключается на профиль оружия (размер, толщина, spread) |
| **ADS (прицеливание)** | FOV камеры уменьшается (60→40), чувствительность снижается, прицел сужается |
| **Смерть** | Прицел скрывается, появляется при респауне |

### CrosshairProfile (ScriptableObject)

Каждое оружие имеет свой профиль прицела:

| Параметр | Fast | Heavy | Roklet | Описание |
|---|---|---|---|---|
| baseSpread | 10 | 18 | 14 | Базовый разброс (px) |
| maxSpread | 55 | 70 | 60 | Макс. разброс (px) |
| shootExpandAmount | 6 | 16 | 10 | Расширение за выстрел |
| moveExpandAmount | 12 | 10 | 11 | Расширение при беге |
| contractSpeed | 90 | 60 | 75 | Скорость сужения (px/s) |
| lineLength | 14 | 18 | 16 | Длина линии |
| lineThickness | 2 | 3 | 2 | Толщина линии |

### Подключение в Unity

1. В HUD Canvas создать `CrosshairRoot` (anchor = center, size 0x0)
2. Добавить 4 дочерних Image (линии: Top/Bottom/Left/Right) + 1 Image (точка)
3. Повесить `CrosshairUI` на `CrosshairRoot`, назначить все ссылки
4. Создать CrosshairProfile ассеты: **Create → Weapons → Crosshair Profile**
5. В инспекторе назначить `crosshairUI` поле в: `MobileShoot`, `MobileMovement`, `PlayerHealth`
6. Для ADS: добавить `ADSController` + UI-кнопку, вызывающую `ToggleADS()`

## Система боя

```
Ввод игрока (кнопка Shoot)
    │
    ├─ Нож? → DoKnifeAttack() → Raycast 2м из центра экрана → урон
    │
    └─ Оружие? → DoShoot()
        ├─ Raycast из центра viewport
        ├─ Направление: hit.point - firePoint.position
        ├─ Instantiate(MagicBullet) → Launch(dir)
        ├─ WeaponRecoil.Kick(strength)
        └─ CrosshairUI.OnShot()

MagicBullet (физическая пуля)
    ├─ Rigidbody.velocity = direction * speed
    ├─ OnTriggerEnter → проверка команды → TakeDamage()
    └─ Auto-destroy по таймеру
```

## AI система

### Враги (EnemyChase.cs)
- Ближний бой или стрельба (зависит от наличия bulletPrefab)
- Pathfinding с обходом препятствий (memory-based avoidance)
- Застревание — детекция и escape (0.35s)
- Стрейф при атаке на дистанции
- Line-of-sight проверка (30 юнитов, блокируется стенами)
- 3 уровня сложности: Easy (-15% скорость), Normal, Hard (+10% скорость)

### Союзник (AllyChase.cs)
- Поиск ближайшего врага → движение → стрельба
- Без pathfinding (может застревать на стенах)

## Раундовая система (Round2v2Manager)

```
PreRound (10 сек) → Магазин открыт, бой отключён
    │
Round (180 сек) → Активный бой, магазин закрыт
    │ ├─ Обе стороны живы → ждём таймер
    │ ├─ Оба врага мертвы → ПОБЕДА раунда
    │ └─ Игрок + союзник мертвы → ПОРАЖЕНИЕ раунда
    │
RoundEnd (3 сек) → Результат, cleanup
    │
MatchOver → Одна из сторон набрала 10 побед
```

## Технические детали

- **Unity Version**: URP (Universal Render Pipeline)
- **Платформы**: Mobile (Android/iOS), Desktop (Editor)
- **Input**: Touch (джойстик + свайп) / Keyboard + Mouse
- **Физика**: Rigidbody-based movement, CapsuleCast для wall sliding
- **Анимации**: Dual system — FPP (руки от первого лица) + TPP (тело третьего лица)
- **IK**: Animation Rigging (TwoBoneIKConstraint) для хвата оружия

## Известные ограничения

- `AllyChase` не имеет обхода препятствий (в отличие от `EnemyChase`)
- `PlayerAim.cs` не используется системой стрельбы (legacy, disconnected)
- Строки UI захардкожены на русском (ПОБЕДА/ПОРАЖЕНИЕ)
- Только single-player (нет сетевого кода)
