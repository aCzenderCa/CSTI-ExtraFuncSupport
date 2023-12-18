# 本项目为游戏[《卡牌生存：热带岛屿》](https://store.steampowered.com/app/1694420/)的功能扩展mod，使modder可以使用lua代码制作模组，提供了额外的自定义数据存储

# LuaActionSupport(卡牌action的lua支持)

## 用法

#### 在ActionName的ParentObjectID里写lua代码

#### 如果是CardAction，ActionName的LocalizationKey以LuaCardAction开头启用lua支持

#### 如果是CardOnCardAction，ActionName的LocalizationKey以LuaCardOnCardAction开头启用lua支持

## 术语说明

### 额外参数

位于lua api函数最后，参数名为ext，参数类型为LuaTable的可选参数，用于传递使用较少，数量较多的一些参数

# LuaAction简单示例

改stat

````lua
SimpleAccessTool[对应stat的uid].StatValue = SimpleAccessTool[对应stat的uid].StatValue * 2 
SimpleAccessTool[对应stat的uid].StatRate = SimpleAccessTool[对应stat的uid].StatRate * 2
````

生成卡或Encounter

````lua
SimpleAccessTool[卡id或encounter的id].Gen(生成次数,不填生成一次)
````

## SimpleAccessTool快速工具

索引器 参数类型 ``string`` 传入uid
返回``SimpleUniqueAccess``
* 成员函数
  * void ClearCurrentEnv() 清空并重置当前场景

## SimpleUniqueAccess——UniqueIDScriptable快速操作接口

### 方法Gen ：

* 参数``count`` 生成次数

  * ``UniqueIDScriptable``为``CardData``时生成count张对应卡
  * ``UniqueIDScriptable``为``Encounter``时生成一次Encounter
* 额外参数：同``CardAccessBridge``的``AddCard``

### 方法`void CacheRawValRange(float x, float y)`

激活`MinMaxValue`的缓存机制，传入x,y被视为原始的`MinMaxValue.x,y`

### 方法`void CacheRawRateRange(float x, float y)`

激活`MinMaxRate`的缓存机制，传入x,y被视为原始的`MinMaxRate.x,y`

#### 示例

```lua
local statX = SimpleAccessTool[x]
statX:CacheRawValRange(statX.StatValueMin,statX.StatValueMax)--需要修改状态值的范围
statX:CacheRawRateRange(statX.StatRateMin,statX.StatRateMax)--需要修改状态速率的范围
```

### 索引器：传入字符串key，获取对应字段的值或者修改对应字段

获取值时返回``SimpleObjAccess``或``SimpleUniqueAccess``（如果获取的是UniqueIDScriptable），访问``AccessObj``可得到实际值

#### 属性``CardDescription`` 可读写字符串属性，修改卡牌描述

#### 属性``StatValue``和``StatRate``: 若对应``UniqueIDScriptable``为gamestat，获取或修改状态的值

##### 子属性~Min，~Max，浮点数，修改对应的上下限

#### 属性AccessObj，获取正在访问的对象

## SimpleObjAccess

索引器：同``SimpleUniqueAccess``
属性``AccessObj``，获取正在访问的对象

## 注意

### 临时保存数据用全局表中的``ModData``表

## ``Debug``（调试）

全局变量``debug``
``info``，``debug``，``warn``，``error``
``debug``全局变量的这几个属性赋值会自动用``UnityEngine.Debug``类输出赋值的值
读取这几个字段会导致报错

## ``CardAccessBridge``（卡牌访问桥）

全局变量``env``
当前Environment卡（与左上角区分，该卡游戏中不可见）
全局变量``exp``
当前Explorable卡（左上角那张卡）
全局变量``weather``
当前天气卡

#### 在``CardAction``中

全局变量``receive``
接受``CardAction``的卡

#### 在``CardOnCardAction``中

全局变量``receive``
接受``CardOnCardAction``的卡
全局变量``given``
提供给``receive``的卡

## ``CardAccessBridge``的属性

* ``CardType``：卡牌类型，是字符串，只读
* ``Weight``：卡的总重，是单浮点数，只读
* ``Id``：卡的uid，是字符串，只读
* `Data`:`DataNodeTableAccessBridge`类型，与卡绑定的数据，不存在且未初始化时返回null
* InventorySlotCount:返回容器中的槽位数量
* IsEquipped 是否已被装备
* IsInHand 是否在手卡栏位（从上到下第三行）
* IsInBase 是否在地面栏位（从上到下第二行）
* IsInLocation 是否在地点栏位（从上到下第一行）
* IsInBackground 是否是来自其他场景的卡（通过AlwaysUpdate=true）
* `CardModel`:`SimpleUniqueAccess`类型

