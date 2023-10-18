using System;
using UnityEngine;
using UnityEngine.UI;

namespace CSTI_LuaActionSupport.UIStruct;

[RequireComponent(typeof(Canvas))]
public class CustomUIManager : MonoBehaviour
{
    public Canvas CustomUICanvas = null!;
}