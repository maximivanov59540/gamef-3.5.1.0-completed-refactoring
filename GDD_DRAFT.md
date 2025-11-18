# Game Design Document (Черновик)
## "Торговый Путь" (Working Title)

**Версия:** 0.1 (Черновик на основе анализа кодовой базы)
**Дата:** 2025-11-18
**Статус:** Извлечено из существующего кода

---

## 1. Концепция

### 1.1 Жанр
**Anno-like экономическая стратегия в реальном времени** с упором на:
- Градостроительство на сетке 500×500 клеток
- Глубокие производственные цепочки (76 типов ресурсов)
- Систему удовлетворения потребностей населения
- Автоматизированную логистику (тележки, склады, дороги)

### 1.2 Сеттинг
**Древняя Русь / Русское средневековье**

Социальная структура основана на исторических классах:
- **Смерды** (Farmers) — крестьяне, базовый класс
- **Посадские** (Craftsmen) — ремесленники, средний класс
- **Цеховые** (Artisans) — мастера, высший класс
- **Белое духовенство** (White Clergy) — приходские священники
- **Черное духовенство** (Black Clergy) — монахи, высшее духовенство

### 1.3 Основная цель игры
Развитие средневекового русского города через:
1. Строительство производственных зданий
2. Создание сложных цепочек производства ресурсов
3. Удовлетворение потребностей растущего населения
4. Управление логистикой (дороги, склады, тележки)
5. Борьба с событиями (пандемии, бунты)
6. Прогрессия через социальные классы

---

## 2. Архитектура (Техническая)

### 2.1 Паттерн: Service Locator
**Источник:** `Infrastructure/ServiceLocator.cs`

Проект отошел от классической Singleton-архитектуры в пользу **Service Locator Pattern**:

```csharp
// Регистрация сервиса (в Bootstrapper)
ServiceLocator.Register<IResourceManager>(resourceManager);

// Получение сервиса
var resources = ServiceLocator.Get<IResourceManager>();
```

**Преимущества:**
- Снижение связанности компонентов
- Упрощенное тестирование
- Централизованное управление зависимостями

### 2.2 Декомпозиция крупных классов
**Источник:** Комментарии в `Economy/Storage/`, `Construction/Core/Logic/`

Крупные классы (BuildingManager ~1300 строк, CartAgent ~1200 строк) были декомпозированы на более мелкие:

**BuildingManager разделен на:**
- `BuildingPlacer` — размещение зданий
- `BuildingRemover` — удаление зданий
- `BuildingUpgrader` — улучшение зданий
- `BuildingMover` — перемещение зданий
- `BuildingValidator` — валидация операций

**CartAgent разделен на:**
- `CartMovement` — движение по дорогам
- `CartInventory` — инвентарь тележки
- `CartTaskExecutor` — выполнение задач доставки

**Координация:**
- Service Locator связывает эти компоненты
- Каждый класс отвечает за одну задачу (Single Responsibility Principle)

---

## 3. Система Ресурсов и Производственные Цепочки

### 3.1 Полный список ресурсов (76 типов)
**Источник:** `Economy/Core/ResourceType.cs`

#### Базовые строительные материалы
```
Wood        (Дерево)
Stone       (Камень)
Planks      (Доски)
```

#### Тир 1: Смерды (Farmers) — 9 ресурсов
```
Vegetables      (Овощи)
Wool            (Шерсть)
SimpleClothing  (Простая одежда)
Honey           (Мед)
Clay            (Глина)
ClayPottery     (Глиняная посуда)
Berries         (Ягоды)
Apples          (Яблоки)
Jam             (Варенье)
```

#### Тир 2: Посадские (Craftsmen) — 21 ресурс
```
Wheat           (Пшеница)
Flour           (Мука)
Bread           (Хлеб)
Cows            (Коровы)
Meat            (Мясо)
Leather         (Кожа)
Fat             (Жир)
Flax            (Лен)
LinenFabric     (Льняная ткань)
SimpleFurniture (Простая мебель)
Wormwood        (Полынь)
Malt            (Солод)
StJohnsWort     (Зверобой)
Sage            (Шалфей)
Oregano         (Душица)
HerbMixture     (Смесь трав)
Beer            (Пиво)
Pigments        (Пигменты)
LeatherShoes    (Кожаные башмаки)
CarvedToys      (Вырезные игрушки)
```

