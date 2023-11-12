---@alias DataNodeData number|string|boolean|DataNodeTableAccessBridge|nil|Vector2
---@class Enum
---@field public Foreach fun<TItem>(this:Enum, enumerable:IEnumerable<TItem>, func:fun(x:TItem):void):void
---@field public Sum fun<TItem>(this:Enum, enumerable:IEnumerable<TItem>, init:number, func:fun(sum:number, x:TItem):number):number
---@field public Map fun<TItem, TInto>(this:Enum, enumerable:IEnumerable<TItem>, func:fun(x:TItem):TInto):IList<TInto>
Enum = {}

---@class IEnumerable<TItem>
---@field GetEnumerator fun(this:IEnumerable<TItem>):IEnumerator<TItem>
IEnumerable = {}

---@class IEnumerator<TItem>
---@field MoveNext fun(this:IEnumerator<TItem>):boolean
---@field Current TItem
IEnumerator = {}

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

---@param id string
---@param ext? table
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
