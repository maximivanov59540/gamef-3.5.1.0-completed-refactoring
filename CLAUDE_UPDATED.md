# CLAUDE.md - AI Assistant Guide for City-Building Game Project

**Last Updated:** 2025-11-18
**Project Version:** gamef-3.5.1.0-completed-refactoring
**Project Type:** Unity City-Building/Economy Simulation Game
**Primary Language:** C# (Unity)
**Code Comments Language:** Russian (Русский)
**Refactoring Status:** ✅ COMPLETED

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Technology Stack](#technology-stack)
3. [Directory Structure](#directory-structure)
4. [Architecture & Design Patterns](#architecture--design-patterns)
5. [Core Systems](#core-systems)
6. [Coding Conventions](#coding-conventions)
7. [Development Workflow](#development-workflow)
8. [Common Tasks](#common-tasks)
9. [Important Notes for AI Assistants](#important-notes-for-ai-assistants)
10. [Key Files Reference](#key-files-reference)

---

## Project Overview

This is a **Unity-based city-building simulation game** featuring deep economic systems, logistics management, and modular building mechanics. The game includes:

- **Grid-based building system** (500x500 cells)
- **Resource production and consumption chains**
- **Warehouse logistics with cart-based delivery**
- **Modular buildings** (farms with fields, monasteries with zones)
- **Road network system** with pathfinding
- **Aura/influence system** for service buildings
- **Tax and money management**
- **Blueprint mode** for planning without resources

**Target Audience:** City-building/strategy game players
**Development Team:** Russian-speaking (all comments and debug logs in Russian)

**Refactoring Achievement:**
- ✅ God Classes decomposed into specialized components
- ✅ Service Locator pattern implemented
- ✅ Singleton count reduced from 22 to 7
- ✅ Performance issues resolved (O(n²) → O(1) or O(n))
- ✅ Total code lines reduced from ~16,340 to ~12,727 (-22%)

---

## Technology Stack

### Core Technologies
- **Unity Game Engine** (2020+)
- **C# (.NET/Mono)**
- **TextMeshPro** (UI text rendering)
- **Unity Event System**

### Key Unity Features Used
- ScriptableObjects (data-driven design)
- Component-based architecture
- Coroutines (production cycles, cart AI)
- Layer system (Ghost layer for previews)
- Physics raycasting (building placement)
- Material swapping (visual states)

### Custom Systems
- Grid management (500x500 array)
- BFS pathfinding algorithm
- State machine pattern (input modes, cart AI)
- **Service Locator** (dependency injection)
- Event-driven UI updates

---

## Directory Structure

```
/home/user/gamef-3.5.1.0-completed-refactoring/
│
├── Infrastructure/            # Core game services (NEW!)
│   ├── ServiceLocator.cs      (61 lines)   - Dependency injection
│   ├── GameBootstrapper.cs    (54 lines)   - Service registration
│   ├── TimeManager.cs         - Game time management
│   ├── CameraController.cs    - Camera movement & zoom
│   │
│   └── Interfaces/            # Service interfaces
│       ├── IGameService.cs    - Base service interface
│       ├── IResourceManager.cs
│       ├── IRoadManager.cs
│       ├── IMoneyManager.cs
│       ├── IEventManager.cs
│       ├── IAuraManager.cs
│       ├── IResourceCoordinator.cs
│       └── INotificationManager.cs
│
├── Construction/              # Building & construction systems
│   ├── Core/                  # Core building mechanics
│   │   ├── BuildingManager.cs           (320 lines)  - Facade for building ops (was 1306!)
│   │   ├── BuildingData.cs              (59 lines)   - ScriptableObject for config
│   │   ├── BuildingIdentity.cs          (42 lines)   - Component for metadata
│   │   ├── SelectionManager.cs          (269 lines)  - Selection & visual feedback
│   │   ├── BuildingVisuals.cs           (96 lines)   - Material state management
│   │   ├── GhostBuildingCollider.cs     (26 lines)   - Collision detection
│   │   ├── GridSystem.cs                (365 lines)  - Grid world management
│   │   ├── GridCellData.cs              (35 lines)   - Grid cell data structure
│   │   ├── GridVisualizer.cs            (88 lines)   - Grid visualization
│   │   ├── BuildOrchestrator.cs         (109 lines)  - Construction orchestration
│   │   ├── BuildSlot.cs                 (21 lines)   - Individual build slot
│   │   │
│   │   ├── Logic/             # Decomposed building logic (NEW!)
│   │   │   ├── BuildingPlacer.cs        (70 lines)   - Placement logic
│   │   │   ├── BuildingRemover.cs       (79 lines)   - Deletion & refund
│   │   │   ├── BuildingTransformer.cs   (121 lines)  - Move, rotate, copy, upgrade
│   │   │   └── BuildingValidator.cs     (61 lines)   - Validation logic
│   │   │
│   │   ├── Router/            # Resource routing logic (NEW!)
│   │   │   ├── BuildingResourceRouting.cs (103 lines) - Facade (was 1375!)
│   │   │   ├── RoutingResolver.cs       (84 lines)   - Route resolution
│   │   │   └── ConsumerSelector.cs      (78 lines)   - Consumer selection
│   │   │
│   │   └── Interfaces/        # Building interfaces
│   │       ├── IBuildingIdentifiable.cs
│   │       └── IBuildingRouting.cs
│   │
│   ├── Input/                 # Player input handling
│   │   ├── PlayerInputController.cs     (174 lines)  - State machine coordinator
│   │   ├── IInputState.cs               - State pattern interface
│   │   └── States/            # 13 input state implementations
│   │       ├── State_None.cs
│   │       ├── State_Building.cs
│   │       ├── State_BuildingUpgrade.cs
│   │       ├── State_Moving.cs
│   │       ├── State_Deleting.cs
│   │       ├── State_Upgrading.cs
│   │       ├── State_Copying.cs
│   │       ├── State_Selecting.cs
│   │       ├── State_GroupCopying.cs
│   │       ├── State_GroupMoving.cs
│   │       ├── State_RoadBuilding.cs
│   │       ├── State_RoadOperation.cs
│   │       └── State_PlacingModule.cs
│   │
│   ├── UI/                    # Construction UI
│   │   ├── BuildUIManager.cs            - Building menu & button handling
│   │   └── PlacementValidation.cs       - Visual feedback
│   │
│   ├── GroupOps/              # Mass operations
│   │   └── GroupOperationHandler.cs     (620 lines)  - Batch copy/move/delete
│   │
│   ├── Modular Buildings/     # Farm modules & zoned areas
│   │   ├── ModularBuilding.cs           - Main building with slots
│   │   ├── BuildingModule.cs            - Module component (fields, pastures)
│   │   └── ZonedArea.cs                 - Monastery/temple zones
│   │
│   └── Roads/                 # Road network system
│       ├── RoadManager.cs               (234 lines)  - Road + Logistics (merged!)
│       ├── RoadData.cs                  - ScriptableObject for road types
│       ├── RoadTile.cs                  - Individual road component
│       ├── RoadBuildHandler.cs          - Road placement logic
│       ├── RoadOperationHandler.cs      - Road deletion/upgrade
│       ├── RoadPathfinder.cs            (291 lines)  - General road pathfinding
│       │
│       └── Logic/             # Road logic components (NEW!)
│           └── CoverageCalculator.cs    - Coverage calculation logic
│
├── Economy/                   # Economic simulation systems
│   ├── Core/                  # Core economy types
│   │   ├── ResourceType.cs              - Enum: Wood, Stone, Planks, etc.
│   │   ├── ResourceCost.cs              - Serializable cost structure
│   │   ├── StorageData.cs               - Storage info (amount, capacity)
│   │   ├── EconomyDataTypes.cs          - Data type definitions
│   │   └── PopulationData.cs            - Population data structure
│   │
│   ├── Systems/               # Manager systems
│   │   ├── ResourceManager.cs           (167 lines)  - Global storage + Population
│   │   └── ResourceProducer.cs          (454 lines)  - Production cycles
│   │
│   ├── Storage/               # Resource storage & logistics
│   │   ├── IResourceProvider.cs         - Interface for resource sources
│   │   ├── IResourceReceiver.cs         - Interface for resource consumers
│   │   ├── BuildingOutputInventory.cs   - Building output storage
│   │   ├── BuildingInputInventory.cs    (272 lines)  - Building input storage (optimized!)
│   │   ├── ResourceRequest.cs           - Request data structure
│   │   ├── ResourceCoordinator.cs       (423 lines)  - Resource distribution
│   │   └── InterfaceTest.cs             - Testing utilities
│   │
│   ├── Warehouse/             # Warehouse & cart logistics
│   │   ├── Warehouse.cs                 - Warehouse building logic
│   │   ├── CentralWarehouse.cs          - Central warehouse coordinator
│   │   ├── CartAgent.cs                 (145 lines)  - Cart AI state machine (was 1262!)
│   │   ├── CartMovement.cs              (117 lines)  - Movement logic (NEW!)
│   │   ├── CartInventory.cs             (69 lines)   - Inventory management (NEW!)
│   │   └── CartPathfinder.cs            (59 lines)   - Pathfinding logic (NEW!)
│   │
│   ├── Money/                 # Currency management
│   │   └── MoneyManager.cs              - Gold/currency singleton
│   │
│   ├── Taxation/              # Tax & happiness systems
│   │   ├── Need.cs                      - Need data structure
│   │   ├── NeedCategory.cs              - Need categories enum
│   │   └── Residence.cs                 (468 lines)  - Residential buildings
│   │
│   ├── Event/                 # Event system (disasters, challenges)
│   │   ├── EventManager.cs              (547 lines)  - Events + Happiness (merged!)
│   │   ├── EventAffected.cs             - Component for affected buildings
│   │   ├── BuildingEvent.cs             - Building-specific event data
│   │   └── EventType.cs                 - Event types enum (Pandemic, Riot)
│   │
│   ├── Aura/                  # Building influence/coverage
│   │   ├── AuraManager.cs               - Global aura coordinator
│   │   ├── AuraEmitter.cs               - Building aura component
│   │   ├── AuraType.cs                  - Aura types enum
│   │   ├── AuraDistributionType.cs      - Distribution type enum
│   │   ├── AuraOnClick.cs               - Click handler
│   │   ├── SelectionAuraBridge.cs       - Selection integration
│   │   └── RadiusVisualizer.cs          - Visual feedback
│   │
│   └── UI/                    # Economy UI
│       ├── UIResourceDisplay.cs         - Resource count display
│       ├── BuildingStatusVisualizer.cs  - Status visualization
│       ├── CartPathVisualizer.cs        - Cart path visualization
│       └── UI_ResourceBalancePanel.cs   - Resource balance panel
│
└── UI/                        # General UI systems
    └── NotificationManager.cs           - In-game notifications
```

**File Count:** 108 C# scripts
**Total Lines:** ~12,727 (reduced from ~16,340)

---

## Architecture & Design Patterns

### 1. **Service Locator Pattern** (NEW!)

Replaces direct Singleton dependencies with interface-based injection:

```csharp
// OLD WAY (tight coupling):
ResourceManager.Instance.AddResources(ResourceType.Wood, 10);

// NEW WAY (loose coupling):
var resourceManager = ServiceLocator.Get<IResourceManager>();
resourceManager.AddToStorage(ResourceType.Wood, 10);
```

**Registered Services:**
- `IResourceManager` → ResourceManager
- `IRoadManager` → RoadManager
- `IMoneyManager` → MoneyManager
- `IEventManager` → EventManager
- `IAuraManager` → AuraManager
- `IResourceCoordinator` → ResourceCoordinator
- `INotificationManager` → NotificationManager

**Benefits:**
- Testable (can inject mocks)
- Flexible (can swap implementations)
- No static dependencies

---

### 2. **Facade Pattern** (NEW!)

God Classes refactored into Facades that delegate to specialized components:

**BuildingManager Example:**
```csharp
public class BuildingManager : MonoBehaviour
{
    private BuildingValidator _validator;    // Validation logic
    private BuildingPlacer _placer;          // Placement logic
    private BuildingRemover _remover;        // Deletion logic
    private BuildingTransformer _transformer; // Transform logic

    public void EnterBuildMode(BuildingData data)
    {
        // Facade delegates to specialized component
        _placer.CreateGhost(data);
    }

    public void ConfirmBuilding()
    {
        // Validates and places
        if (_validator.CanPlace(...))
            _placer.ConfirmPlacement();
    }
}
```

---

### 3. **Singleton Pattern** (REDUCED!)

**Before:** 22 Singletons
**After:** 7 Singletons

**Remaining Singletons:**
- `ResourceManager.Instance` (also implements IResourceManager)
- `MoneyManager.Instance`
- `PlayerInputController.Instance`
- `AuraManager.Instance`
- `EventManager.Instance` (merged with HappinessManager)
- `RoadManager.Instance` (merged with LogisticsManager)
- `TimeManager.Instance`

**Removed/Merged:**
- PopulationManager → integrated into ResourceManager.Population
- WorkforceManager → integrated into PopulationData
- HappinessManager → merged into EventManager
- LogisticsManager → merged into RoadManager
- BuildingRegistry → uses HashSet instead of Singleton pattern

---

### 4. **State Pattern**

Input system uses clean state machine:
```csharp
public interface IInputState
{
    void OnEnter();
    void OnUpdate();
    void OnExit();
}
```

**13 Input Modes:**
1. `None` - Idle/camera control
2. `Building` - Place buildings
3. `BuildingUpgrade` - Upgrade building type/tier
4. `Moving` - Relocate buildings
5. `Deleting` - Remove buildings
6. `Upgrading` - Convert blueprints
7. `Copying` - Duplicate buildings
8. `Selecting` - Multi-select
9. `GroupCopying` - Batch copy
10. `GroupMoving` - Batch move
11. `RoadBuilding` - Build roads
12. `RoadOperation` - Delete/upgrade roads
13. `PlacingModule` - Add farm modules

---

### 5. **Component-Based Architecture**

Unity's ECS approach with clean separation:
```csharp
// Building components
BuildingIdentity       // Metadata
+ ResourceProducer     // Production logic
+ AuraEmitter          // Influence area
+ BuildingVisuals      // Visual state
+ BuildingInputInventory   // Input storage
+ BuildingOutputInventory  // Output storage
```

---

### 6. **Observer Pattern**

Event-driven UI updates:
```csharp
public event System.Action<ResourceType> OnResourceChanged;
public event System.Action<bool> OnDebtStatusChanged;
public event System.Action<Vector2Int> OnRoadAdded;
```

---

### 7. **Strategy Pattern**
- `AuraDistributionType` enum (Radial vs RoadBased)
- `IResourceProvider` / `IResourceReceiver` interfaces

---

### 8. **Data-Driven Design**
ScriptableObjects for configuration:
- `BuildingData` - Building properties
- `RoadData` - Road types
- `ResourceProductionData` - Production recipes

---

## Core Systems

### 1. Service Locator System (`ServiceLocator.cs`)

**Purpose:** Centralized dependency injection without tight coupling.

**Registration (GameBootstrapper.cs):**
```csharp
void Awake()
{
    ServiceLocator.Clear();

    ServiceLocator.Register<IResourceManager>(_resourceManager);
    ServiceLocator.Register<IRoadManager>(_roadManager);
    ServiceLocator.Register<IMoneyManager>(_moneyManager);
    // ... register all services
}
```

**Usage:**
```csharp
// Get service
var resourceManager = ServiceLocator.Get<IResourceManager>();
resourceManager.AddToStorage(ResourceType.Wood, 50);
```

**Benefits:**
- Decouples systems
- Enables unit testing
- No static dependencies

---

### 2. Grid System (`GridSystem.cs`)

**Purpose:** Manages 500x500 grid world with building placement tracking.

**Key Features:**
- Multi-layer data (buildings, roads, modules, zones)
- O(1) cell lookup via 2D arrays
- Rotation support (0°, 90°, 180°, 270°)
- Collision detection

**Critical Methods:**
```csharp
bool CanPlaceBuilding(Vector2Int gridPos, Vector2Int size)
void PlaceBuilding(Vector2Int gridPos, GameObject building, Vector2Int size)
void RemoveBuilding(Vector2Int gridPos, Vector2Int size)
GameObject GetBuildingAt(Vector2Int gridPos)
```

---

### 3. Building System (Decomposed!)

**Old:** BuildingManager.cs (1306 lines, 25 public methods)
**New:** Facade pattern with specialized components

**Architecture:**
```
BuildingManager (Facade, 320 lines)
├── BuildingValidator (61 lines)
│   └── Validates placement, resources, grid
├── BuildingPlacer (70 lines)
│   └── Handles placement, ghost preview
├── BuildingRemover (79 lines)
│   └── Deletion, 50% refund calculation
└── BuildingTransformer (121 lines)
    └── Move, rotate, copy, upgrade operations
```

**Key Operations:**
- **Placement** - `EnterBuildMode()`, ghost building preview
- **Deletion** - 50% resource refund
- **Movement** - Relocate existing buildings
- **Rotation** - 90° increments with size swapping
- **Blueprint Mode** - Plan without consuming resources

---

### 4. Resource System (`ResourceManager.cs`)

**Resource Types:** `Wood`, `Stone`, `Planks` (extensible enum)

**Storage Model:**
```csharp
public Dictionary<ResourceType, StorageData> GlobalStorage;
public class StorageData
{
    public float currentAmount;
    public float capacity;
}
```

**New Features:**
- Implements `IResourceManager` interface
- Contains `PopulationData` subsystem (merged from PopulationManager)
- Event-driven updates

**Key Operations:**
```csharp
float AddToStorage(ResourceType type, float amount)
float TakeFromStorage(ResourceType type, float amount)
bool CanAfford(List<ResourceCost> costs)
```

---

### 5. Production System (`ResourceProducer.cs`)

**Purpose:** Handles building production cycles.

**Production Cycle:**
```
1. Check workforce available
2. Check input resources in building inventory
3. Calculate efficiency (workforce × ramp-up × module bonus)
4. Accumulate progress over time
5. When cycle completes → consume inputs, produce outputs
6. Request warehouse to pick up outputs
```

**Efficiency Modifiers:**
- **Workforce** - Population must be available
- **Ramp-up/Ramp-down** - Smooth start/stop
- **Module Bonus** - Farm fields boost by 20% each

---

### 6. Logistics System (Decomposed!)

**Old:** CartAgent.cs (1262 lines, complex state machine + movement + inventory)
**New:** Separated concerns

**Architecture:**
```
CartAgent (State Machine, 145 lines)
├── CartMovement (117 lines)
│   └── Pathfinding, movement, position updates
├── CartInventory (69 lines)
│   └── Cargo slots, loading, unloading
└── CartPathfinder (59 lines)
    └── BFS pathfinding on road network
```

**Cart State Machine:**
```
1. Idle (at warehouse)
2. LoadingOutput (from producer)
3. DeliveringOutput (to warehouse)
4. UnloadingOutput (at warehouse)
5. LoadingInput (from warehouse)
6. ReturningWithInput (to receiver)
```

---

### 7. Road System (`RoadManager.cs`)

**Purpose:** Road network + logistics coordination (merged!)

**Merged Systems:**
- Road graph management (was RoadManager)
- Logistics requests (was LogisticsManager)

**Features:**
- ✅ Tile-based roads with HashSet graph (O(1) instead of O(n))
- Different road types with speed multipliers
- Upgrade system (sand → stone)
- BFS pathfinding

**Performance Fix:**
```csharp
// OLD: List (O(n) Contains)
private Dictionary<Vector2Int, List<Vector2Int>> _roadGraph;

// NEW: HashSet (O(1) Contains)
private Dictionary<Vector2Int, HashSet<Vector2Int>> _roadGraph;
```

---

### 8. Resource Routing System (Decomposed!)

**Old:** BuildingResourceRouting.cs (1375 lines, 6+ responsibilities)
**New:** Facade with specialized components

**Architecture:**
```
BuildingResourceRouting (Facade, 103 lines)
├── RoutingResolver (84 lines)
│   └── Finds optimal input/output routes
└── ConsumerSelector (78 lines)
    └── Round-robin distribution logic
```

---

### 9. Event System (`EventManager.cs`)

**Purpose:** Manages random events + happiness (merged!)

**Merged Systems:**
- Event system (pandemics, riots)
- Happiness tracking (was HappinessManager)

**Event Types:**
1. **Pandemic** - Disease outbreak affecting residences
2. **Riot** - Unrest affecting production

**Performance Optimization:**
```csharp
// Statistics updated once per second, not every frame
_statsUpdateTimer += Time.deltaTime;
if (_statsUpdateTimer >= STATS_UPDATE_INTERVAL)
{
    UpdateStatistics();
    _statsUpdateTimer = 0f;
}
```

---

## Coding Conventions

### Naming Conventions

```csharp
// Private fields - underscore prefix + camelCase
private IResourceManager _resourceManager;
private GameObject _ghostBuilding;

// Public fields - camelCase or PascalCase
public BuildingData buildingData;
public float ProductionSpeed;

// Methods - PascalCase
public void EnterBuildMode(BuildingData data) { }

// Constants - SCREAMING_SNAKE_CASE
private const int MAX_GRID_SIZE = 500;

// Properties - PascalCase
public static ResourceManager Instance { get; private set; }
```

### Unity Attributes

```csharp
[Header("Ссылки на компоненты")]  // Section headers in Inspector
[SerializeField] private GridSystem _gridSystem;  // Private but Inspector-editable
[Tooltip("Начальный лимит для всех ресурсов")]  // Designer documentation
[RequireComponent(typeof(BuildingIdentity))]  // Enforce dependencies
```

### Comments & Documentation

**Language:** Russian (Русский)

```csharp
// --- Ссылки на другие системы ---  (Section dividers)
/// <summary>Хелпер для State_Building</summary>  (XML docs)
// 🚀 PERFORMANCE FIX: Dictionary для O(1) lookup
```

---

## Development Workflow

### Service Registration

1. **Create Service Interface** in Infrastructure/
   ```csharp
   public interface IMyService : IGameService
   {
       void DoSomething();
   }
   ```

2. **Implement Interface**
   ```csharp
   public class MyService : MonoBehaviour, IMyService
   {
       public void DoSomething() { }
   }
   ```

3. **Register in GameBootstrapper**
   ```csharp
   [SerializeField] private MyService _myService;

   void Awake()
   {
       ServiceLocator.Register<IMyService>(_myService);
   }
   ```

4. **Use Service**
   ```csharp
   var service = ServiceLocator.Get<IMyService>();
   service.DoSomething();
   ```

---

## Common Tasks

### Task 1: Add New Building Type

1. **Create BuildingData ScriptableObject**
2. **Create Building Prefab** with components
3. **Reference in BuildingData**
4. **Add to UI Menu**

### Task 2: Add New Resource Type

1. **Update ResourceType.cs enum**
2. **ResourceManager auto-initializes** all enum values
3. **Add UI Display** in UIResourceDisplay

### Task 3: Modify Production Recipe

**Edit ResourceProductionData ScriptableObject** in Inspector

---

## Important Notes for AI Assistants

### 1. **Use Service Locator, Not Singletons**

```csharp
// ❌ OLD (tight coupling):
ResourceManager.Instance.AddResources(ResourceType.Wood, 10);

// ✅ NEW (loose coupling):
var resourceManager = ServiceLocator.Get<IResourceManager>();
resourceManager.AddToStorage(ResourceType.Wood, 10);
```

### 2. **Decomposed Components**

When working with buildings:
- `BuildingManager` is now a **facade**
- Logic is in `BuildingValidator`, `BuildingPlacer`, etc.
- Don't add 500-line methods to facades!

### 3. **Performance Patterns**

```csharp
// ✅ GOOD: HashSet for membership checks
private HashSet<Vector2Int> _roads = new HashSet<Vector2Int>();
if (_roads.Contains(pos)) { }

// ❌ BAD: List for membership checks
private List<Vector2Int> _roads = new List<Vector2Int>();
if (_roads.Contains(pos)) { } // O(n)!

// ✅ GOOD: Dictionary for lookups
private Dictionary<ResourceType, StorageData> _lookup;
var data = _lookup[type]; // O(1)

// ❌ BAD: List.FirstOrDefault for lookups
var data = _list.FirstOrDefault(x => x.type == type); // O(n)
```

### 4. **Merged Systems**

- **EventManager** = Events + Happiness
- **RoadManager** = Roads + Logistics
- **ResourceManager** = Resources + Population

Don't try to access `HappinessManager` or `LogisticsManager` - they no longer exist!

---

## Key Files Reference

### Must-Read Files (New Architecture!)

1. **Infrastructure/ServiceLocator.cs** (61 lines)
   - Core dependency injection system

2. **Infrastructure/GameBootstrapper.cs** (54 lines)
   - Service registration

3. **Construction/Core/Logic/** (331 lines total)
   - BuildingPlacer, BuildingRemover, BuildingTransformer, BuildingValidator
   - Decomposed building operations

4. **Economy/Warehouse/Cart*.cs** (390 lines total)
   - CartAgent, CartMovement, CartInventory, CartPathfinder
   - Decomposed cart AI

5. **Construction/Core/Router/** (265 lines total)
   - RoutingResolver, ConsumerSelector
   - Decomposed resource routing

6. **Economy/Systems/ResourceManager.cs** (167 lines)
   - Implements IResourceManager
   - Contains PopulationData

7. **Construction/Roads/RoadManager.cs** (234 lines)
   - Implements IRoadManager
   - Merged road + logistics systems

8. **Economy/Event/EventManager.cs** (547 lines)
   - Implements IEventManager
   - Merged events + happiness

---

## Changelog

### 2025-11-18 - Version 2.0.0 - REFACTORING COMPLETED

**Major Changes:**
- ✅ Implemented Service Locator pattern
- ✅ Decomposed 5 God Classes into 15+ specialized components
- ✅ Reduced Singleton count from 22 to 7
- ✅ Fixed 7 critical O(n²) performance issues
- ✅ Merged redundant systems (EventManager+HappinessManager, RoadManager+LogisticsManager)
- ✅ Total code reduction: 16,340 → 12,727 lines (-22%)

**Performance Improvements:**
- RoadManager: List → HashSet (O(n) → O(1))
- BuildingInputInventory: List.FirstOrDefault → Dictionary (O(n) → O(1))
- EventManager: Statistics update throttled (every frame → once per second)
- BuildingRegistry: List → HashSet for registration

**Architecture Improvements:**
- BuildingManager: 1306 → 320 lines (facade)
- CartAgent: 1262 → 145 lines (facade)
- BuildingResourceRouting: 1375 → 103 lines (facade)
- New directories: Construction/Core/Logic/, Construction/Core/Router/, Construction/Roads/Logic/

**Merged Systems:**
- PopulationManager → ResourceManager.Population
- HappinessManager → EventManager
- LogisticsManager → RoadManager

### 2025-11-17 - Version 1.1.0 - Pre-Refactoring

- Initial comprehensive documentation
- God Classes identified
- Performance issues catalogued

---

**Last Updated:** 2025-11-18
**Version:** 2.0.0
**Maintained By:** AI Assistant (Claude)
**Refactoring Status:** ✅ COMPLETED
