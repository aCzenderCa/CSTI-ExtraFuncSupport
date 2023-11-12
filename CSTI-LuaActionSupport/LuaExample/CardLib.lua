---@class CardAccessBridge
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
---@field public AddCard fun(this:CardAccessBridge, id:string, count:number, ext:table):void @opt:count, ext
---@field public Remove fun(this:CardAccessBridge, doDrop:boolean):void
CardAccessBridge = {}

---@class DataNodeTableAccessBridge
---@field public LuaKeys table
---@field public [string] DataNodeData
---@field public Table Dictionary<string, DataNode> @readonly
---@field public Count number
DataNodeTableAccessBridge = {}

---@class Dictionary<TKey,TVal>:IEnumerable
---@field [TKey] TVal
Dictionary = {}

---@class CommonSimpleAccess
---@field public AccessObj any @readonly
---@field public [string] any
CommonSimpleAccess = {}

---@class SimpleUniqueAccess:CommonSimpleAccess
---@field public CardDescription string
---@field public Gen fun(this:SimpleUniqueAccess, count:number, ext:table):void
---@field public StatValue number
---@field public StatValueMin number
---@field public StatValueMax number
---@field public StatRate number
---@field public StatRateMin number
---@field public StatRateMax number
---@field public CacheRawValRange fun(this:SimpleUniqueAccess, x:number, y:number):void
---@field public CacheRawRateRange fun(this:SimpleUniqueAccess, x:number, y:number):void
SimpleUniqueAccess = {}

---@class GameStatAccessBridge
---@field public Value number
---@field public Rate number
GameStatAccessBridge = {}

---@class CardData
CardData = {}

---@class GameStat
GameStat = {}

---@class IList:IEnumerable
---@field public [number] any
IList = {}


---@class List<TItem>:IList
---@field public [number] TItem
---@field public Count number @readonly
List = {}
