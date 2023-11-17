---@class CardAccessBridge
---@field public CardBase InGameCardBase
---@field public CardModel SimpleUniqueAccess @readonly
---@field public IsEquipped boolean @readonly
---@field public IsInHand boolean @readonly
---@field public IsInBase boolean @readonly
---@field public IsInLocation boolean @readonly
---@field public CheckInventory fun(this:CardAccessBridge, ...:string):boolean
---@field public HasInInventory fun(this:CardAccessBridge, uid:string, needCount:number):boolean
---@field public LiquidInventory fun(this:CardAccessBridge):CardAccessBridge
---@field public [string] any
---@field public [number] List<CardAccessBridge>
---@field public SlotType string
---@field public CardType string
---@field public Weight number
---@field public HasTag fun(this:CardAccessBridge, tag:string):boolean
---@field public TravelCardIndex number
---@field public Id string @readonly
---@field public Data DataNodeTableAccessBridge
---@field public InitData fun(this:CardAccessBridge):void
---@field public SaveData fun(this:CardAccessBridge):void
---@field public Spoilage number
---@field public Usage number
---@field public Fuel number
---@field public Progress number
---@field public Special1 number
---@field public Special2 number
---@field public Special3 number
---@field public Special4 number
---@field public LiquidQuantity number
---@field public AddCard fun(this:CardAccessBridge, id:string, count?:number, ext?:table<string,number|SimpleUniqueAccess|DataNodeTableAccessBridge>|Gen_ext):void
---@field public Remove fun(this:CardAccessBridge, doDrop:boolean):void

---@class InGameCardBase

---@class DataNodeTableAccessBridge
---@field public LuaKeys table
---@field public [string] DataNodeData
---@field public Table Dictionary<string, DataNode> @readonly
---@field public Count number

---@class Dictionary<TKey1,TVal1>:IEnumerable<KeyValuePair<TKey1, TVal1>>
---@field [TKey1] TVal1
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

---@class ICollection<TItem>:IEnumerable<TItem>
---@field Count number

---@class IList<TItem>:ICollection<TItem>
---@field public [number] TItem

---@class List<TItem>:IList<TItem>