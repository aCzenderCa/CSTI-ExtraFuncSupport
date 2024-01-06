---@class CardAccessBridge:ITransProvider
---@field CardBase InGameCardBase
---@field CardModel SimpleUniqueAccess @readonly
---@field IsEquipped boolean @readonly
---@field IsInHand boolean @readonly
---@field IsInBase boolean @readonly
---@field IsInLocation boolean @readonly
---@field IsInBackground boolean @readonly
---@field CurrentContainer CardAccessBridge|nil @readonly
---@field CheckInventory fun(this:CardAccessBridge,useAll:boolean, ...:string):boolean
---@field CheckTagInventory fun(this:CardAccessBridge,useAll:boolean, ...:string):boolean
---@field CheckRegexTagInventory fun(this:CardAccessBridge,useAll:boolean, ...:string):boolean
---@field HasInInventory fun(this:CardAccessBridge, uid:string, needCount:number):boolean
---@field LiquidInventory fun(this:CardAccessBridge):CardAccessBridge
---@field InventorySlotCount number @readonly
---@field [string] any
---@field [number] List<CardAccessBridge>
---@field SlotType string
---@field CardType string
---@field Weight number
---@field HasTag fun(this:CardAccessBridge, tag:string):boolean
---@field TravelCardIndex number
---@field Id string @readonly
---@field Data DataNodeTableAccessBridge
---@field InitData fun(this:CardAccessBridge):void
---@field SaveData fun(this:CardAccessBridge):void
---@field Spoilage number
---@field Usage number
---@field Fuel number
---@field Progress number
---@field Special1 number
---@field Special2 number
---@field Special3 number
---@field Special4 number
---@field LiquidQuantity number
---@field MoveToSlot fun(this:CardAccessBridge, slotType:"Equipment"|"Base"|"Hand"|"Location"):boolean
---@field MoveTo fun(this:CardAccessBridge, cardAccessBridge:CardAccessBridge):boolean
---@field AddCard fun(this:CardAccessBridge, id:string, count?:number, ext?:table<string,number|SimpleUniqueAccess|DataNodeTableAccessBridge>|Gen_ext):void
---@field Remove fun(this:CardAccessBridge, doDrop:boolean, dontInstant?:boolean):void
---@field AddAnim fun(this:CardAccessBridge, animList:table<string>|nil, animTimeList:table<number>|nil):void
---@field RemoveAnim fun(this:CardAccessBridge):boolean
---
---@class InGameCardBase

---@class ITransProvider

---@alias DataNodeData_lua number|string|boolean|table<string,DataNodeData_lua>|nil|Vector2
---@class DataNodeTableAccessBridge
---@field public LuaKeys table<number,string>
---@field public [string] DataNodeData
---@field public Table Dictionary<string, DataNode> @readonly
---@field public LuaTable table<string,DataNodeData_lua>
---@field public Count number

---@class Dictionary<TKey2,TVal2>:IEnumerable<KeyValuePair<TKey2, TVal2>>
---@field [TKey2] TVal2
---@field Count number

---@class KeyValuePair<TKey,TVal>
---@field Key TKey
---@field Value TVal

---@class CommonSimpleAccess
---@field public AccessObj any @readonly
---@field public [string] any

---@shape Gen_ext
---@field Usage number
---@field Fuel number
---@field Spoilage number
---@field ConsumableCharges number
---@field Liquid number
---@field Special1 number
---@field Special2 number
---@field Special3 number
---@field Special4 number
---@field LiquidCard SimpleUniqueAccess
---@field count number
---@field initData DataNodeTableAccessBridge
---@field [any] nil

---@class SimpleUniqueAccess:CommonSimpleAccess
---@field public CardDescription string
---@field public Gen fun(this:SimpleUniqueAccess, count?:number, ext?:table<string,number|SimpleUniqueAccess|DataNodeTableAccessBridge>|Gen_ext):void
---@field public StatValue number
---@field public StatValueMin number
---@field public StatValueMax number
---@field public StatRate number
---@field public StatRateMin number
---@field public StatRateMax number
---@field public CacheRawValRange fun(this:SimpleUniqueAccess, x:number, y:number):void
---@field public CacheRawRateRange fun(this:SimpleUniqueAccess, x:number, y:number):void

---@class GameStatAccessBridge
---@field public Value number
---@field public Rate number

---@class CardData

---@class GameStat

---@class ICollection<TItem1>:IEnumerable<TItem1>
---@field Count number

---@class IList<TItem1>:ICollection<TItem1>
---@field public [number] TItem1

---@class List<TItem>:IList<TItem>

---@class CardOnCardAction:CardAction

---@class CardAction

---@class DismantleActionButton

---@class DismantleCardAction:CardAction