```
Spoilage，Usage，Fuel，Progress，Special1，Special2，Special3，Special4
```

八个耐久度，是浮点数，可读可写
``LiquidQuantity``L流体容器所含流体的数量，300==一碗，可读可写

## ``CardAccessBridge``的函数

### 索引器

* 参数 ``int index``

  * 访问的卡为容器时，获取容器内对应槽位的卡 类型``CardAccessBridge``的列表
* 参数 ``string key``:从卡的``DroppedCollections``（存档数据）读写内容

  * 设置的值直接为整数时，直接以``key``保存整数值
  * 其他情况下以``DroppedCollections[key]``为``$"zender.luaSupportData.{key}:{value}"``值为（1，1）保存

### `AddAnim(table<string>? animList, table<number>? animTimeList)`
添加一组动画，animList是各图片的name，animTimeList是各图片的持续时间（单位秒）\
如果输入的animList与animTimeList为null，则效果变为每间隔0.02秒更新一次卡图（50fps）
```lua
local anim = {}
local animTime = {}
for i = 1, 300 do
  anim[i] = "output_" .. i .. "_testonly"
  animTime[i] = 0.04
end
receive:AddAnim(anim,animTime);
```

### `RemoveAnim`
移除卡牌上的动画

### `InitData`:
初始化与卡绑定的数据，即Data属性

### `SaveData`:

保存与卡绑定的数据

#### Data操作示例：

```lua
receive:InitData()
local d = receive.Data
if(d["i"]==nil)then
  d["i"] = 1
else
  d["i"] = d["i"] + 1
end
receive:SaveData()
```

### `UpdateVisuals`:无参数无返回，更新该卡的卡图等

### ``CheckInventory``：

* 参数：``bool useAll, params string[] uid``
* 返回值：``bool``
  * 返回卡牌是否装有uid数组中所有对应卡牌（useAll为true）|任一对应卡牌（useAll为false）
  * 用法示例
```lua
receive:CheckInventory(true, { 'a', 'b' })
receive:CheckInventory(true, 'a', 'b')
```

### ``CheckTagInventory``：

* 参数：``bool useAll, params string[] tags``
* 返回值：``bool``
  * 返回卡牌是否装有tags数组中所有对应卡牌（useAll为true）|任一对应卡牌（useAll为false）

### ``CheckRegexTagInventory``：

* 参数：``bool useAll, params string[] regexTags``
* 返回值：``bool``
  * 返回卡牌是否装有regexTags数组中所有对应卡牌（useAll为true）|任一对应卡牌（useAll为false）

### ``HasInInventory``：

* 参数：``string uid, long needCount = 0``
* 返回值：``bool``
  * 返回卡牌是否装有至少指定数量的uid所对应的卡牌

### ``HasTagInInventory``：

* 参数：``string tag, long needCount = 0``
* 返回值：``bool``
  * 返回卡牌是否装有至少指定数量的有对应tag的卡牌

### ``HasRegexTagInInventory``：

* 参数：``string regexTag, long needCount = 0``
* 返回值：``bool``
  * 返回卡牌是否装有至少指定数量的有对应regexTag的卡牌

### ``HasTag``：

* 参数：``string tag``
* 返回值：``bool``
  * 返回卡牌是否包含名称为参数tag的``CardTag``

### ``HasRegexTag``：

* 参数：``string regexTag``
* 返回值：``bool``
  * 返回卡牌是否包含名称与regexTag正则匹配的``CardTag``

### ``AddCard``：

* 参数：``string id，int amount=1`` 无返回值
* 以本卡为基础生成``UniqueId``为id的卡牌，若id对应卡牌为液体，``amout``代表流体量，如果Card本身可以装载该液体，液体会被自动放入容器里，否则``amout``代表生成次数，如果Card本身是容器，那么生成的卡牌会自动放进容器里面，否则放到容器同一栏位中
* 额外参数：
  * TransferedDurabilities部分:`float`类型：

```
"Usage"
"Fuel"
"Spoilage"
"ConsumableCharges"
"Liquid"
"Special1"
"Special2"
"Special3"
"Special4"
```

对应同名字段的值

``"LiquidCard"``：``SimpleUniqueAccess``类型，伴随生成什么流体
  * `"initData"`:`DataNodeTableAccessBridge`类型，初始化携带的lua-nbt数据

### `Remove`：