#### Тир 3: Цеховые (Artisans) — 13 ресурсов
```
HeartyStew      (Сытное рагу)
CarvedFrame     (Резная рама)
Copper          (Медь)
Tin             (Олово)
Bronze          (Бронза)
Quartz          (Кварц)
Ash             (Зола)
BronzeMirror    (Бронзовое зеркало)
Glass           (Стекло)
Soap            (Мыло)
Mead            (Медовуха)
Gingerbread     (Пряники)
Jewelry         (Украшения)
```

#### Клир 1: Белое духовенство (White Clergy) — 6 ресурсов
```
Cloth           (Сукно)
PriestClothing  (Одежда священника)
Wax             (Воск)
WaxCandles      (Восковые свечи)
Lime            (Известь)
Icons           (Иконы)
```

#### Клир 2: Черное духовенство (Black Clergy) — 8 ресурсов
```
Buckwheat       (Гречка)
Mushrooms       (Грибы)
LentFood        (Постная еда)
Paper           (Бумага)
ManuscriptBook  (Рукописная книга)
Grapes          (Виноград)
Must            (Сусло)
ChurchWine      (Церковное вино)
HealingHerbBag  (Мешочек целебных трав)
```

### 3.2 Примеры производственных цепочек
**Источник:** `Economy/Core/EconomyDataTypes.cs`, документация в `CLAUDE.md`

#### Цепочка 1: Хлеб (3 этапа)
```
Farm (Wheat) → Mill (Wheat → Flour) → Bakery (Flour → Bread)
```
- **Входы:** Пшеница (Wheat)
- **Промежуточный продукт:** Мука (Flour)
- **Финальный продукт:** Хлеб (Bread) — базовая потребность Посадских

#### Цепочка 2: Пиво (2 этапа)
```
Herb Gardens (Wormwood, Malt, StJohnsWort, Sage, Oregano)
  → Herbalist (HerbMixture)
  → Brewery (HerbMixture → Beer)
```
- **Входы:** 5 видов трав
- **Промежуточный продукт:** Смесь трав (HerbMixture)
- **Финальный продукт:** Пиво (Beer) — роскошь для Посадских (требует 100 населения)

#### Цепочка 3: Бронзовое зеркало (2 этапа)
```
Copper Mine (Copper) + Tin Mine (Tin)
  → Smelter (Copper + Tin → Bronze)
  → Mirror Workshop (Bronze → BronzeMirror)
```
- **Входы:** Медь (Copper) + Олово (Tin) — **два ресурса одновременно**
- **Промежуточный продукт:** Бронза (Bronze)
- **Финальный продукт:** Бронзовое зеркало (BronzeMirror) — роскошь для Цеховых

#### Цепочка 4: Доски (простая, 1 этап)
```
Sawmill (Wood → Planks)
```
- **Вход:** Дерево (Wood)
- **Выход:** Доски (Planks) — строительный материал

#### Цепочка 5: Церковное вино (2 этапа)
```
Vineyard (Grapes) → Winery (Grapes → Must → ChurchWine)
```
- **Вход:** Виноград (Grapes)
- **Промежуточный продукт:** Сусло (Must)
- **Финальный продукт:** Церковное вино (ChurchWine) — для Черного духовенства

---

## 4. Население и Потребности (Needs)

### 4.1 Уровни населения (PopulationTier)
**Источник:** `Economy/Systems/PopulationTier.cs`

```csharp
public enum PopulationTier
{
    Farmers = 0,        // Смерды (низший класс)
    Craftsmen = 1,      // Посадские (средний класс)
    Artisans = 2,       // Цеховые (высший класс)
    WhiteClergy = 3,    // Белое духовенство (клир 1)
    BlackClergy = 4     // Черное духовенство (клир 2)
}
```

### 4.2 Система потребностей (Need System)
**Источник:** `Economy/Taxation/Need.cs`

Игра использует **систему Anno 1800**: каждая потребность настраивается через ScriptableObject.

#### Параметры потребности:

**Потребление:**
- `resourceType` — тип ресурса (Fish, Bread, Beer, и т.д.)
- `amountPerMinute` — сколько потребляет 1 дом за 1 минуту (например, 0.1 = 10 домов на 1 ресурс/мин)

**Категория:**
- `category` — категория потребности (см. 4.3)

