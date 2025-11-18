# GAME_MECHANICS.md - Complete Feature & Mechanics Reference

**Last Updated:** 2025-11-18
**Project Type:** Unity City-Building/Economy Simulation Game
**Documentation Type:** Complete Implemented Features List
**Language:** English (code comments in Russian)

---

## Table of Contents

1. [Overview](#overview)
2. [Construction Systems (1-4)](#construction-systems)
3. [Economy & Resources (5-7)](#economy--resources)
4. [Infrastructure (8-9)](#infrastructure)
5. [Population & Workforce (10)](#population--workforce)
6. [Housing & Happiness (11-12)](#housing--happiness)
7. [Influence Systems (13)](#influence-systems)
8. [Modular Buildings (14-15)](#modular-buildings)
9. [Events System (16)](#events-system)
10. [User Interface (17-18)](#user-interface)
11. [Visual Feedback (19)](#visual-feedback)
12. [Data & Utilities (20-22)](#data--utilities)
13. [Statistics Summary](#statistics-summary)

---

## Overview

This document provides a **complete catalog of all implemented game mechanics** in the city-building simulation. Each system is broken down into sub-features with references to implementing files.

**Total Systems:** 22 major categories
**Total Features:** 200+ individual mechanics
**Code Coverage:** 90 C# scripts, 10,789+ lines of code

---

## Construction Systems

### 1. BUILDING PLACEMENT SYSTEM

#### 1.1 Placement & Construction
**Core Mechanics:**
- **Individual Building Placement** - Place buildings on grid with ghost visualization
- **Building Rotation** - Rotate 90° with automatic size swapping (3x2 → 2x3)
- **Collision Detection** - Check grid cells for conflicts before placement
- **Ghost Building Preview** - Green (valid) / Red (invalid) visual feedback
- **Resource Cost System** - Deduct resources before construction
- **Build Mode State** - `State_Building` input mode
- **Placement Validation** - Check grid space, resources, collisions, debt status

**Key Files:**
- `Construction/Core/BuildingManager.cs` (1306 lines)
- `Construction/Input/States/State_Building.cs`
- `Construction/Core/GhostBuildingCollider.cs`

**Code Example:**
```csharp
// Validation sequence
bool canPlace = gridSystem.CanPlaceBuilding(gridPos, size)
                && resourceManager.CanAfford(costs)
                && !moneyManager.IsInDebt();
```

---

#### 1.2 Building Movement & Relocation
**Core Mechanics:**
- **Individual Building Movement** - Pick up and relocate existing buildings
- **Rotation During Move** - Rotate buildings while moving
- **Move Validation** - Check destination validity
- **50% Resource Refund on Delete** - Returns half of build cost
- **Movement Mode** - `State_Moving` input state

**Key Files:**
- `Construction/Core/BuildingManager.cs`
- `Construction/Input/States/State_Moving.cs`

**Refund Formula:**
```csharp
foreach (var cost in buildingData.costs)
{
    float refund = cost.amount * 0.5f; // Always 50%
    ResourceManager.Instance.AddResources(cost.resourceType, refund);
}
```

---

#### 1.3 Building Deletion
**Core Mechanics:**
- **Individual Building Deletion** - Remove buildings with resource refund
- **Mass Deletion** - Delete multiple buildings in rectangle selection
- **Road-Only Deletion** - Separate road deletion from buildings
- **Confirmation Dialog** - Confirm mass deletions
- **Deletion Mode** - `State_Deleting` input state

**Key Files:**
- `Construction/Input/States/State_Deleting.cs`
- `Construction/Core/BuildingManager.cs`

---

#### 1.4 Building Selection & Information
**Core Mechanics:**
- **Building Selection** - Click to view information and controls
- **Building Info Panel** - Display name and properties
- **Rectangle Multi-Select** - Select multiple buildings via drag
- **Selection Highlighting** - Visual feedback for selected buildings
- **Selection Manager** - Central selection tracking system

**Key Files:**
- `Construction/Core/SelectionManager.cs` (269 lines)
- `UI/UIManager.cs` (314 lines)
- `Construction/Input/States/State_Selecting.cs`

---

#### 1.5 Blueprint Mode
**Core Mechanics:**
- **Blueprint Placement** - Place buildings without spending resources
- **Blueprint Visualization** - Special blue/transparent visual state
- **Blueprint → Real Upgrade** - Convert blueprint to real building later
- **Blueprint Toggle** - Enable/disable blueprint mode

**Key Files:**
- `Construction/Core/BlueprintManager.cs` (62 lines)
- `Construction/Core/BuildingManager.cs`

**Visual States:**
| State | Color | Purpose |
|-------|-------|---------|
| Ghost | Green | Valid placement preview |
| Invalid | Red | Cannot place here |
| Blueprint | Blue/Transparent | Planned building |
| Real | Normal | Completed building |

---

### 2. MASS BUILDING OPERATIONS

#### 2.1 Mass Building (Brush Tool)
**Core Mechanics:**
- **Rectangle Mass Placement** - Place multiple buildings in grid pattern
- **Mass Rotation** - Rotate entire mass placement
- **Mass Preview** - Visual grid showing where buildings will be
- **Mass Validation** - Check all cells before placement
- **Efficient Batch Processing** - Place many buildings in one operation

**Key Files:**
- `Construction/MassBuildHandler.cs`
- `Construction/Input/States/State_Building.cs`

---

#### 2.2 Mass Movement
**Core Mechanics:**
- **Group Building Movement** - Move multiple buildings together
- **Relative Position Preservation** - Keep relative positioning intact
- **Group Rotation** - Rotate entire group with pivot point
- **Group Move Validation** - Check all destinations
- **Group Move Mode** - `State_GroupMoving` for mass relocation

**Key Files:**
- `Construction/GroupOps/GroupOperationHandler.cs` (620 lines)
- `Construction/Input/States/State_GroupMoving.cs`

---

#### 2.3 Mass Copying
**Core Mechanics:**
- **Group Copy Operation** - Duplicate multiple buildings
- **Ghost Pool for Preview** - Pool of ghost buildings for visual feedback
- **Copy Offset Tracking** - Remember relative positions
- **Group Selection** - Select and copy building groups
- **Mass Copy Mode** - `State_GroupCopying` for mass duplication

**Key Files:**
- `Construction/GroupOps/GroupOperationHandler.cs`
- `Construction/Input/States/State_GroupCopying.cs`
- `Construction/Input/States/State_Copying.cs`

---

#### 2.4 Group Operation Manager
**Core Mechanics:**
- **Unified Group Operations** - Single system for batch operations
- **Rotation Vector Math** - Proper calculation of rotated positions
- **Building Pool Management** - Efficient reuse of ghost buildings

**Key Files:**
- `Construction/GroupOps/GroupOperationHandler.cs` (620 lines)

---

### 3. GRID SYSTEM & WORLD MANAGEMENT

#### 3.1 Grid Foundation
**Core Mechanics:**
- **500x500 Game World** - Large game world on fixed grid
- **Grid Cell Data** - Store occupancy and building info per cell
- **Coordinate System** - Convert between grid and world positions
- **Multi-Layer Tracking** - Track buildings, roads, modules, zones
- **Cell Size Configuration** - Configurable cell dimensions

**Key Files:**
- `Construction/Core/GridSystem.cs` (365 lines)
- `Construction/Core/GridCellData.cs` (35 lines)

**Coordinate Conversion:**
```csharp
// World → Grid
Vector2Int gridPos = new Vector2Int(
    Mathf.RoundToInt(worldPos.x),
    Mathf.RoundToInt(worldPos.z)  // Note: Z-axis!
);

// Grid → World
Vector3 worldPos = new Vector3(gridPos.x, 0, gridPos.y);
```

---

#### 3.2 Grid Visualization & Debug
**Core Mechanics:**
- **Grid Visualizer** - Display grid lines and cells
- **Debug Grid Display** - Toggle grid visibility
- **Visual Grid Rendering** - Show building footprints
- **Cell Highlighting** - Highlight specific cells for debugging

**Key Files:**
- `Construction/Core/GridVisualizer.cs` (88 lines)
- `Construction/Core/GridSystem.cs`

---

#### 3.3 Mouse Input & Raycasting
**Core Mechanics:**
- **Mouse Grid Position** - Track mouse position on grid
- **Mouse World Position** - Convert screen mouse to world position
- **Ground Raycast Detection** - Find ground height for building placement
- **UI Overlap Detection** - Prevent building on UI elements

**Key Files:**
- `Construction/Core/GridSystem.cs`

---

### 4. INPUT STATE MACHINE & CONTROLS

#### 4.1 Input State System
**Core Mechanics:**
- **State Pattern Implementation** - Clean state transitions
- **13 Input Modes** - Different interaction modes
- **State Lifecycle** - OnEnter, OnUpdate, OnExit callbacks
- **Mode Switching** - Clean transitions between states
- **Singleton Input Controller** - Central input management

**Key Files:**
- `Construction/Input/PlayerInputController.cs` (174 lines)
- `Construction/Input/IInputState.cs`

---

#### 4.2 Input States (Total: 13)

| State | Purpose | File | Hotkey |
|-------|---------|------|--------|
| **None** | Idle/camera control | `State_None.cs` | ESC |
| **Building** | Place individual buildings | `State_Building.cs` | B |
| **BuildingUpgrade** | Upgrade building type/tier | `State_BuildingUpgrade.cs` | U |
| **Moving** | Relocate buildings | `State_Moving.cs` | M |
| **Deleting** | Remove buildings | `State_Deleting.cs` | Delete |
| **Upgrading** | Convert blueprints to real | `State_Upgrading.cs` | G |
| **Copying** | Duplicate buildings | `State_Copying.cs` | C |
| **Selecting** | Rectangle selection | `State_Selecting.cs` | S |
| **GroupCopying** | Mass duplication | `State_GroupCopying.cs` | - |
| **GroupMoving** | Mass movement | `State_GroupMoving.cs` | - |
| **RoadBuilding** | Build roads | `State_RoadBuilding.cs` | R |
| **RoadOperation** | Delete/upgrade roads | `State_RoadOperation.cs` | - |
| **PlacingModule** | Add modules to buildings | `State_PlacingModule.cs` | - |

**State Transition Flow:**
```
None → Building → (ESC) → None
None → Selecting → GroupCopying → (Execute) → None
Building → (R key) → RoadBuilding → (ESC) → None
```

---

#### 4.3 Camera Controls
**Core Mechanics:**
- **WASD Movement** - Pan camera with keyboard
- **Mouse Wheel Zoom** - Zoom in/out with scroll wheel
- **Zoom Limits** - Minimum (15) and Maximum (100)
- **Smooth Movement** - Configurable movement speed

**Key Files:**
- `Infrastructure/CameraController.cs`

**Configuration:**
```csharp
[SerializeField] private float moveSpeed = 10f;
[SerializeField] private float minZoom = 15f;
[SerializeField] private float maxZoom = 100f;
```

---

## Economy & Resources

### 5. RESOURCE MANAGEMENT SYSTEM

#### 5.1 Global Resource Storage
**Core Mechanics:**
- **75+ Resource Types** - Wood, Stone, Planks, Bread, Cloth, etc.
- **Global Storage Pool** - Centralized resource inventory
- **Resource Capacity Limits** - Storage limits per resource type
- **Starting Resources** - Initial stockpile (100 Wood, 50 Stone)
- **Expandable Storage** - Increase limits through buildings
- **Resource Events** - `OnResourceChanged` for UI updates

**Key Files:**
- `Economy/Systems/ResourceManager.cs` (167 lines)

**Resource Categories:**
- **Basic Materials:** Wood, Stone, Clay, Iron Ore
- **Processed Goods:** Planks, Stone Blocks, Bricks, Iron Ingots
- **Food Items:** Bread, Meat, Fish, Beer, Wine
- **Clothing:** Simple Cloth, Fine Cloth, Leather
- **Luxury Goods:** Jewelry, Spices, Books
- **Religious Items:** Candles, Incense, Religious Texts

---

#### 5.2 Resource Tracking
**Core Mechanics:**
- **Current Amount Tracking** - Monitor stock levels
- **Maximum Capacity Tracking** - Storage limits per type
- **StorageData Structure** - Serializable resource storage data
- **Resource Cost Definition** - Define building costs

**Key Files:**
- `Economy/Core/StorageData.cs`
- `Economy/Core/ResourceCost.cs`
- `Economy/Core/ResourceType.cs`

**Data Structure:**
```csharp
public class StorageData
{
    public float currentAmount;
    public float capacity;
}

public class ResourceCost
{
    public ResourceType resourceType;
    public float amount;
}
```

---

#### 5.3 Building Input/Output Inventories
**Core Mechanics:**
- **Building Input Inventory** - Consume raw materials
- **Building Output Inventory** - Store finished products
- **Input Buffer System** - Temporary storage for incoming resources
- **Output Buffer System** - Temporary storage for outgoing resources
- **Inventory Capacity** - Storage limits per building
- **Inventory Events** - `OnFull`, `OnSpaceAvailable` signals

**Key Files:**
- `Economy/Storage/ResourceProvider.cs`
- `Economy/Storage/ResourceReceiver.cs`

---

#### 5.4 Resource Type System
**Core Mechanics:**
- **75+ Resource Types** - Extensive economy
- **Resource Tiers** - Different tiers (Farmers, Artisans, Masters, Clergy)
- **Basic Materials** - Foundation of Wood, Stone, Planks
- **Processed Goods** - Intermediate products
- **Luxury Items** - High-value goods
- **Religious Items** - Clergy-specific resources

**Key Files:**
- `Economy/Core/ResourceType.cs`

**Resource Tier Breakdown:**
| Tier | Population Level | Example Resources |
|------|-----------------|-------------------|
| **Tier 1** | Farmers | Wood, Stone, Bread, Fish |
| **Tier 2** | Artisans | Planks, Cloth, Pottery |
| **Tier 3** | Masters | Tools, Furniture, Fine Cloth |
| **Tier 4** | White Clergy | Candles, Religious Texts |
| **Tier 5** | Black Clergy | Incense, Relics, Sacred Items |

---

### 6. PRODUCTION SYSTEM

#### 6.1 Production Cycles
**Core Mechanics:**
- **Time-Based Production** - Configurable cycle times
- **Progress Accumulation** - Accumulate over time until completion
- **Input/Output Recipes** - Define what produces from what
- **Production State Machine** - Track working/paused/full states
- **Pause on Missing Input** - Auto-pause when materials unavailable
- **Pause on Full Output** - Auto-pause when storage full

**Key Files:**
- `Economy/Systems/ResourceProducer.cs` (454 lines)

**Production Cycle Flow:**
```
1. Check workforce available
2. Check input resources in building inventory
3. Calculate efficiency (workforce × ramp-up × module bonus)
4. Accumulate progress over time
5. When cycle completes → consume inputs, produce outputs
6. Request warehouse to pick up outputs
```

---

#### 6.2 Production Efficiency
**Core Mechanics:**
- **Workforce-Based Efficiency** - Efficiency depends on workers
- **Ramp-Up/Ramp-Down** - Smooth acceleration/deceleration (0-100%)
- **Module Bonus** - Farm fields give +20% bonus each
- **Efficiency Modifiers** - Stacking modifiers for final output
- **Efficiency Slider** - Manual control of efficiency (0-100%)
- **Efficiency Display** - Show current % in UI

**Key Files:**
- `Economy/Systems/ResourceProducer.cs`

**Efficiency Calculation:**
```csharp
float baseEfficiency = workforceRatio; // 0.0 to 1.0
float rampedEfficiency = baseEfficiency * rampUpProgress;
float moduleBonus = 1.0f + (moduleCount * 0.20f); // +20% per module
float finalEfficiency = rampedEfficiency * moduleBonus * manualSlider;
```

---

#### 6.3 Production Data
**Core Mechanics:**
- **ResourceProductionData** - ScriptableObject for recipes
- **Input Costs** - List of required resources
- **Production Output** - Produced resource per cycle
- **Cycle Time** - Duration between completions
- **Production Pause** - Ability to manually pause

**Key Files:**
- `Economy/Systems/ResourceProducer.cs`

**Example Production Recipe:**
```csharp
[CreateAssetMenu(fileName = "BakeryProduction", menuName = "Production/Recipe")]
public class BakeryProductionData : ScriptableObject
{
    public List<ResourceCost> inputResources; // 2 Grain
    public ResourceType outputResource;        // Bread
    public float outputAmount = 1f;
    public float productionCycleTime = 30f;    // 30 seconds
}
```

---

### 7. WAREHOUSE & LOGISTICS SYSTEM

#### 7.1 Warehouse Building
**Core Mechanics:**
- **Central Storage Hub** - Collect and distribute resources
- **Multi-Resource Storage** - Store all resource types
- **Global Pool Integration** - Access shared resource storage
- **Service Radius** - Coverage zone for carts
- **Cart Queue System** - Manage simultaneous unloading
- **Configurable Queue Size** - Maximum 1-5 carts

**Key Files:**
- `Economy/Warehouse/WarehouseManager.cs`
- `Economy/Warehouse/CentralWarehouse.cs`

---

#### 7.2 Cart Agent AI
**Core Mechanics:**
- **6-State State Machine** - Idle → LoadingOutput → DeliveringOutput → UnloadingOutput → LoadingInput → ReturningWithInput
- **Multi-Cargo Support** - 3 cargo slots per cart
- **Slot Capacity** - Maximum 5 units per cargo type
- **Automatic Movement** - Auto-follow road paths
- **Smart Loading** - Load multiple resource types
- **Smart Unloading** - Intelligently distribute goods
- **Loading/Unloading Logic** - Coordination at warehouses

**Key Files:**
- `Economy/Warehouse/CartAgent.cs` (1238 lines)

**Cart State Flow:**
```
┌─────────────────────────────────────────────┐
│                                             │
▼                                             │
Idle (at warehouse)                          │
│                                             │
▼                                             │
LoadingOutput (from producer)                │
│                                             │
▼                                             │
DeliveringOutput (to warehouse)              │
│                                             │
▼                                             │
UnloadingOutput (at warehouse) ──────────────┘
│
▼
LoadingInput (from warehouse)
│
▼
ReturningWithInput (to receiver)
│
▼
(back to Idle)
```

---

#### 7.3 Cart Movement & Pathfinding
**Core Mechanics:**
- **Road Pathfinding** - Navigate through road network
- **BFS Pathfinding** - Breadth-first search for shortest path
- **Movement Speed** - Configurable cart speed
- **Loading Time** - Configurable loading/unloading duration
- **Path Following** - Smooth movement along calculated path
- **Destination Routing** - Smart delivery to buildings

**Key Files:**
- `Construction/Roads/LogisticsPathfinder.cs` (302 lines)
- `Economy/Warehouse/CartAgent.cs`

---

#### 7.4 Logistics Coordination
**Core Mechanics:**
- **Request Board System** - Buildings post delivery requests
- **Request Fulfillment** - Carts service requests
- **Priority System** - Sort requests by urgency (1-5)
- **Request Thresholds** - Create request when < 25% full
- **Request Cancellation** - Clear requests when > 80% full
- **Distance-Based Routing** - Priority to nearest destination

**Key Files:**
- `Economy/Logistics/LogisticsManager.cs`
- `Economy/Logistics/ResourceRequest.cs`

**Request Priority Levels:**
| Priority | Trigger | Description |
|----------|---------|-------------|
| **5 (Critical)** | < 10% | Building nearly empty |
| **4 (High)** | < 25% | Low stock |
| **3 (Medium)** | < 50% | Half empty |
| **2 (Low)** | < 75% | Comfortable stock |
| **1 (Minimal)** | < 90% | Nearly full |

---

#### 7.5 Advanced Resource Routing
**Core Mechanics:**
- **Direct Producer-Consumer Links** - Bypass warehouse when possible
- **Automatic Warehouse Discovery** - Find nearest warehouse
- **Custom Routes** - Set specific suppliers/consumers
- **Round-Robin Distribution** - Balance deliveries
- **Producer Coordination** - Avoid duplicate deliveries
- **Prefer Direct Supply** - Minimize warehouse trips

**Key Files:**
- `Construction/Core/BuildingResourceRouting.cs` (1339 lines)

**Routing Options:**
```csharp
public class BuildingResourceRouting
{
    public Transform outputDestinationTransform;  // Where to deliver
    public Transform inputSourceTransform;         // Where to get input
    public bool preferDirectSupply = true;        // Producer → Consumer
    public bool preferDirectDelivery = false;     // Bypass warehouse
    public bool enableRoundRobin = true;          // Rotate destinations
    public bool enableCoordination = true;        // Coordinate producers
}
```

---

#### 7.6 Resource Coordinator
**Core Mechanics:**
- **Producer-Consumer Matching** - Smart supply matching
- **Supply Route Tracking** - Track active delivery routes
- **Network Coordination** - Coordinate within road network
- **Route Timeout** - Clear stale routes after 30 seconds
- **Prevent Oversupply** - Avoid duplicate deliveries
- **Network Isolation** - Separate unconnected road islands

**Key Files:**
- `Economy/Storage/ResourceCoordinator.cs` (423 lines)

---

## Infrastructure

### 8. ROAD SYSTEM & PATHFINDING

#### 8.1 Road Network
**Core Mechanics:**
- **Tile-Based Roads** - Build roads on grid cells
- **Road Types** - Different road materials (sand, stone)
- **Road Graph** - Graph structure of connected roads
- **Speed Multipliers** - Different speeds per road type
- **Road Upgrade System** - Improve road quality

**Key Files:**
- `Construction/Roads/RoadManager.cs` (234 lines)
- `Construction/Roads/RoadData.cs`
- `Construction/Roads/RoadTile.cs`

**Road Types:**
| Road Type | Speed Multiplier | Upgrade Cost |
|-----------|------------------|--------------|
| **Sand Road** | 1.0x | Low |
| **Stone Road** | 1.5x | Medium |
| **Paved Road** | 2.0x | High |

---

#### 8.2 Road Building
**Core Mechanics:**
- **Two-Click Road Building** - Click start, then end
- **Continuous Road Building** - Draw roads continuously
- **Placement Validation** - Check grid cells
- **Preview Mode** - Show road path before placement
- **Auto-Connect** - Connect to neighboring roads

**Key Files:**
- `Construction/Roads/RoadBuildHandler.cs`
- `Construction/Input/States/State_RoadBuilding.cs`

---

#### 8.3 Road Operations
**Core Mechanics:**
- **Road Deletion** - Remove individual road tiles
- **Mass Road Deletion** - Remove multiple roads
- **Road Upgrade** - Improve road quality
- **Road Edit Mode** - `State_RoadOperation` for management

**Key Files:**
- `Construction/Roads/RoadOperationHandler.cs`
- `Construction/Input/States/State_RoadOperation.cs`

---

#### 8.4 Pathfinding Algorithms
**Core Mechanics:**
- **BFS Pathfinding** - Breadth-first search for shortest paths
- **Multi-Point BFS** - Find distances from multiple start points
- **Road Access Finding** - Find nearest road connections
- **Path Availability Check** - Test if path exists
- **Distance Map** - Calculate distances in road steps

**Key Files:**
- `Construction/Roads/LogisticsPathfinder.cs` (302 lines)
- `Construction/Roads/RoadPathfinder.cs` (291 lines)

**BFS Algorithm Complexity:**
- **Time:** O(V + E) where V = vertices, E = edges
- **Space:** O(V)
- **Optimal:** Finds shortest path by tile count

---

#### 8.5 Road Coverage Visualization
**Core Mechanics:**
- **Visual Coverage Overlay** - Show service radius
- **Blue Color Coding** - Rich blue for coverage
- **Fade Animation** - Smooth appear/disappear (0.12s)
- **Building Outline** - Highlight covered buildings
- **Multi-Source Coverage** - Show from multiple emitters
- **Efficiency-Based Color** - Color intensity = coverage strength

**Key Files:**
- `Construction/Roads/RoadCoverageVisualizer.cs` (540 lines)

---

## Population & Workforce

### 9. POPULATION & WORKFORCE SYSTEM

#### 9.1 Population Management
**Core Mechanics:**
- **Tiered Population** - 5 population tiers
- **Population Tracking** - Track current and maximum per tier
- **Housing Capacity** - Buildings provide housing limits
- **Population Growth** - Based on housing availability
- **Population Removal** - Deleting buildings removes housing
- **Inspector Statistics** - View population breakdown in real-time

**Key Files:**
- `Economy/Systems/PopulationManager.cs` (245 lines)
- `Economy/Systems/PopulationTier.cs` (11 lines)

**Population Tiers:**
| Tier | Name | Workforce Type | Example Buildings |
|------|------|----------------|-------------------|
| **1** | Farmers | Manual Labor | Farms, Fisheries, Logging |
| **2** | Artisans | Skilled Labor | Workshops, Bakeries |
| **3** | Masters | Expert Labor | Advanced Workshops |
| **4** | White Clergy | Religious | Churches, Chapels |
| **5** | Black Clergy | Monastic | Monasteries, Abbeys |

---

#### 9.2 Workforce Allocation
**Core Mechanics:**
- **Worker Type Assignment** - Buildings need specific worker types
- **Workforce Requirement** - Buildings need X workers
- **Available Workforce** - Track available workers
- **Workforce Ratio** - Calculate worker availability ratio
- **Pause on Worker Shortage** - Pause production if insufficient workers
- **Workforce Distribution** - Automatic fair worker allocation

**Key Files:**
- `Economy/Systems/WorkforceManager.cs` (261 lines)

**Workforce Calculation:**
```csharp
// Calculate workforce ratio (0.0 to 1.0)
float workforceRatio = availableWorkers / requiredWorkers;

// Production efficiency affected by workforce
float efficiency = workforceRatio * otherModifiers;

// Auto-pause if workforce too low
if (workforceRatio < 0.5f)
{
    PauseProduction("Insufficient workforce");
}
```

---

#### 9.3 Workforce Statistics
**Core Mechanics:**
- **Real-Time Tracking** - Monitor workforce supply/demand
- **Inspector Visualization** - View all tier statistics
- **Ratio Calculation** - Ratio of available/required per tier
- **Shortage Detection** - Identify understaffed buildings
- **Workforce Requests** - Buildings request workers

**Key Files:**
- `Economy/Systems/WorkforceManager.cs`

---

## Housing & Happiness

### 10. RESIDENTIAL BUILDINGS & HAPPINESS

#### 10.1 Residential Buildings
**Core Mechanics:**
- **Housing Type** - Residences for different population tiers
- **Housing Capacity** - How many people can live there
- **Needs System** - Residents have needs (food, clothing, luxury)
- **Consumption Intervals** - Periodic need checks
- **Base Taxes** - Base tax rate per residence
- **Tax Bonuses** - Extra tax from satisfied needs

**Key Files:**
- `Economy/Taxation/Residence.cs` (468 lines)

---

#### 10.2 Population Needs
**Core Mechanics:**
- **Need Categories** - Basic, Comfort, Luxury, etc.
- **Resource Consumption** - Each need requires resources
- **Consumption Rate** - Resources per minute per residence
- **Population Bonuses** - Satisfied needs give population
- **Happiness Impact** - Bonus or penalty to happiness
- **Tax Impact** - Needs affect tax income
- **Unlock System** - Needs require population thresholds

**Key Files:**
- `Economy/Taxation/Residence.cs`
- `Economy/Needs/Need.cs`
- `Economy/Needs/NeedCategory.cs`

**Need Categories:**
| Category | Tier | Example Needs | Impact |
|----------|------|---------------|--------|
| **Basic** | All | Food, Water | -50 happiness if unmet |
| **Comfort** | Artisans+ | Clothing, Heating | -20 happiness, +10% tax |
| **Luxury** | Masters+ | Wine, Jewelry | +30 happiness, +50% tax |
| **Religious** | Clergy | Religious Items | +20 happiness |

---

#### 10.3 Happiness System
**Core Mechanics:**
- **Global Happiness Tracking** - Overall population mood
- **Happiness Range** - From -100 to +100
- **Happiness Modifiers** - Add/subtract from happiness
- **Event Chance Modifier** - Low happiness increases events
- **Normalized Values** - Get happiness as 0-1
- **UI Integration** - Display happiness status

**Key Files:**
- `Economy/Happiness/HappinessManager.cs`

**Happiness Effects:**
```csharp
// Event chance modified by happiness
float eventChance = baseChance * (1.0f - normalizedHappiness);

// Tax income modified by happiness
float taxIncome = baseTax * happinessMultiplier;
// happinessMultiplier ranges from 0.5 (unhappy) to 1.5 (happy)
```

---

#### 10.4 Taxation System
**Core Mechanics:**
- **Tax Collection** - Collect from residences
- **Tax Per Minute** - Smooth income system
- **Happiness Multiplier** - Happy population pays more
- **Satisfaction Bonuses** - Extra tax from satisfied needs
- **Tax Manager** - Central tax coordination
- **Minute Ticks** - Recalculate taxes every 60 seconds

**Key Files:**
- `Economy/Taxation/TaxManager.cs`
- `Economy/Taxation/Residence.cs`

**Tax Formula:**
```csharp
float baseTax = residence.baseTaxRate;
float satisfactionBonus = satisfiedNeeds * 0.1f; // +10% per need
float happinessMultiplier = 0.5f + (normalizedHappiness * 1.0f);
float finalTax = baseTax * (1 + satisfactionBonus) * happinessMultiplier;
```

---

### 11. MONEY & ECONOMY SYSTEM

#### 11.1 Currency Management
**Core Mechanics:**
- **Gold/Money Tracking** - Central treasury
- **Starting Balance** - Begin with 100 gold
- **Add Money** - From taxes, sales, etc.
- **Spend Money** - Deduct for building costs
- **Debt Prevention** - Cannot build if negative money
- **Singleton Manager** - Central money system

**Key Files:**
- `Economy/Money/MoneyManager.cs`

---

#### 11.2 Economic Balance
**Core Mechanics:**
- **Expense Tracking** - Construction costs
- **Income Tracking** - Tax revenue
- **Balance Monitoring** - Prevent debt
- **Building Cost Penalties** - More expensive if in debt (future mechanic)
- **Economic Status Indicator** - Show financial health

**Key Files:**
- `Economy/Money/MoneyManager.cs`
- `Economy/Taxation/TaxManager.cs`

---

## Influence Systems

### 12. AURA & INFLUENCE SYSTEM

#### 12.1 Aura Emitters
**Core Mechanics:**
- **Building Influence Zones** - Buildings affect nearby areas
- **Aura Types** - Market, Hospital, Police, Warehouse
- **Radius Configuration** - Set coverage distance
- **Radial Distribution** - Simple radius-based coverage
- **Road-Based Distribution** - Coverage along road network
- **Event Mitigation** - Auras reduce event chances
- **Configurable Mitigation** - Set % reduction (0-100%)

**Key Files:**
- `Economy/Aura/AuraEmitter.cs`
- `Economy/Aura/AuraType.cs`

**Aura Types:**
| Aura Type | Purpose | Distribution | Range |
|-----------|---------|--------------|-------|
| **Market** | Shop access | Road-Based | 20 tiles |
| **Hospital** | Health service | Radial | 15 tiles |
| **Police** | Safety | Radial | 25 tiles |
| **Warehouse** | Logistics hub | Road-Based | 30 tiles |

---

#### 12.2 Aura Manager
**Core Mechanics:**
- **Emitter Registration** - Track all active auras
- **Coverage Checking** - Test if position in aura
- **Multi-Type Coverage** - Check specific aura type
- **Road Access Validation** - Check road connectivity
- **Neighbor Detection** - Find adjacent cells

**Key Files:**
- `Economy/Aura/AuraManager.cs`

---

#### 12.3 Aura Distribution Types
**Core Mechanics:**
- **Radial Aura** - Coverage based on Euclidean distance
- **Road-Based Aura** - Coverage only along road network
- **Hybrid Support** - Different buildings use different types

**Key Files:**
- `Economy/Aura/AuraDistributionType.cs`

**Distribution Comparison:**
```csharp
// Radial - simple distance check
bool inRange = Vector2.Distance(buildingPos, targetPos) <= radius;

// Road-Based - requires road path
bool inRange = pathfinder.HasPath(buildingPos, targetPos, maxDistance: radius);
```

---

#### 12.4 Aura Visualization
**Core Mechanics:**
- **Coverage Display** - Show aura range on screen
- **Blue Highlighting** - Visual feedback for covered areas
- **Fade In/Out** - Smooth animation (0.12s)
- **Multi-Source Display** - Show multiple auras combined
- **Efficiency Visualization** - Color intensity = coverage strength

**Key Files:**
- `Construction/Roads/RoadCoverageVisualizer.cs` (540 lines)
- `Economy/Aura/RadiusVisualizer.cs`

---

## Modular Buildings

### 13. MODULAR BUILDING SYSTEM

#### 13.1 Modular Building Base
**Core Mechanics:**
- **Building with Slots** - Main building has attachment points
- **Module Management** - Dynamically add/remove modules
- **Module Limits** - Maximum modules per building
- **Module List** - Track active modules
- **Slot System** - Predefined slots for modules

**Key Files:**
- `Construction/Modular Buildings/ModularBuilding.cs`

---

#### 13.2 Building Modules
**Core Mechanics:**
- **Farm Fields** - Agricultural enhancement modules
- **Pastures** - Livestock modules
- **Module Bonus** - +20% production per module
- **Module Components** - Track individual modules
- **Modular Building Integration** - Modules enhance parent

**Key Files:**
- `Construction/Modular Buildings/BuildingModule.cs`

**Module Bonus Calculation:**
```csharp
int moduleCount = modularBuilding.GetModuleCount();
float productionBonus = 1.0f + (moduleCount * 0.20f);
// 0 modules = 1.0x (100%)
// 1 module  = 1.2x (120%)
// 2 modules = 1.4x (140%)
// 3 modules = 1.6x (160%)
```

---

#### 13.3 Module Placement
**Core Mechanics:**
- **Adjacent Cell Placement** - Place modules next to main building
- **Slot Visualization** - Show valid placement locations
- **Module State** - Track each module
- **Module Add Mode** - `State_PlacingModule` for placement
- **Quantity Limit** - Configurable maximum

**Key Files:**
- `Construction/Input/States/State_PlacingModule.cs`
- `Construction/Modular Buildings/ModularBuilding.cs`

---

### 14. ZONED AREAS SYSTEM

#### 14.1 Zoned Buildings
**Core Mechanics:**
- **Monastery/Temple Zones** - Predefined building layouts
- **Zone Container** - Main zone building
- **Build Slots** - Predefined placement locations
- **Slot Filtering** - Allow only specific buildings
- **Slot Management** - Track occupied/empty slots

**Key Files:**
- `Construction/Modular Buildings/ZonedArea.cs`

---

#### 14.2 Build Slots
**Core Mechanics:**
- **Individual Slots** - Each with position/size
- **Slot Availability** - Occupied vs. empty
- **Building Placement** - Place buildings in slots
- **Slot Filtering** - Restrict by building type/size
- **Slot Validation** - Check if building fits

**Key Files:**
- `Construction/Core/BuildSlot.cs` (21 lines)

---

#### 14.3 Zone UI Feedback
**Core Mechanics:**
- **Slot Highlighting** - Visually show valid slots
- **Slot Selection** - Click slot to build there
- **Zone Info Display** - Show available slots

**Key Files:**
- `Construction/Modular Buildings/ZonedArea.cs`
- `UI/UIManager.cs`

---

## Events System

### 15. EVENT SYSTEM

#### 15.1 Random Events
**Core Mechanics:**
- **Pandemic Events** - Disease outbreak affecting residences
- **Riot Events** - Unrest affecting production and residences
- **Event Probability** - Configurable base chances (7%)
- **Event Duration** - Time until event cleanup
- **Happiness Modifier** - Low happiness increases event chance
- **Event Unlocking** - Events tied to building types

**Key Files:**
- `Economy/Event/EventManager.cs` (431 lines)

**Event Probability Formula:**
```csharp
float baseChance = 0.07f; // 7%
float happinessMod = 1.0f - normalizedHappiness; // 0.0 to 1.0
float finalChance = baseChance * happinessMod;

// Examples:
// Happiness 100 → 7% * 0.0 = 0% chance
// Happiness 50  → 7% * 0.5 = 3.5% chance
// Happiness 0   → 7% * 1.0 = 7% chance
```

---

#### 15.2 Event Management
**Core Mechanics:**
- **Event Registry** - Track all affected buildings
- **Event Tracking** - Which buildings have active events
- **Event Duration** - Pandemics (5 min), Riots (3 min)
- **Automatic Cleanup** - Remove expired events
- **Event Statistics** - Count active events
- **Check Interval** - Configurable (every 1 minute default)

**Key Files:**
- `Economy/Event/EventManager.cs`

**Event Configuration:**
| Event Type | Base Chance | Duration | Affected Buildings |
|------------|-------------|----------|-------------------|
| **Pandemic** | 7% | 5 minutes | Residences |
| **Riot** | 7% | 3 minutes | Residences + Production |

---

#### 15.3 Event Impact on Buildings
**Core Mechanics:**
- **EventAffected Component** - Mark buildings vulnerable to events
- **Production Penalty** - Reduce efficiency during event
- **Population Impact** - Affect resident health/mood
- **Event Duration** - Time until cleanup
- **Stacking Events** - Multiple events possible

**Key Files:**
- `Economy/Event/EventAffected.cs`
- `Economy/Event/BuildingEvent.cs`

**Impact Examples:**
```csharp
// Pandemic on Residence
productionEfficiency *= 0.5f;  // -50% efficiency
populationGrowth = 0f;         // No growth
happiness -= 30;               // -30 happiness

// Riot on Production Building
productionEfficiency *= 0.3f;  // -70% efficiency
workerAvailability *= 0.6f;    // -40% workers
```

---

#### 15.4 Event Configuration
**Core Mechanics:**
- **Event Type Enum** - Pandemic, Riot types
- **Base Chance Settings** - Configure probabilities
- **Duration Settings** - Control event length
- **Happiness Multiplier** - Control happiness influence
- **Max Happiness Reduction** - Minimum event chance

**Key Files:**
- `Economy/Event/EventType.cs`
- `Economy/Event/EventManager.cs`

---

## User Interface

### 16. UI SYSTEM

#### 16.1 Main UI Manager
**Core Mechanics:**
- **Info Panel** - Display selected building information
- **Building Name Display** - Show what's selected
- **Module Button Container** - Dynamic UI for modules
- **Production Panel** - Show production controls
- **Warehouse Panel** - Display warehouse queue
- **Balance Panel** - Economy overview
- **Confirmation Dialog** - Confirm destructive actions

**Key Files:**
- `UI/UIManager.cs` (314 lines)

---

#### 16.2 Resource Display
**Core Mechanics:**
- **Resource Counters** - Show Wood, Stone, Planks
- **Population Counter** - Display population statistics
- **Money Display** - Show treasury balance
- **Real-Time Updates** - Update every frame
- **Formatted Numbers** - Show rounded whole numbers

**Key Files:**
- `UI/UIResourceDisplay.cs`

**UI Update Pattern:**
```csharp
void Update()
{
    woodText.text = Mathf.RoundToInt(ResourceManager.Instance.GetAmount(ResourceType.Wood)).ToString();
    stoneText.text = Mathf.RoundToInt(ResourceManager.Instance.GetAmount(ResourceType.Stone)).ToString();
    moneyText.text = MoneyManager.Instance.CurrentMoney.ToString("F0");
}
```

---

#### 16.3 Notification System
**Core Mechanics:**
- **In-Game Notifications** - Popup messages
- **Auto-Hide** - Disappear after 3 seconds
- **Status Messages** - Mode changes, errors, confirmations
- **Bug Fix (#13)** - Prevent message sticking

**Key Files:**
- `UI/NotificationManager.cs`

**Notification Types:**
- **Info:** Mode changes, selections
- **Warning:** Low resources, worker shortage
- **Error:** Invalid placement, insufficient funds
- **Success:** Building completed, upgrade successful

---

#### 16.4 Building Status Display
**Core Mechanics:**
- **Production Efficiency** - Show % productivity
- **Efficiency Slider** - UI manual control
- **Warehouse Queue Info** - Display cart count
- **Building Selection Info** - Show name and statistics

**Key Files:**
- `UI/BuildingStatusVisualizer.cs`

---

#### 16.5 Resource Balance Panel UI
**Core Mechanics:**
- **Economy Overview** - Income/expense overview
- **Balance Report** - Show financial status
- **Panel Toggle** - Show/hide details
- **Real-Time Updates** - Sync with game state

**Key Files:**
- `UI/UI_ResourceBalancePanel.cs`

---

### 17. BUILD UI MANAGEMENT

#### 17.1 Build Menu System
**Core Mechanics:**
- **Building Menu** - List of available buildings
- **Category Filtering** - Group buildings by type
- **Build Button Handling** - Trigger build mode
- **Hotkey Support** - Keyboard shortcuts
- **Icon Display** - Building icons/thumbnails

**Key Files:**
- `Construction/UI/BuildUIManager.cs`

---

#### 17.2 Placement Validation UI
**Core Mechanics:**
- **Visual Feedback** - Green/red placement indicators
- **Validation Messages** - Show why placement invalid
- **Tooltip System** - Hover information
- **Cost Display** - Show building costs

**Key Files:**
- `Construction/UI/PlacementValidation.cs`

---

## Visual Feedback

### 18. VISUAL EFFECTS & BUILDING FEEDBACK

#### 18.1 Visual State System
**Core Mechanics:**
- **Ghost State** - Green preview
- **Invalid State** - Red invalid placement
- **Blueprint State** - Blue transparent planned
- **Real State** - Normal completed building
- **Material Swapping** - Change visual on state

**Key Files:**
- `Construction/Core/BuildingVisuals.cs` (96 lines)

**Visual State Table:**
| State | Color | Material | Purpose |
|-------|-------|----------|---------|
| **Ghost** | Green | Transparent | Valid placement preview |
| **Invalid** | Red | Transparent | Cannot place here |
| **Blueprint** | Blue | Semi-transparent | Planned building |
| **Real** | Normal | Opaque | Completed building |

---

#### 18.2 Building Highlighting
**Core Mechanics:**
- **Selection Highlight** - Show selected building
- **Aura Coverage** - Highlight covered buildings
- **Mode Feedback** - Visual feedback for current mode

**Key Files:**
- `Construction/Core/BuildingVisuals.cs`
- `Construction/Core/SelectionManager.cs`

---

#### 18.3 Ghost Building System
**Core Mechanics:**
- **Dynamic Ghost Creation** - Create preview on demand
- **Ghost Positioning** - Follow mouse in build mode
- **Ghost Rotation** - Rotate preview with building
- **Ghost Validation** - Color based on placement validity
- **Ghost Cleanup** - Destroy when exiting mode

**Key Files:**
- `Construction/Core/BuildingManager.cs`
- `Construction/Core/GhostBuildingCollider.cs` (26 lines)

---

#### 18.4 Cart Path Visualization
**Core Mechanics:**
- **Path Display** - Show cart routes
- **Visual Feedback** - Highlight active deliveries
- **Debug Lines** - Show pathfinding for testing

**Key Files:**
- `Economy/Warehouse/CartPathVisualizer.cs`

---

## Data & Utilities

### 19. BUILDING IDENTITY & METADATA

#### 19.1 Building Identification
**Core Mechanics:**
- **Unique Identity** - Each building has identity component
- **Building Data Reference** - Link to configuration
- **Grid Position** - Store grid cell location
- **Rotation Tracking** - Store current rotation
- **Blueprint State** - Track if blueprint or real

**Key Files:**
- `Construction/Core/BuildingIdentity.cs` (42 lines)

---

#### 19.2 Building Data (ScriptableObject)
**Core Mechanics:**
- **Building Name** - Display name
- **Building Size** - Width/height in grid cells
- **Construction Cost** - Resource requirements
- **Building Prefab** - Prefab reference
- **Upkeep Cost** - Maintenance expenses
- **Mass Build Flag** - Allow brush tool usage

**Key Files:**
- `Construction/Core/BuildingData.cs` (59 lines)

**BuildingData Structure:**
```csharp
[CreateAssetMenu(fileName = "NewBuilding", menuName = "Buildings/BuildingData")]
public class BuildingData : ScriptableObject
{
    public string buildingName;
    public Vector2Int size;
    public List<ResourceCost> costs;
    public GameObject buildingPrefab;
    public float upkeepCost;
    public bool allowMassBuild;
    public Sprite buildingIcon;
}
```

---

### 20. ORCHESTRATION & MANAGEMENT

#### 20.1 Build Orchestrator
**Core Mechanics:**
- **Construction Coordination** - Manage placement steps
- **Resource Checking** - Validate before building
- **Grid Registration** - Register building on grid
- **Event Triggering** - Notify systems of new building

**Key Files:**
- `Construction/Core/BuildOrchestrator.cs` (109 lines)

**Orchestration Flow:**
```
1. Validate Resources
2. Validate Grid Space
3. Validate Money
4. Deduct Resources
5. Instantiate Building
6. Register on Grid
7. Trigger Events
8. Update UI
```

---

#### 20.2 Time Management
**Core Mechanics:**
- **Time Tracking** - Real-time or in-game time
- **Ticks/Intervals** - Periodic updates
- **Speed Control** - Game speed multiplier (future)

**Key Files:**
- `Infrastructure/TimeManager.cs`

---

### 21. UTILITY & DATA SYSTEMS

#### 21.1 Economic Data Types
**Core Mechanics:**
- **ResourceCost** - Define cost (type + amount)
- **StorageData** - Storage info (current + max)
- **ResourceRequest** - Delivery request data

**Key Files:**
- `Economy/Core/ResourceCost.cs`
- `Economy/Core/StorageData.cs`
- `Economy/Logistics/ResourceRequest.cs`
- `Economy/Core/EconomyDataTypes.cs`

---

#### 21.2 Interface System
**Core Mechanics:**
- **IResourceProvider** - Supply interface (warehouses, producers)
- **IResourceReceiver** - Demand interface (factories, warehouses)
- **IInputState** - Input state interface

**Key Files:**
- `Economy/Storage/IResourceProvider.cs`
- `Economy/Storage/IResourceReceiver.cs`
- `Construction/Input/IInputState.cs`

**Interface Pattern:**
```csharp
public interface IResourceProvider
{
    bool HasResource(ResourceType type, float amount);
    void TakeResource(ResourceType type, float amount);
    ResourceType GetProvidedResourceType();
}

public interface IResourceReceiver
{
    bool CanReceiveResource(ResourceType type, float amount);
    void ReceiveResource(ResourceType type, float amount);
    ResourceType GetRequestedResourceType();
}
```

---

### 22. SCENE & WORLD MANAGEMENT

#### 22.1 Manager Instantiation
**Core Mechanics:**
- **Singleton Pattern** - Single instance per manager
- **Scene Setup** - All managers in scene
- **Dependency Injection** - Managers reference each other
- **Null Checks** - Fallback `FindFirstObjectByType`

**Key Files:**
- Various manager `Awake()` methods

**Singleton Pattern:**
```csharp
public static ResourceManager Instance { get; private set; }

void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
}
```

---

#### 22.2 Road Network Persistence
**Core Mechanics:**
- **Save Road Graph** - Persist road network
- **Road Tile Storage** - Store road locations
- **Graph Rebuild** - Restore from scene state

**Key Files:**
- `Construction/Roads/RoadManager.cs`

---

## Statistics Summary

### 📈 TOTAL STATISTICS

**Code Metrics:**
- **Total C# Scripts:** 90 files
- **Lines of Code:** 10,789+ (excluding comments/whitespace)
- **Largest File:** `BuildingResourceRouting.cs` (1339 lines)
- **Second Largest:** `BuildingManager.cs` (1306 lines)
- **Third Largest:** `CartAgent.cs` (1238 lines)

**Game Systems:**
- **Input States:** 13 different modes
- **Resource Types:** 75+ resources
- **Population Tiers:** 5 tiers
- **Event Types:** 2 types (Pandemic, Riot)
- **Building Types:** Unlimited (data-driven)
- **Grid Size:** 500×500 cells (250,000 total)
- **Road Graph:** Dynamic network
- **Aura Types:** 4 types (Market, Hospital, Police, Warehouse)
- **Cargo Slots:** 3 per cart
- **Max Cart Queue:** 1-5 configurable per warehouse

**Production System:**
- **Production States:** 3 (Working, Paused, Full)
- **Efficiency Range:** 0-100%
- **Module Bonus:** +20% per module
- **Max Modules:** Configurable per building
- **Ramp Time:** Configurable acceleration/deceleration

**Economic System:**
- **Starting Money:** 100 gold
- **Starting Resources:** 100 Wood, 50 Stone
- **Refund Rate:** 50% on building deletion
- **Tax Frequency:** Per minute (60s ticks)
- **Need Categories:** 5+ categories

**Logistics System:**
- **Cart States:** 6 state machine states
- **Cargo Capacity:** 5 units per slot × 3 slots = 15 total
- **Pathfinding:** BFS algorithm (O(V+E) complexity)
- **Priority Levels:** 1-5 (Minimal to Critical)
- **Request Threshold:** < 25% triggers request
- **Cancel Threshold:** > 80% cancels request

**Visual System:**
- **Building States:** 4 visual states
- **Fade Duration:** 0.12 seconds
- **Coverage Color:** Rich blue (#0080FF)
- **Zoom Range:** 15 (min) to 100 (max)

---

## Feature Completion Checklist

### ✅ Fully Implemented Systems

- [x] Building placement with ghost preview
- [x] Mass building operations (copy/move/delete)
- [x] 13-state input system
- [x] 500×500 grid world
- [x] 75+ resource types
- [x] Production cycles with efficiency
- [x] Warehouse & cart logistics
- [x] BFS pathfinding on roads
- [x] 5-tier population system
- [x] Workforce allocation
- [x] Housing & needs system
- [x] Happiness tracking
- [x] Taxation system
- [x] Money management
- [x] Aura/influence system
- [x] Modular buildings (farms)
- [x] Zoned areas (monasteries)
- [x] Event system (pandemics, riots)
- [x] Blueprint mode
- [x] Road building & upgrading
- [x] Resource routing coordination
- [x] UI notification system
- [x] Visual feedback system

---

## Architecture Highlights

### 🏛️ Design Patterns Used

1. **Singleton Pattern** - 8+ managers
2. **State Pattern** - 13 input states
3. **Observer Pattern** - Event-driven UI
4. **Strategy Pattern** - Aura distribution types
5. **Component Pattern** - Unity ECS approach
6. **Object Pool Pattern** - `ListPool<T>`
7. **Manager Pattern** - Dedicated subsystem managers
8. **Data-Driven Design** - ScriptableObject configs

### 🔄 Key Algorithms

1. **BFS Pathfinding** - Road navigation (O(V+E))
2. **Grid Collision Detection** - O(1) cell lookup
3. **Resource Matching** - Producer-consumer coordination
4. **Efficiency Calculation** - Multi-factor production
5. **Event Probability** - Happiness-modified randomness

### 📊 Performance Optimizations

1. **Object Pooling** - Reduce GC pressure
2. **Event-Driven Updates** - Avoid Update() loops
3. **Cached Components** - Minimize GetComponent calls
4. **2D Array Grid** - O(1) lookup vs. Dictionary
5. **Coroutines** - Spread work over frames

---

## Version History

### 2025-11-18 - Version 1.0.0 - Initial Documentation
- Complete feature catalog created
- All 22 major systems documented
- Statistics and metrics compiled
- Code examples and formulas added
- Cross-referenced with CLAUDE.md

---

## Usage Notes

### For Developers
This document serves as a **feature reference** - use it to:
- Understand what systems exist
- Find implementing files
- Learn system interactions
- Identify dependencies

### For AI Assistants
When modifying code:
- Check if feature already exists here
- Reference implementing files
- Understand system integration
- Maintain consistency with existing patterns

### For Designers
Use this to:
- Understand available mechanics
- Plan new features building on existing systems
- Balance game economy
- Configure ScriptableObject data

---

**Last Updated:** 2025-11-18
**Maintained By:** AI Assistant (Claude)
**Companion Document:** CLAUDE.md (Technical Documentation)
**Total Features Documented:** 200+