* 参数：``bool doDrop``
* 无返回值
  * 删除所访问的卡，``doDrop``为``true``时会掉落容器内物品（还有其他用处）
  * 如果是容器内的卡，会同时自动把自己从容器里移除

## 类型`DataNodeTableAccessBridge`:

* 索引器：`string key`
  * 返回值为DataNode支持的类型：
    * Number -> double
    * Str
    * Bool
    * Table -> DataNodeTableAccessBridge
    * Nil
    * Vector2

* 属性
  * LuaTable LuaKeys
    * 以LuaTable返回字典的键
  * LuaTable LuaTable（属性类型和属性名都是LuaTable）
    * 将保存的数据以LuaTable返回

## ``GameStatAccessBridge``（游戏状态访问桥）

### 属性

* ``Value``：读取时返回状态当前值（考虑modifier），赋值时修改基础值而非modifier值
* ``Rate``：读取时返回状态当前速率（考虑modifier），赋值时修改基础速率而非modifier值

## 全局变量

### ``gameManager``（``GameManager.Instance``）

### ``Ret`` 用于返回值

* 在动态卡牌描述中，``Ret[“ret”]``为要将描述改为的内容,不填就不变
* 在lua卡牌action中,``Ret[“result”]``为lua代码执行后要额外等待多少tp,不填就不等待
  * `Ret["miniTime"]`为lua代码执行后要等待多少mini tp
  * `Ret["tickTime"]`为lua代码执行后要等待多少tick tp（10tick tp = 1 mini tp）

## 持久化存储辅助内容

使用Load*函数加载表时返回``DataNodeTableAccessBridge``（C#表访问桥）
``DataNodeTableAccessBridge``有一个索引器：传入字符串的``key``，读写持久化数据
修改与持久化数据同步

## 全局函数

* ``SaveCurrentSlot`` ：参数字符串 `string key`，（数字或字符串） val  将key，val保存到与当前存档槽绑定的数据表
* ``SaveGlobal`` ：参数字符串 `string key`，（数字或字符串） val 将key，val保存到全局数据表
* ``LoadCurrentSlot`` ：参数字符串`string key`  返回 数字或字符串 val 从与当前存档槽绑定的数据表中读取key对应的val
* ``LoadGlobal`` ：参数字符串`string key` 返回 数字或字符串 val 从全局数据表读取key对应的val
* `GetCard`：参数 `string id`，返回值`CardData`
  * 返回传入id对应卡的`CardData`
* `GetGameCard`：返回找到的第一个id对应卡牌，是`CardAccessBridge`
* `GetGameCardByTag`:返回找到的第一个tag对应卡牌，是`CardAccessBridge`
* `GetGameCards`：返回找到的id对应的所有卡牌，是`CardAccessBridge`的列表
  * 额外参数：
    * `ext["type"]`：字符串，以下为内容与效果对应
      * `"Equipment"`：只返回装备的卡
      * `"Hand"`：只返回从上到下第三行的卡
      * `"Base"`：只返回从上到下第二行的卡
      * `"Location"`：只返回从上到下第一行的卡
      * `"Inventory"`：只返回容器中的卡
* `GetGameCardsByTag`:返回找到的tag对应的所有卡牌，是`CardAccessBridge`的列表
* `GetStat`：返回id对应`GameStat`
* `GetGameStat`：返回id对应`GameStatAccessBridge`
* `CountCardOnBoard`：`string id, bool _CountInInventories = true, bool _CountInBackground = false`
  * `_CountInInventories`:是否统计容器内的卡
  * `_CountInBackground`:是否统计在背景中的卡
  * 返回id对应卡的数量
* `CountCardInBase`：`string id, bool _CountInInventories = true`
  * `_CountInInventories`:是否统计容器内的卡
  * 返回id对应卡在base上的数量
* `CountCardInHand`：`string id, bool _CountInInventories = true`
  * `_CountInInventories`:是否统计容器内的卡
  * 返回id对应卡在手上的数量
* `CountCardInLocation`：`string id, bool _CountInInventories = true`
  * `_CountInInventories`:是否统计容器内的卡
  * 返回id对应卡在location上的数量
* `CountCardEquipped`：`string id`
  * 只统计装备的卡，返回id对应卡数量

## LuaSystem
全局变量`LuaSystem`:lua table类型(不需要用`：`调用)\
主要用于生命周期回调（如某个MonoBehaviour被创建，在某个MonoBehaviour上附加update）

* 函数
  * `AddSystem`:输入`string type, string sys_type, string uid, LuaFunction function`

