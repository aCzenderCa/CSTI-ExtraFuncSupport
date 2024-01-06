---@class LuaAnim
---@field CurMouse fun():ITransProvider
---@field GenCFXR fun(transProvider:ITransProvider,fxName:fxName|string,moveWithProvider?:boolean,time?:number,ext?:table):void
LuaAnim = {}

---@alias fxTextName "CFXR _BOING_"|"CFXR _BOOM_"|"CFXR _POW_"|"CFXR _SLASH_"|"CFXR2 _CURSED_"|"CFXR2 _WHAM_ 3"|"CFXR4 _FROZEN_"|"CFXR4 _POISONED_"
---@alias fxCFXR1Name_1 "CFXR Electrified 3"|"CFXR Explosion 1"|"CFXR Explosion Smoke 2 Solo (HDR)"|"CFXR Fire Breath"|"CFXR Fire"|"CFXR Flash"|"CFXR Hit A (Red)"
---@alias fxCFXR1Name_2 "CFXR Hit D 3D (Yellow)"|"CFXR Impact Glowing HDR (Blue)"|"CFXR Magic Poof"|"CFXR Smoke Source 3D"|"CFXR Water Ripples"|"CFXR Water Splash (Smaller)"
---@alias fxCFXR1Name fxCFXR1Name_1|fxCFXR1Name_2
---@alias fxCFXR2Name_1 "CFXR2 Blood (Directional)"|"CFXR2 Blood Shape Splash"|"CFXR2 Broken Heart"|"CFXR2 Broken Heart"|"CFXR2 Firewall A"|"CFXR2 Ground Hit"
---@alias fxCFXR2Name_2 "CFXR2 Poison Cloud"|"CFXR2 Shiny Item (Loop)"|"CFXR2 Skull Head Alt"|"CFXR2 Souls Escape"|"CFXR2 Sparks Rain"
---@alias fxCFXR2Name_3 "CFXR2 WW Explosion"|"CFXR2 WW Enemy Explosion"
---@alias fxCFXR2Name fxCFXR2Name_1|fxCFXR2Name_2|fxCFXR2Name_3
---@alias fxCFXR3Name_1 "CFXR3 Ambient Glows"|"CFXR3 Fire Explosion B"|"CFXR3 Hit Electric C (Air)"|"CFXR3 Hit Fire B (Air)"|"CFXR3 Hit Ice B (Air)"
---@alias fxCFXR3Name_2 "CFXR3 Hit Leaves A (Lit)"|"CFXR3 Hit Light B (Air)"|"CFXR3 Hit Misc A"|"CFXR3 Hit Misc F Smoke"|"CFXR3 LightGlow A (Loop)"
---@alias fxCFXR3Name_3 "CFXR3 Magic Aura A (Runic)"|"CFXR3 Shield Leaves A (Lit)"
---@alias fxCFXR3Name fxCFXR3Name_1|fxCFXR3Name_2|fxCFXR3Name_3
---@alias fxCFXR4Name_1 "CFXR4 Bouncing Glows Bubble (Blue Purple)"|"CFXR4 Bubbles Breath Underwater Loop"|"CFXR4 Falling Stars"|"CFXR4 Firework 1 Cyan-Purple (HDR)"
---@alias fxCFXR4Name_2 "CFXR4 Firework HDR Shoot Single (Random Color)"|"CFXR4 Sun"|"CFXR4 Sword Hit FIRE (Cross)"|"CFXR4 Sword Hit ICE (Cross)"|"CFXR4 Sword Hit PLAIN (Cross)"
---@alias fxCFXR4Name_3 "CFXR4 Sword Trail FIRE (360 Spiral)"|"CFXR4 Sword Trail FIRE (360 Thin Spiral)"|"CFXR4 Sword Trail ICE (360 Spiral)"|"CFXR4 Sword Trail ICE (360 Thin Spiral)"
---@alias fxCFXR4Name_4 "CFXR4 Sword Trail PLAIN (360 Spiral)"|"CFXR4 Sword Trail PLAIN (360 Thin Spiral)"|"CFXR4 Wind Trails"
---@alias fxCFXR4Name fxCFXR4Name_1|fxCFXR4Name_2|fxCFXR4Name_3|fxCFXR4Name_4
---@alias fxName fxTextName|fxCFXR1Name|fxCFXR2Name|fxCFXR3Name|fxCFXR4Name