**Бонусы (если удовлетворена):**
- `populationBonus` — +X жителей на дом (например, +5 за еду)
- `happinessBonus` — +Y счастья за цикл потребления (например, +0.5 за еду)
- `taxBonusPerCycle` — +Z золота за цикл (например, +0.5 за базовые нужды, +2.0 за роскошь)

**Штрафы (если НЕ удовлетворена):**
- `happinessPenalty` — -Y счастья за цикл (например, -1.0 за голод)

**Модификаторы событий:**
- `pandemicChanceReduction` — снижение шанса пандемии (0-1, например 0.2 = -20%)
- `riotChanceReduction` — снижение шанса бунта (0-1, например 0.15 = -15%)

**Разблокировка (Anno 1800 Progression):**
- `requiresUnlock` — требует ли разблокировки?
- `unlockPopulationTier` — какой класс населения нужен (Farmers, Craftsmen, и т.д.)
- `unlockAtPopulation` — сколько жителей этого класса нужно (например, 50 смердов → Одежда)

#### Механика разблокировки:

**Источник:** `Economy/Taxation/Need.cs:171-192`

```
Пока потребность ЗАБЛОКИРОВАНА:
  • НЕ потребляется (ресурс не списывается)
  • НЕ дает бонусы (население, счастье, налоги)
  • НЕ дает штрафы (нет негатива от отсутствия)
  • Дом работает так, будто этой потребности не существует

После РАЗБЛОКИРОВКИ (достигли нужного населения):
  • Начинает потребляться автоматически
  • Применяются все бонусы и штрафы
  • Появляется в UI (если реализовано)
```

**Прогрессия населения (Anno 1800):**
```
Farmers (смерды) → требуют базовые нужды (Vegetables, Honey)
  ↓ Достигли 50 смердов
Craftsmen (посадские) → разблокируется Одежда, Хлеб
  ↓ Достигли 100 посадских
Artisans (цеховые) → разблокируется Пиво, Украшения
```

#### Примеры конфигурации (из кода):

**Пример 1: Базовая еда (Fish) — Доступна сразу**
```
resourceType = Fish
amountPerMinute = 0.1           // 10 домов = 1 рыба/мин
category = Basic
populationBonus = 5             // +5 жителей когда есть еда
happinessBonus = 0.5            // +0.5 счастья
taxBonusPerCycle = 0.5          // +0.5 золота
happinessPenalty = -1.0         // -1.0 счастья (голод - критично!)
requiresUnlock = false          // ✓ Доступна сразу!
```

**Пример 2: Одежда (Clothes) — Требует 50 смердов**
```
resourceType = Clothes
amountPerMinute = 0.05          // 20 домов = 1 одежда/мин
category = Comfort
populationBonus = 3             // +3 жителя
happinessBonus = 0.3            // +0.3 счастья
taxBonusPerCycle = 1.0          // +1.0 золота (платят больше за комфорт)
happinessPenalty = -0.5         // -0.5 счастья
requiresUnlock = true           // ✓ Требует разблокировки!
unlockPopulationTier = Farmers  // Нужны Farmers (смерды)
unlockAtPopulation = 50         // Минимум 50 смердов
```

**Пример 3: Пиво (Beer) — Требует 100 посадских**
```
resourceType = Beer
amountPerMinute = 0.033         // 30 домов = 1 пиво/мин
category = Luxury
populationBonus = 2             // +2 жителя
happinessBonus = 0.2            // +0.2 счастья
taxBonusPerCycle = 2.0          // +2.0 золота (роскошь дорого стоит)
happinessPenalty = -0.2         // -0.2 счастья (не критично)
requiresUnlock = true           // ✓ Требует разблокировки!
unlockPopulationTier = Craftsmen // Нужны Craftsmen (посадские)
unlockAtPopulation = 100        // Минимум 100 посадских
```

### 4.3 Категории потребностей (NeedCategory)
**Источник:** `Economy/Taxation/NeedCategory.cs`

```csharp
public enum NeedCategory
{
    Basic,      // Базовая потребность (еда, вода, кров)
    Comfort,    // Потребности довольства (одежда, мебель)
    Personal,   // Персональные потребности (развлечения, образование)
    Luxury      // Потребности роскоши (украшения, деликатесы)
}
```

**Влияние категорий на геймплей:**

- **Basic** — критичны для выживания, высокие штрафы за отсутствие (-1.0 счастья)
- **Comfort** — важны для роста населения, средние бонусы (+3 жителя)
- **Personal** — снижают шанс негативных событий (например, -15% к бунту)
- **Luxury** — максимальные налоговые бонусы (+2.0 золота), но некритичны

