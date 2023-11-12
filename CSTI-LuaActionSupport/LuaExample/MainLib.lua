---@class Enum
---@field public Foreach fun(this:Enum, enumerable:IEnumerable, func:fun(x:any):void):void
---@field public Sum fun(this:Enum, enumerable:IEnumerable, init:number, func:fun(sum:number, x:any):number):number
---@field public Map fun(this:Enum, enumerable:IEnumerable, func:fun(x:any):any):IList
EnumCls = {}
Enum = EnumCls()

---@class IEnumerable
IEnumerable = {}

---@class DataNode
---@field NodeType number
---@field number number
---@field str string
---@field _bool boolean
---@field vector2 Vector2
---@field table Dictionary<string, DataNode>
DataNode = {}

---@class Vector2
---@field x number
---@field y number
Vector2 = {}

---@class Register
---@field public Reg fun(this:Register, klass:string, method:string, uid:string, function:function)
RegisterCls = {}
Register = RegisterCls()

---@class SimpleAccessTool
---@field public __index fun(key:string):SimpleUniqueAccess
---@field public ClearCurrentEnv fun():void
SimpleAccessToolCls = {}
SimpleAccessTool = SimpleAccessToolCls()

---@param key string
function SaveCurrentSlot(key, val)
end

---@param key string
function SaveGlobal(key, val)
end

---@param key string
---@return any
function LoadCurrentSlot(key)
end

---@param key string
---@return any
function LoadGlobal(key)
end

---@param id string
---@return CardData
function GetCard(id)
end

---@param id string
---@return CardAccessBridge
function GetGameCard(id)
end

---@param tag string
---@return CardAccessBridge
function GetGameCardByTag(tag)
end

---@param id string
---@param ext table
---@return List<CardAccessBridge>
function GetGameCards(id, ext)
end

---@param tag string
---@return List<CardAccessBridge>
function GetGameCardsByTag(tag)
end

---@param id string
---@param _CountInInventories boolean
---@param _CountInBackground boolean
---@return number
function CountCardOnBoard(id, _CountInInventories, _CountInBackground)
end

---@param id string
---@param _CountInInventories boolean
---@return number
function CountCardInBase(id, _CountInInventories)
end

---@param id string
---@return number
function CountCardEquipped(id)
end

---@param id string
---@param _CountInInventories boolean
---@return number
function CountCardInHand(id, _CountInInventories)
end

---@param id string
---@param _CountInInventories boolean
---@return number
function CountCardInLocation(id, _CountInInventories)
end

---@param id string
---@return GameStat
function GetStat(id)
end

---@param id string
---@return GameStatAccessBridge
function GetStat(id)
end
