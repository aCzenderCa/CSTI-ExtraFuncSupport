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

---@class LuaInput
---@field GetScroll fun():number
---@field GetKey fun(key:string):boolean
---@field GetKeyDown fun(key:string):boolean
---@field GetKeyUp fun(key:string):boolean
LuaInput = {}

---@class LuaTimer
---@field ProcessCacheEnum fun():void
---@field Frame fun(function:fun():void):void
---@field FixFrame fun(function:fun():void):void
---@field FrameTime fun():number
---@field FixFrameTime fun():number
LuaTimer = {}

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

std__debug = debug

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

---@alias COCA_CardATagAreCorrectF fun(__instance:CardOnCardAction,_Receiving:CardAccessBridge,_Given:CardAccessBridge,result:boolean):boolean
---@alias CA_CardATagAreCorrectF fun(__instance:CardAction,_Receiving:CardAccessBridge,result:boolean):boolean
---@alias IGS_CValueF fun(__instance:GameStatAccessBridge,__instance_StatModel:SimpleUniqueAccess,result:number,_NotAtBase:boolean):number
---@alias IGCB_CanRecInInveF fun(__instance:CardAccessBridge,_Card:SimpleUniqueAccess,_WithLiquid:SimpleUniqueAccess):boolean
---@alias IGCB_CanRecInInveInsF fun(__instance:CardAccessBridge,_Card:CardAccessBridge):boolean
---@alias IGCB_InveWeightF fun(__instance:CardAccessBridge,result:number):number
---@alias IGCB_CNameF fun(__instance:CardAccessBridge,_IgnoreLiquid:boolean):string
---@alias IGCB_CDesF fun(__instance:CardAccessBridge,_IgnoreLiquid:boolean):string
---@alias IGCB_F_ALL IGCB_CanRecInInveF|IGCB_CanRecInInveInsF|IGCB_InveWeightF|IGCB_CNameF|IGCB_CDesF
---@alias GM_ChgSVal fun(__instance:GameManager,_Stat:GameStatAccessBridge,_Value:number,_Modification:number):void
---@alias GM_ChgSRate fun(__instance:GameManager,_Stat:GameStatAccessBridge,_Rate:number,_Modification:number):void
---@alias GM_F_ALL GM_ChgSVal|GM_ChgSRate
---@alias DAB_Setup fun(__instance:DismantleActionButton,_Action:DismantleCardAction,_Card:CardAccessBridge,_Action_ActionName_LocalizationKey:string):(boolean,boolean)

---@alias RegFunc COCA_CardATagAreCorrectF|CA_CardATagAreCorrectF|IGS_CValueF|IGCB_F_ALL|GM_F_ALL|DAB_Setup
---@alias RegClass "DismantleActionButton"|"GameManager"|"InGameCardBase"|"InGameStat"|"CardOnCardAction"|"CardAction"
---@alias IGCB_RegMethod "CanReceiveInInventory"|"CanReceiveInInventoryInstance"|"InventoryWeight"|"CardName"|"CardDescription"
---@alias RegMethod "CardsAndTagsAreCorrect"|"CurrentValue"|IGCB_RegMethod|"ChangeStatValue"|"ChangeStatRate"|"Setup"
---@class Register 
---@field Reg fun(this:Register, klass:RegClass, method:RegMethod, uid:string, function:RegFunc)
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