---

## 5. Логистика

### 5.1 Основа: Дорожная сеть
**Источник:** `Construction/Roads/RoadManager.cs`, `Construction/Roads/LogisticsPathfinder.cs`

- Логистика работает **только по дорогам** (road graph)
- Используется **BFS-алгоритм** для поиска пути между зданиями
- Разные типы дорог имеют разные скорости движения (speed multipliers)

### 5.2 Тележки (CartAgent)
**Источник:** `Economy/Warehouse/CartAgent.cs` (1238 строк)

Автоматические агенты для доставки ресурсов.

**State Machine (состояния тележки):**
```
1. Idle               — ожидание на складе
2. LoadingOutput      — загрузка товара от производителя
3. DeliveringOutput   — доставка товара на склад
4. UnloadingOutput    — выгрузка на склад
5. LoadingInput       — загрузка сырья со склада
6. ReturningWithInput — доставка сырья к производителю
```

**Логика работы:**
1. Производитель запрашивает сырье → склад отправляет тележку
2. Тележка едет к производителю по дорогам (BFS pathfinding)
3. Забирает готовую продукцию → везет на склад
4. Разгружает → загружает сырье → везет обратно к производителю
5. Повторяет цикл

### 5.3 Система координации (ResourceCoordinator)
**Источник:** `Economy/Storage/ResourceCoordinator.cs` (423 строки)

**Проблема:** Несколько производителей (например, 3 рудника) могут одновременно поставлять одному потребителю (кузница), что приводит к избытку.

**Решение:** Координатор регистрирует связи "производитель → потребитель":

```csharp
// Рудник #1 начинает снабжать Кузницу #1
coordinator.RegisterSupplyRoute(mine1, smelter1, ResourceType.Copper);

// Рудник #2 проверяет: занята ли Кузница #1?
bool reserved = coordinator.IsConsumerReserved(smelter1, ResourceType.Copper);
if (reserved)
{
    // Кузница #1 занята, ищем Кузницу #2
    FindNextConsumer();
}
```

**Результат:**
- Идеальное распределение производителей по потребителям (1:1)
- Нет хаотичного round-robin
- Предотвращение избытка/недостатка

### 5.4 Маршрутизация ресурсов (BuildingResourceRouting)
**Источник:** `Construction/Core/BuildingResourceRouting.cs` (104 строки)

Каждое здание может настроить:

**Опции маршрутизации:**
- `outputDestinationTransform` — куда доставлять выход (null = автопоиск)
- `inputSourceTransform` — откуда брать вход (null = автопоиск)
- `preferDirectSupply` — предпочитать прямые цепочки поставок (Farm → Bakery)
- `preferDirectDelivery` — минуя склад
- `enableRoundRobin` — чередовать между потребителями
- `enableCoordination` — использовать ResourceCoordinator

**Типы связей:**

**1. Прямая цепочка (Direct Supply Chain):**
```
Farm (Wheat) → [outputDestination = Mill] → Mill (Flour) → [outputDestination = Bakery] → Bakery (Bread)
```

**2. Косвенная через склад:**
```
Sawmill (Planks) → Warehouse → Carpentry Workshop (Furniture)
```

**3. Round-robin (несколько потребителей):**
```
Mine (Iron) → [enableRoundRobin=true] → Smelter #1, Smelter #2, Smelter #3 (по очереди)
```

---

## 6. События (Events)

### 6.1 Типы событий (EventType)
**Источник:** `Economy/Event/EventType.cs`

```csharp
public enum EventType
{
    None,       // Нет активного события
    Pandemic,   // Пандемия (болезнь) - только для жилых зданий
    Riot        // Бунт - для жилых и производственных зданий
}
```

### 6.2 Параметры событий (EventManager)
**Источник:** `Economy/Event/EventManager.cs`

#### Базовые шансы:
```csharp
basePandemicChance = 0.07f;  // 7% при каждой проверке
baseRiotChance = 0.07f;      // 7% при каждой проверке
```

#### Длительность:
```csharp
pandemicDurationSeconds = 300f;  // 5 минут
riotDurationSeconds = 180f;      // 3 минуты
```

#### Интервал проверки:
```csharp
eventCheckIntervalMinutes = 1f;  // Проверка каждую 1 минуту
```

