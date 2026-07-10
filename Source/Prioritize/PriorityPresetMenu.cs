using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Prioritize;

public static class PriorityPresetMenu
{
    public static void Show(bool negative)
    {
        var settings = PrioritizeMod.Instance.Settings;
        var options = new List<FloatMenuOption>();

        for (var i = 1; i <= 5; i++)
        {
            var value = (short)(negative ? -i : i);
            options.Add(new FloatMenuOption(value.ToString(), delegate
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
            }));
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }
}