* 接受的`string type, string sys_type, string uid, LuaFunction function`组合
  * `"InGameCardBase","OnUpdate",CardModel.UniqueID,fun(card:CardAccessBridge):void`
    * 卡牌存在时每帧调用一次
  * `"InGameCardBase","PostInit",CardModel.UniqueID,fun(card:CardAccessBridge):void`
    * 卡牌初始化完成时调用一次

## LuaTimer
全局变量`LuaTimer`:lua table类型(不需要用`：`调用)

* 函数
  * `ProcessCacheEnum`:无参数，无返回值，手动执行当前累计要执行的延时函数（如生成卡，状态值修改）
  * `Frame`：传入一个lua函数变量（要求无输入参数，可返回一个bool），输入的函数会每一帧执行一次，若返回false，则移除该函数
  * `FixFrame`：传入一个lua函数（要求同上），输入的函数会每秒固定执行50次（频率是尽可能均匀的）
  * `EveryTime`:传入一个lua函数（要求同上）和一个number参数time，输入的函数每time秒会执行一次
  * `FrameTime`：返回两帧之间的时间间隔
  * `FixFrameTime`：返回两次FixFrame之间的时间间隔
  * `Rand`：返回一个[0,1]的随机数
  * `StartCoroutine`：传入一个lua函数（无输入，返回float），启动一个协程，如果lua函数返回(-3,-2]，则等待所有延迟函数执行完成（如生成卡，修改状态值）期间阻止玩家操作，如果lua函数返回(-2,-1]，则等待一个物理帧，如果lua函数返回(-1,0]，则等待一帧，否则等待返回值秒，若不返回则结束协程

## LuaInput
全局变量`LuaInput`:lua table类型(不需要用`：`调用)

键鼠输入相关