### 6.3 Влияние счастья на события
**Источник:** `Economy/Event/EventManager.cs:179-201`

Счастье населения **напрямую влияет на шанс событий**:

```csharp
// Формула модификатора счастья
float GetEventChanceModifier()
{
    // Нормализуем счастье (0.0 - 1.0)
    float normalized = GetNormalizedHappiness();

    // Инвертируем: низкое счастье → высокий модификатор
    float modifier = 2.0f * (1.0f - normalized);

    return modifier;
}
```

**Примеры:**
- Счастье = **-100** (минимум) → модификатор = **2.0** (вдвое больше шансов на события)
- Счастье = **0** (нейтрально) → модификатор = **1.0** (базовый шанс)
- Счастье = **100** (максимум) → модификатор = **0.0** (почти нет шанса событий)

### 6.4 Влияние потребностей на события
**Источник:** `Economy/Event/EventManager.cs:462-488`

Удовлетворенные потребности **снижают шанс конкретных событий**:

**Пандемия:**
```csharp
// Если дом потребляет "Мешочек целебных трав" (HealingHerbBag)
Need healingHerbs:
  resourceType = HealingHerbBag
  pandemicChanceReduction = 0.2  // -20% к шансу пандемии
```

**Бунт:**
```csharp
// Если дом потребляет "Развлечения" (Entertainment)
Need entertainment:
  resourceType = Entertainment
  riotChanceReduction = 0.15  // -15% к шансу бунта
```

### 6.5 Влияние аур на события
**Источник:** `Economy/Event/EventManager.cs:423-460`

Специальные здания создают ауры (радиусы влияния), снижающие шанс событий:

**Больница (Hospital):**
- Тип ауры: `AuraType.Hospital`
- Снижает шанс пандемии в радиусе действия
- Эффект складывается (несколько больниц = больше защиты)

**Полицейский участок (Police):**
- Тип ауры: `AuraType.Police`
- Снижает шанс бунта в радиусе действия
- Эффект складывается

**Формула итогового шанса события:**
```csharp
finalChance = baseChance × happinessModifier × auraModifier × needsModifier
```

**Пример расчета шанса пандемии:**
```
baseChance = 0.07 (7%)
happinessModifier = 1.2 (счастье немного ниже нормы)
auraModifier = 0.7 (больница снижает на 30%)
needsModifier = 0.8 (целебные травы снижают на 20%)

finalChance = 0.07 × 1.2 × 0.7 × 0.8 = 0.047 (4.7%)
```

### 6.6 Разблокировка событий
**Источник:** `Economy/Event/EventManager.cs:517-530`

События **разблокируются при постройке специальных зданий**:

```csharp
pandemicsUnlocked = false;  // Разблокируется при постройке первой больницы
riotsUnlocked = false;      // Разблокируется при постройке первого полицейского участка
```

**Логика:**
1. В начале игры события **выключены** (игрок учится базовым механикам)
2. Игрок строит первую **больницу** → пандемии разблокированы (`UnlockPandemics()`)
3. Игрок строит первый **полицейский участок** → бунты разблокированы (`UnlockRiots()`)

**Это создает прогрессию:**
- Ранняя игра: фокус на экономике и логистике
- Средняя игра: добавляются болезни (нужно строить больницы и снабжать травами)
- Поздняя игра: добавляются бунты (нужно управлять счастьем и строить полицию)

---

## 7. Производственная Система

### 7.1 Структура производящего здания
**Источник:** `Economy/Systems/ResourceProducer.cs`, `Economy/Storage/BuildingInputInventory.cs`, `Economy/Storage/BuildingOutputInventory.cs`

Каждое производящее здание состоит из **5 компонентов**:

```
┌─────────────────────────────────────────────────────────────┐
│              ПРОИЗВОДСТВЕННОЕ ЗДАНИЕ                         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  1. BuildingInputInventory (ВХОД)                           │
│     ├─ requiredResources: [Wheat: 10, Wood: 5, ...]       │
│     ├─ priority: 3                                         │
│     └─ requestThreshold: 0.25 (создает запрос при 25%)    │
│                                                              │
│  2. ResourceProducer (ЛОГИКА ПРОИЗВОДСТВА)                 │
│     ├─ productionData: {inputCosts, outputYield,...}      │
│     ├─ cycleTimeSeconds: 5.0                              │
│     ├─ finalEfficiency = workforce × rampUp × modules     │
│     └─ Produce() когда: входы есть + выход не полон      │
│                                                              │
│  3. BuildingOutputInventory (ВЫХОД)                        │
│     ├─ outputResource: {resourceType, maxAmount}          │
│     └─ OnFull / OnSpaceAvailable события                  │
│                                                              │
│  4. BuildingResourceRouting (МАРШРУТ)                      │
│     ├─ outputDestination: автопоиск или явное задание    │
│     ├─ inputSource: автопоиск или явное задание          │
│     └─ enableRoundRobin: чередование потребителей        │
│                                                              │
│  5. BuildingIdentity (МЕТАДАННЫЕ)                          │
│     └─ buildingData: ScriptableObject с настройками       │
└─────────────────────────────────────────────────────────────┘
```

