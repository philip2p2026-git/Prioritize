using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Prioritize;

public static class PriorityPresetMenu
{
    public static IEnumerable<FloatMenuOption> Options(bool negative)
    {
        var settings = PrioritizeMod.Instance.Settings;

        for (var i = 1; i <= 5; i++)
        {
            var value = (short)(negative ? -i : i);
            yield return new FloatMenuOption(value.ToString(), delegate
            {
                if (negative)
                {
                    settings.NegativePreset = value;
                }
                else
                {
                    settings.PositivePreset = value;
                }

                MainMod.SelectedPriority = value;
                PrioritizeMod.Instance.WriteSettings();
            });
        }
    }
}
