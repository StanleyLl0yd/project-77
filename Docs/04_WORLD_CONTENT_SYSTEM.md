# Project 77 — World & Content System

## 1. Content architecture

Контент должен масштабироваться модульно. Основные единицы:

- Zone;
- Landmark;
- Building;
- Decoration;
- Character;
- Creature/Pet;
- Collectible;
- Artifact;
- Expedition;
- Puzzle pack;
- Story beat;
- Planet;
- Season;
- Event.

Все сущности должны иметь стабильные data IDs и быть конфигурируемыми без изменения основного кода.

## 2. Island design

Остров — не свободный city-builder sandbox с сотнями клеток. Предлагается гибрид:

- крупные landmarks стоят в определённых местах;
- игрок выбирает вариант восстановления/скин;
- выделенные декоративные площадки позволяют свободнее персонализировать пространство;
- трофейные зоны демонстрируют достижения.

Это снижает стоимость level-art, не ломая чувство «мой остров».

## 3. Restoration states

Каждый landmark может иметь 3–5 состояний:

1. Ruined.
2. Cleared.
3. Functional.
4. Upgraded.
5. Cosmetic variant / prestige.

Пример маяка:

- разрушен;
- очищен;
- лампа работает;
- усилена радиосистема;
- внешний стиль выбирает игрок.

## 4. Collection families

Коллекции должны давать долгосрочную дешёвую content runway.

Примеры:

- island wildlife;
- expedition badges;
- lost expedition items;
- alien alloys;
- star maps;
- plants of Viridia;
- Borealis crystals;
- ancient glyphs;
- robot parts;
- model ships;
- seasonal sets.

### Rarity

Использовать понятные уровни, но не превращать систему в casino rarity obsession.

- Common;
- Uncommon;
- Rare;
- Epic;
- Legendary.

Редкость — прежде всего коллекционная/визуальная ценность.

## 5. Trophy principle

Большие достижения должны оставлять физический след на острове.

Примеры:

- первая внеземная порода → экспонат возле лаборатории;
- завершение Borealis → ледяной монумент;
- community event → общий памятник;
- первый anniversary → уникальная башня/дерево;
- найденный редкий creature → живёт на острове.

Таким образом island = player history.

## 6. Planet package

Полноценная новая планета — дорогой контент. Поэтому выпускать её как пакет:

- 1 hero biome;
- 2–4 subzones;
- 1–2 landmark chains;
- 1 main mystery;
- 20–40 core levels/вариаций;
- 1 collection family;
- 2–4 creatures;
- 10–20 decorations;
- 1 trophy;
- reusable event hooks.

Числа ориентировочные и должны зависеть от реальной скорости производства.

## 7. Reusable content

Чтобы планеты не становились одноразовыми:

- daily expedition nodes;
- rotating resource hotspots;
- collection completion;
- seasonal revisit;
- community objectives;
- rare anomalies;
- photo/discovery tasks.

## 8. Cosmetic slots

Монетизируемые визуальные поверхности:

### Island

- building skins;
- bridges/roads;
- flora sets;
- lighting;
- weather;
- banners;
- dock/ship pad;
- ambient creatures.

### 77

- shell skin;
- eyes/display theme;
- accessory;
- trail/effect;
- idle animation;
- emote.

### Ship

- hull paint;
- engine trail;
- cockpit ornament;
- landing effect.

### Characters

- outfits;
- backpacks/tools;
- seasonal looks.

## 9. Content cadence tiers

### Daily

Config-driven tasks/anomalies.

### Weekly

Small event, challenge set, rotation.

### Seasonal

Themed progression + cosmetics + mini-story.

### Major update

New zone/planet/system.

## 10. Asset strategy

Нужен стилизованный art direction, позволяющий:

- использовать ограниченную геометрию/спрайты;
- делать множество вариаций через материалы/цвет/декаль;
- легко читать объекты на маленьком экране;
- поддерживать праздничные reskins;
- не требовать photoreal production.

Рекомендуемый визуальный вектор: тёплая stylized 2.5D/isometric база + более впечатляющие, но стилистически совместимые инопланетные биомы.