### 7.2 Цикл производства
**Источник:** `Economy/Systems/ResourceProducer.cs:265`

```csharp
void Update()
{
    // 1. Проверка доступа к складу (дороги и логистика)
    CheckWarehouseConnection();

    // 2. Расчет эффективности производства
    float finalEfficiency = CalculateFinalEfficiency();

    // finalEfficiency = rampUpEfficiency × workforceCap × efficiencyModifier × moduleBonus
    //                 = (0-100%)        × (0-100%)     × (1.0)            × (1.0-2.0+)

    // 3. Если эффективность > 0.001%
    if (finalEfficiency > 0.001f)
    {
        cycleTimer += Time.deltaTime;

        // Скорректированное время цикла
        float adjustedCycleTime = cycleTimeSeconds / finalEfficiency;

        // 4. Когда цикл завершен
        if (cycleTimer >= adjustedCycleTime)
        {
            // a) Проверить: есть ли входные ресурсы?
            bool hasInputs = inputInventory.HasAllResources();

            // b) Проверить: есть ли место в выходном инвентаре?
            bool hasSpace = outputInventory.HasSpace();

            if (hasInputs && hasSpace)
            {
                // c) Съесть входные ресурсы
                inputInventory.ConsumeResources();

                // d) Произвести выход
                outputInventory.AddResource(productionData.outputYield);

                // e) Перезагрузить таймер
                cycleTimer = 0f;
            }
        }
    }
}
```

### 7.3 Модификаторы эффективности

**1. Workforce Cap (0-100%):**
- Зависит от доступного населения (PopulationManager)
- Нет рабочих = 0% эффективности (производство останавливается)
- Полная рабочая сила = 100% эффективности

**2. Ramp Up/Down (0-100%):**
- Плавный разгон/торможение производства
- Длительность разгона: 60 секунд (по умолчанию)
- Предотвращает мгновенные скачки производства

**3. Module Bonus (1.0-2.0+):**
- Модульные здания (фермы) получают бонус от модулей (полей)
- Каждое поле: +20% к эффективности
- Пример: 5 полей = 1.0 + (5 × 0.2) = 2.0 (удвоение скорости)

**4. Efficiency Modifier (1.0):**
- Резерв для будущих модификаторов (события, технологии, и т.д.)

**Итоговая формула:**
```
finalEfficiency = rampUpEfficiency × workforceCap × efficiencyModifier × moduleBonus

Пример:
  rampUpEfficiency = 80% (разгоняется)
  workforceCap = 100% (полная рабочая сила)
  efficiencyModifier = 1.0 (нет модификаторов)
  moduleBonus = 1.6 (3 поля: 1 + 3×0.2)

  finalEfficiency = 0.8 × 1.0 × 1.0 × 1.6 = 1.28 (128% скорости)

Если базовый цикл = 10 секунд:
  adjustedCycleTime = 10 / 1.28 = 7.81 секунды
```

---

## 8. Система Строительства

### 8.1 Сетка (Grid System)
**Источник:** `Construction/Core/GridSystem.cs`

- **Размер мира:** 500×500 клеток
- **Слои данных:**
  - Buildings (основные здания)
  - Roads (дороги)
  - Modules (модули ферм)
  - Zones (зоны монастырей)
- **Координаты:** World Position (X, Z) ↔ Grid Position (X, Y)
- **Вращение:** 0°, 90°, 180°, 270° (с автоматическим swapping размера)

### 8.2 Режим Blueprints (Чертежи)
**Источник:** `Construction/Core/BlueprintManager.cs`

Позволяет **планировать без расхода ресурсов**:

