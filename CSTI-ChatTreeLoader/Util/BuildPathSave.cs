using ChatTreeLoader.Patchers;
using HarmonyLib;

namespace ChatTreeLoader.Util
{
    public static class BuildPathSave
    {
        public static string SavePath(this Encounter encounter)
        {
            var encounterUniqueID = encounter.UniqueID;

            if (!ModEncounter.ModEncounters.ContainsKey(encounterUniqueID))
            {
                return null;
            }

            var curPath = MainPatcher.CurPaths[encounterUniqueID];
            return $"__{{{encounterUniqueID}}}ModEncounter.Infos__{{EncounterPath:{curPath.Join(null, ".")}}}";
        }
    }
}