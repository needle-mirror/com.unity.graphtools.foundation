using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Obsolete("2021-01-05 BaseAction was renamed to Command (UnityUpgradable) -> Command")]
    public abstract class BaseAction {}

    public abstract class Command
    {
        public string UndoString { get; set; }
    }
}