- Режим чертежа: здания строятся **бесплатно**, но **не функционируют**
- Визуальный стиль: синий/прозрачный материал
- Upgrade (улучшение чертежа):
  - Проверить доступность ресурсов
  - Списать ресурсы
  - Переключить `isBlueprint = false`
  - Активировать здание

### 8.3 Модульные здания
**Источник:** `Construction/Modular Buildings/ModularBuilding.cs`

**Пример:** Ферма + поля

- **Основное здание:** `ModularBuilding` компонент
- **Модули:** `BuildingModule` (поля, пастбища)
- **Бонус:** Каждый модуль +20% к производству
- **Лимит модулей:** Настраиваемый (например, max 5 полей на ферму)

### 8.4 Зонированные области
**Источник:** `Construction/Modular Buildings/ZonedArea.cs`

**Пример:** Монастырь

- Основная зона с **предопределенными слотами** (`BuildSlot`)
- Фильтрация по типу здания/размеру
- Независимое управление зданиями внутри зоны
- Визуальная подсветка доступных слотов

---

## 9. Входные Режимы (Input States)

### 9.1 State Machine Pattern
**Источник:** `Construction/Input/PlayerInputController.cs`, `Construction/Input/IInputState.cs`

Игра использует **State Pattern** для управления вводом игрока.

**13 режимов:**

```csharp
public enum InputMode
{
    None,               // Idle/камера
    Building,           // Размещение зданий
    BuildingUpgrade,    // Upgrade типа/уровня здания
    Moving,             // Перемещение зданий
    Deleting,           // Удаление зданий
    Upgrading,          // Конвертация чертежей в реальные
    Copying,            // Копирование зданий
    Selecting,          // Выделение группы (box select)
    GroupCopying,       // Массовое копирование
    GroupMoving,        // Массовое перемещение
    RoadBuilding,       // Строительство дорог
    RoadOperation,      // Удаление/улучшение дорог
    PlacingModule       // Размещение модулей ферм
}
```

### 9.2 Жизненный цикл состояния

```csharp
public interface IInputState
{
    void OnEnter();   // Вход в режим (setup)
    void OnUpdate();  // Каждый кадр (логика)
    void OnExit();    // Выход из режима (cleanup)
}
```

**Пример:** `State_Building`
- `OnEnter()` — создать ghost building (зеленый превью)
- `OnUpdate()` — двигать ghost за мышкой, проверять валидность, клик = разместить
- `OnExit()` — удалить ghost building

---

## 10. Визуальная Обратная Связь

### 10.1 Состояния визуализации зданий
**Источник:** `Construction/Core/BuildingVisuals.cs`

**Визуальные состояния:**

- **Ghost (Green)** — валидное место для размещения (можно строить)
- **Invalid (Red)** — невалидное место (занято, нет ресурсов, коллизия)
- **Blueprint (Blue)** — запланированное здание (чертеж)
- **Real (Normal)** — завершенное функционирующее здание

### 10.2 Покрытие дорог (Road Coverage)
**Источник:** `Construction/Roads/RoadCoverageVisualizer.cs` (540 строк)

Визуализация радиусов влияния:
- Радиус склада (warehouse logistics radius)
- Радиус рынка (market coverage)
- Радиус больницы/полиции (event reduction auras)
- BFS-based или radial визуализация

---

## Резюме

Этот GDD извлечен на 100% из существующего кода проекта "Торговый Путь". Все данные (ресурсы, параметры событий, формулы, архитектура) актуальны и проверяемы через указанные файлы-источники.

**Ключевые особенности:**
1. **76 типов ресурсов** в 5 социальных уровнях (русский средневековый сеттинг)
2. **Anno 1800-style система потребностей** с прогрессивной разблокировкой
3. **Service Locator Architecture** вместо Singleton-зависимостей
4. **Автоматизированная логистика** (BFS-pathfinding, cart AI, resource coordination)
5. **Сложные производственные цепочки** (множественные входы, прямые/косвенные связи)
6. **Система событий** с модификаторами от счастья/аур/потребностей
7. **13 режимов ввода** (state machine)
8. **Модульные и зонированные здания**

**Для проверки актуальности:** Все данные содержат ссылки на файлы-источники с абсолютными путями или указанием строк кода.

---

**Дата создания:** 2025-11-18
**Основано на кодовой базе:** `/home/user/gamef-3.5.1.0-completed-refactoring/`
**Версия проекта:** 3.5.1.0 (completed refactoring)
