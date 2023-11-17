ModData = {}

---@type CardAccessBridge
receive = nil

---@type CardAccessBridge
given = nil

---@class LuaScriptRetValues
---@field [string] any

---@type LuaScriptRetValues
Ret = nil

---@class GameManager

---@type GameManager
gameManager = nil

---@type CardAccessBridge
env = nil

---@type CardAccessBridge
exp = nil

---@type CardAccessBridge
weather = nil

---@class CardTypes
---@class DataNodeType

---@class DataNodeTypeBase
---@field Number DataNodeType
---@field Str DataNodeType
---@field Bool DataNodeType
---@field Table DataNodeType
---@field Nil DataNodeType
---@field Vector2 DataNodeType
DataNodeType = {}

---@class CardTypesBase
---@field Item CardTypes
---@field Base CardTypes
---@field Location CardTypes
---@field Event CardTypes
---@field Environment CardTypes
---@field Weather CardTypes
---@field Hand CardTypes
---@field Blueprint CardTypes
---@field Explorable CardTypes
---@field Liquid CardTypes
---@field EnvImprovement CardTypes
---@field EnvDamage CardTypes
CardTypes = {}

---@alias DataNodeData number|string|boolean|DataNodeTableAccessBridge|nil|Vector2
---@class Enum
---@field public Foreach fun<TItem>(this:Enum, enumerable:IEnumerable<TItem>, func:fun(x:TItem):void):void
---@field public Sum fun<TItem>(this:Enum, enumerable:IEnumerable<TItem>, init:number, func:fun(sum:number, x:TItem):number):number
---@field public Map fun<TItem, TInto>(this:Enum, enumerable:IEnumerable<TItem>, func:fun(x:TItem):TInto):IList<TInto>
---@field public Pairs fun<TItem>(this:Enum, list:IList<TItem>):(fun(list:IEnumerator<TItem>, i:number):(number, TItem), IEnumerator<TItem>, number)
---@field public Pairs fun<TKey,TVal>(this:Enum, dict:Dictionary<TKey,TVal>):(fun(dict:Dictionary<TKey,TVal>, key:TKey):(TKey,TVal), Dictionary<TKey,TVal>, TKey)
Enum = {}

---@class DebugBridge
---@field info any @write only
---@field debug any @write only
---@field warn any @write only
---@field error any @write only
debug = {}

---@class IEnumerable<TItem>
---@field GetEnumerator fun(this:IEnumerable<TItem>):IEnumerator<TItem>

---@class IEnumerator<TItem>
---@field MoveNext fun(this:IEnumerator<TItem>):boolean
---@field Current TItem

---@class DataNode
---@field NodeType DataNodeType
---@field number number
---@field str string
---@field _bool boolean
---@field vector2 Vector2
---@field table Dictionary<string, DataNode>

---@class Vector2
---@field x number
---@field y number

---@class Register
---@field Reg fun(this:Register, klass:string, method:string, uid:string, function:function)
---@field Reg fun(this:Register, klass:"CardOnCardAction", method:"CardsAndTagsAreCorrect", uid:string, function:fun(__instance:CardOnCardAction,_Receiving:CardAccessBridge,_Given:CardAccessBridge,result:boolean):boolean)
---@field Reg fun(this:Register, klass:"CardAction", method:"CardsAndTagsAreCorrect", uid:string, function:fun(__instance:CardAction,_Receiving:CardAccessBridge,result:boolean):boolean)
---@field Reg fun(this:Register, klass:"InGameStat", method:"CurrentValue", uid:string, function:fun(__instance:GameStatAccessBridge,__instance_StatModel:SimpleUniqueAccess,result:number,_NotAtBase:boolean):number)
---@field Reg fun(this:Register, klass:"InGameCardBase", method:"CanReceiveInInventory", uid:string, function:fun(__instance:CardAccessBridge,_Card:SimpleUniqueAccess,_WithLiquid:SimpleUniqueAccess):boolean)
---@field Reg fun(this:Register, klass:"InGameCardBase", method:"CanReceiveInInventoryInstance", uid:string, function:fun(__instance:CardAccessBridge,_Card:CardAccessBridge):boolean)
---@field Reg fun(this:Register, klass:"InGameCardBase", method:"InventoryWeight", uid:string, function:fun(__instance:CardAccessBridge,result:number):number)
---@field Reg fun(this:Register, klass:"InGameCardBase", method:"CardName", uid:string, function:fun(__instance:CardAccessBridge,_IgnoreLiquid:boolean):string)
---@field Reg fun(this:Register, klass:"InGameCardBase", method:"CardDescription", uid:string, function:fun(__instance:CardAccessBridge,_IgnoreLiquid:boolean):string)
---@field Reg fun(this:Register, klass:"GameManager", method:"ChangeStatValue", uid:string, function:fun(__instance:GameManager,_Stat:GameStatAccessBridge,_Value:number,_Modification:number):void)
---@field Reg fun(this:Register, klass:"GameManager", method:"ChangeStatRate", uid:string, function:fun(__instance:GameManager,_Stat:GameStatAccessBridge,_Rate:number,_Modification:number):void)
---@field Reg fun(this:Register, klass:"DismantleActionButton", method:"Setup", uid:string, function:fun(__instance:DismantleActionButton,_Action:DismantleCardAction,_Card:CardAccessBridge,_Action_ActionName_LocalizationKey:string):(boolean,boolean))
Register = {}

---@class SimpleAccessTool
---@field public [string] SimpleUniqueAccess
---@field public ClearCurrentEnv fun():void
SimpleAccessTool = {}

---@param key string
function SaveCurrentSlot(key, val)
end

---@param key string
function SaveGlobal(key, val)
end

---@param key string
---@return DataNodeData
function LoadCurrentSlot(key)
    return nil
end

---@param key string
---@return DataNodeData
function LoadGlobal(key)
    return nil
end

---@param id string
---@return CardData
function GetCard(id)
    return nil
end

---@param id string
---@return CardAccessBridge
function GetGameCard(id)
    return nil
end

---@param tag string
---@return CardAccessBridge
function GetGameCardByTag(tag)
    return nil
end

---@shape GetGameCards_ext
---@field type string
---@field [any] nil
---@param id string
---@param ext? GetGameCards_ext
---@return List<CardAccessBridge>
function GetGameCards(id, ext)
    return nil
end

---@param tag string
---@return List<CardAccessBridge>
function GetGameCardsByTag(tag)
    return nil
end

---@param id string
---@param _CountInInventories? boolean
---@param _CountInBackground? boolean
---@return number
function CountCardOnBoard(id, _CountInInventories, _CountInBackground)
    return nil
end

---@param id string
---@param _CountInInventories? boolean
---@return number
function CountCardInBase(id, _CountInInventories)
    return nil
end

---@param id string
---@return number
function CountCardEquipped(id)
    return nil
end

---@param id string
---@param _CountInInventories? boolean
---@return number
function CountCardInHand(id, _CountInInventories)
    return nil
end

---@param id string
---@param _CountInInventories? boolean
---@return number
function CountCardInLocation(id, _CountInInventories)
    return nil
end

---@param id string
---@return GameStat
function GetStat(id)
    return nil
end

---@param id string
---@return GameStatAccessBridge
function GetStat(id)
    return nil
end
