using MonoMod.ModInterop;
using System;

namespace MadelineParty {
    [ModImportName("AchievementHelper")]
    public static class AchievementHelperImports {
        public static Action<string, string> TriggerAchievement;
    }
}