* 函数
  * `GetScroll`:无参数，返回两帧之间滚轮的旋转量
  * `GetKey`：输入按键的名字（如F，L，F1），返回对应按键是否正被按下
  * `GetKeyDown`：输入按键的名字（如F，L，F1），返回对应按键是否刚被按下
  * `GetKeyUp`：输入按键的名字（如F，L，F1），返回对应按键是否刚刚弹起
  * [按键名称表](https://docs.unity.cn/cn/2020.3/ScriptReference/KeyCode.html)

## LuaGraphics
全局变量`LuaGraphics`:`LuaTable`类型（不需要`:`调用）

图像UI等相关内容

* 函数
  * UpdateCards：无参数无返回值，更新所有卡槽上的图片
  * UpdatePopup：更新所有卡牌界面的标题和介绍

## LuaRegister

全局变量`Register`：`LuaRegister`类型(需要用`：`调用)

函数`Reg`：`string klass, string method, string uid, LuaFunction function`\
`klass`：要patch的类的名字，如`"InGameCardBase"`\
`method`：要patch的函数的名字，如`"CanReceiveInInventory"`\
`uid`：如果patch的函数基于`UniqueIDScriptable`类型对象执行，那么只有`uid`匹配，注册的func才会被用到，如果patch的函数
基于`CardAction`（及其子类）类型对象执行，那么`uid`需要与`ActionName.LocalizationKey`匹配

* `InGameCardBase`：
  * `CurrentImage`
    * `CurrentImage_Getter`:获取槽位上的卡图
      * `LuaFunction`要求：输入`CardGraphics __instance, CardAccessBridge CardLogic, SimpleUniqueAccess CardModel, string name`
        * __instance卡槽本体，CardLogic卡槽上第一张卡，CardModel卡槽上卡的model，name原本图片的名字
      * 返回string：若返回，且ModLoader加载了名称与返回值匹配的图片，则将卡图修改为该图片
      * 示例：
```lua
Register:Reg('InGameCardBase', 'CurrentImage_Getter', 'id', function(cg,card,model,sp_name)
  return 'spirte_name'
end)
```
  * `CanReceiveInInventory`：返回是否可以装入某个卡
    * `LuaFunction`要求：输入`CardAccessBridge this,SimpleUniqueAccess card,SimpleUniqueAccess liquid` 返回`bool`或`nil`，`bool`则修改函数结果，`nil`函数结果不变
    * `this`为执行`CanReceiveInInventory`本身的卡，`card`为要装入的物品的`CardData`，`liquid`为装入流体容器时，流体的`CardData`
  * `CanReceiveInInventoryInstance`：
    * `LuaFunction`要求：输入`CardAccessBridge this，InGameCardBase Card` 返回`bool`或`nil`
    * `Card`为要装入的卡的实例
  * `InventoryWeight`：返回重量
    * `LuaFunction`要求：输入`CardAccessBridge this，float __result` 返回`number`或`nil`，为`nil`则不修改
    * `__result`为当前计算得的重量，
  * `CardName`：卡牌名称  `CardDescription`：卡牌描述
    * `LuaFunction`要求：输入`CardAccessBridge this，bool _IgnoreLiquid`返回字符串或nil
    * `ignoreLiquid`参数由游戏本身传入

* `InspectionPopup`
  * `Setup`
    * `Setup_ModBG`:在popup初始化时修改背景，只有打开与Reg时uid匹配的卡时会执行
      * `LuaFunction`要求：输入`InspectionPopup this, CardAccessBridge _Card`,返回`(string bg_fg, string bg_bg)`,可以少返回，不返回的项不会被修改，bg_fg是popup背景中的前景部分（白色那块），bg_bg是popup背景中的背景部分（白色那块后面的玩意）
      * 示例:
```lua
Register:Reg('InspectionPopup', 'Setup_ModBG', 'id', function(popup,card)
  local mod_bg = LuaTimer.Rand() > 0.5
  if mod_bg then
    return 'name_of_fg', 'name_of_bg'
  else
    return 'name_of_fg'
  end
end)
```

* `DismantleActionButton`：

  * `Setup`：卡牌的按钮初始化函数
    * `LuaFunction`要求：`输入DismantleActionButton this，DismantleCardAction _Action，CardAccessBridge _Card，string _Action.ActionName.LocalizationKey 返回两个bool`
    * 第一个bool返回值决定该按钮是否可见，第二个bool返回值决定该按钮是否可以按下
    * `_Action`：用来初始化按钮的`DismantleCardAction`数据，`_Card`要初始化按钮的卡牌实例，`_Action.ActionName.LocalizationKey`，用于区分同个卡牌的不同按钮，action名的翻译key
* `GameManager`：

  * `ChangeStatValue`：修改状态值的函数
    * `LuaFunction`要求：输入`GameManager this，GameStatAccessBridge _Stat，float _Value，StatModification _Modification` 无返回值
    * `_Stat`：被修改的状态，`_Value`：往状态上加上多少，`_Modification`：以什么方式修改，枚举类型，大概会变成数字
    * 可以在这里进行状态修改，生成卡等非即时操作
  * `ChangeStatRate`：修改状态速率的函数
    * `LuaFunction`要求：输入`GameManager this，GameStatAccessBridge _Stat，float _Rate，StatModification _Modification` 无返回值

* `InGameStat`
  * `CurrentValue`:返回状态当前数值（修正+基础值+考虑上下限）
    * `LuaFunction`要求：输入`GameStatAccessBridge __instance, SimpleUniqueAccess __instance.StatModel, float __result, bool _NotAtBase`
      * `__result`:C#代码计算得的结果或按顺序上一个Lua Patch返回的结果
    * 返回float:若返回，将__result改为返回值

* `CardOnCardAction`
  * `CardsAndTagsAreCorrect`:某个Given Card是否满足交互条件
    * `LuaFunction`要求：输入`CardOnCardAction __instance, CardAccessBridge _Receiving, CardAccessBridge _Given, bool __result`
      * `__result`:C#代码计算得的条件结果或按顺序上一个Lua Patch返回的结果
    * 返回bool:若返回，将__result改为返回值

* `CardAction`
  * `CardsAndTagsAreCorrect`:某个Receiving Card是否满足交互条件
    * `LuaFunction`要求：输入`CardAction __instance, CardAccessBridge _ForCard, bool __result`
      * `__result`:C#代码计算得的条件结果或按顺序上一个Lua Patch返回的结果
    * 返回bool:若返回，将__result改为返回值



## LuaCodeCardDescription

（1）若卡牌的描述本该显示为如下（包括前后两行）\
（2）若DismantleActionButton按钮上文本本应显示为如下

```
###luaAction CardDescription
Ret[“ret”] = "test"
###
```

（1）则卡牌的描述会显示为`test`

（2）则DismantleActionButton按钮上文本显示为`test`(额外：设置Ret["show"]以决定按钮是否显示\
设置Ret["canUse"]以决定按钮是否可用)

在
`###luaAction CardDescription`
和
`###`
之间的内容会作为lua代码执行，其第一个返回值就是要显示的内容\
全局表中的`receive`是对应的卡实体

* 仅DismantleActionButton:
  * ModData表内
    * Args__instance:DismantleActionButton实例
    * Args__Index:按钮在card的按钮列表内的位置
    * Args__Action:DismantleCardAction _Action
    * Args__Highlighted:bool Setup的参数
    * Args__StackVersion:bool Setup的参数