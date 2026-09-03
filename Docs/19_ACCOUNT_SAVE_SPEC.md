# Project 77 — Account & Save Specification

## 1. Цель

Прогресс Project 77 со временем становится ценным активом игрока: остров, покупки, сезонные предметы, коллекции и многолетняя история аккаунта. Потеря save — критический reputational incident.

## 2. Account model

Baseline:

1. **Guest-first**: первый запуск не требует регистрации.
2. Игрок получает стабильный internal Player ID.
3. Позже предлагается optional account binding.
4. Binding не должен стирать локальный прогресс.
5. Account recovery проектируется до релиза, а не после первых потерь данных.

Provider конкретной авторизации не LOCKED; game logic не зависит от vendor SDK.

## 3. Save domains

Предлагаемая верхнеуровневая схема:

```text
SaveData
  schemaVersion
  player
  progression
  island
  inventory
  characters
  story
  collections
  expeditions
  events
  settings
  statistics
  timestamps
```

Purchases/entitlements и критическая premium economy не должны полагаться только на изменяемый локальный save.

## 4. Versioning & migrations

Каждый persistent save имеет `schemaVersion`.

Правило:

```text
v10 -> migration -> v11 -> migration -> v12
```

Запрещено:

- reset старого save при несовместимости;
- silently drop неизвестные purchased/rare items;
- делать миграцию без backup/validation path.

Для каждой migration:

- unit tests;
- representative old saves;
- corrupted/partial save cases;
- idempotency where practical;
- migration telemetry.

## 5. Local + cloud strategy

Core gameplay может использовать локальный cache для скорости и offline, но cloud является механизмом восстановления и cross-device continuity.

Cloud provider должен быть за interface, чтобы можно было заменить vendor.

Минимум:

- automatic cloud sync;
- last known good snapshot;
- server timestamp;
- conflict metadata;
- restore after reinstall;
- device-change restore;
- support-driven recovery path.

## 6. Conflict resolution

Нельзя автоматически выбирать просто «самый новый файл» для всех данных.

Категории:

- additive: часть коллекций/achievements можно merge;
- authoritative: entitlements/premium balance определяются серверной записью;
- progression: выбирается/мерджится по version + server timestamp + consistency rules;
- settings: local-most-recent допустим.

При неоднозначном конфликте важнее не потерять редкий прогресс, чем сохранить идеальную чистоту данных.

## 7. Purchased entitlements

Покупки обрабатываются idempotently.

Ключ транзакции не должен выдать reward дважды при повторной доставке webhook/client callback.

Необходимо поддерживать:

- purchase validation;
- acknowledgement where required;
- restore;
- refund/revocation;
- subscription active/grace/paused/expired states where provider supports them;
- support audit trail.

## 8. Backup & recovery

Перед destructive migration/merge:

- сохранять previous known-good snapshot;
- хранить достаточную audit information для поддержки;
- иметь recovery tooling по Player ID.

## 9. Save corruption

При повреждении:

1. не перезаписывать облачный good snapshot corrupted локальной копией;
2. попытаться восстановить последнюю валидную версию;
3. если восстановление неполное — сохранить entitlements и максимальный доказуемый progress;
4. записать diagnostic event;
5. предложить support code/Player ID.

## 10. Data deletion

Удаление аккаунта и данных должно учитывать store/legal requirements и не должно создавать ситуацию, где клиент локально «воскрешает» удалённый серверный аккаунт при следующем sync.
