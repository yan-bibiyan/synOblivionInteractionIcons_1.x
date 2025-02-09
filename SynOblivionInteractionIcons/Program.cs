using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Threading.Tasks;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Mutagen.Bethesda.Plugins;
using System.Xml.Linq;

namespace SynOblivionInteractionIcons
{
    public class Program
    {
        private static readonly ModKey KeyOblivIcon = ModKey.FromNameAndExtension("skymojibase.esl");
        private static Lazy<Settings>? Settings = null;

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetAutogeneratedSettings("settings", "settings.json", out Settings)
                .SetTypicalOpen(GameRelease.SkyrimSE, "OblivionInteractionIcons.esp").AddRunnabilityCheck(state =>
                {
                    state.LoadOrder.AssertHasMod(KeyOblivIcon, true, "\n\nskymojibase.esl missing!\n\n");
                })
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var settings = Settings?.Value;
            if (settings == null)
                throw new Exception("Settings could not be found!");

            ISkyrimModGetter? oblivionIconInteractor = state.LoadOrder.GetIfEnabled(KeyOblivIcon).Mod;
            if (oblivionIconInteractor == null)
                throw new Exception($"{KeyOblivIcon.Name} could not be found!");

            if (settings.PatchFlorae)
            {
                PatchFlorae(oblivionIconInteractor, state);
            }
            if (settings.PatchKnownActivators || settings.PatchUnknownActivators)
            {
                PatchActivators(oblivionIconInteractor, state, settings);
            }
        }

        private static void PatchFlorae(ISkyrimModGetter oblivionIconInteractor, IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            List<FormKey>? OIIFlora = oblivionIconInteractor.Florae.Select(x => x.FormKey).ToList();
            List<IFloraGetter>? winningFlora = state.LoadOrder.PriorityOrder.WinningOverrides<IFloraGetter>().Where(x => OIIFlora.Contains(x.FormKey)).ToList();

            Console.WriteLine("Patching Flora");
            foreach (var flora in state.LoadOrder.PriorityOrder.OnlyEnabled().Flora().WinningOverrides())
            {
                (string iconCharacter, string? iconColor) = GetIconInfo(flora);

                var floraPatch = state.PatchMod.Florae.GetOrAddAsOverride(flora);

                string newActivateTextOverride = "<font face=\"Iconographia\">" + iconCharacter + "</font>";
                floraPatch.ActivateTextOverride = iconColor == null ? newActivateTextOverride
                    : "<font color='" + iconColor + "'>" + newActivateTextOverride + "</font>";
            }

            foreach (var flora in oblivionIconInteractor.Florae)
            {
                var winningOverride = winningFlora.Where(x => x.FormKey == flora.FormKey).First();
                var PatchFlora = state.PatchMod.Florae.GetOrAddAsOverride(winningOverride);

                if (flora.ActivateTextOverride == null) continue;
                PatchFlora.ActivateTextOverride = flora.ActivateTextOverride.String;
            }
        }

        private static void PatchActivators(ISkyrimModGetter oblivionIconInteractor, IPatcherState<ISkyrimMod, ISkyrimModGetter> state, Settings settings)
        {
            Console.WriteLine("Patching Activators");

            List<FormKey>? OIIActivators = oblivionIconInteractor.Activators.Select(x => x.FormKey).ToList();
            List<IActivatorGetter>? winningActivator = state.LoadOrder.PriorityOrder.WinningOverrides<IActivatorGetter>().Where(x => OIIActivators.Contains(x.FormKey)).ToList();

            foreach (var activator in state.LoadOrder.PriorityOrder.OnlyEnabled().Activator().WinningOverrides())
            {
                (string? iconCharacter, string? iconColor, bool known) = GetIconInfo(activator);
                if (iconCharacter is null) continue;
                
                if ((known && settings.PatchKnownActivators) || (!known && settings.PatchUnknownActivators))
                {
                    var activatorPatch = state.PatchMod.Activators.GetOrAddAsOverride(activator);

                    string newActivateTextOverride = "<font face=\"Iconographia\">" + iconCharacter + "</font>";
                    activatorPatch.ActivateTextOverride = iconColor == null ? newActivateTextOverride
                        : "<font color='" + iconColor + "'>" + newActivateTextOverride + "</font>";
                }
            }

            foreach (var activator in oblivionIconInteractor.Activators)
            {
                var winningOverride = winningActivator.Where(x => x.FormKey == activator.FormKey).First();
                var activatorPatch = state.PatchMod.Activators.GetOrAddAsOverride(winningOverride);

                if (activator.ActivateTextOverride == null) continue;
                activatorPatch.ActivateTextOverride = activator.ActivateTextOverride.String;
            }
        }

        private static (string? iconCharacter, string? iconColor, bool known) GetIconInfo(IActivatorGetter activator)
        {
            var activateTextOverride = activator.ActivateTextOverride != null ? activator.ActivateTextOverride.String : null;
            var editorId = activator.EditorID != null ? activator.EditorID.ToString() : null;
            var name = activator.Name != null ? activator.Name.String : null;
            
            string iconCharacter;
            string? iconColor = null;
            bool known = true;

            //Blacklisting superfluos entries
            if (activator.ActivateTextOverride == null && editorId.ToUpperContainsAny("TRIGGER", "FX"))
            {
                return (null, null, false);
            }
            // Steal
            else if (activateTextOverride.ToUpperEquals("STEAL"))
            {
                iconColor = "ff0000";
                iconCharacter = "S";
            }
            // Pickpocket
            else if (activateTextOverride.ToUpperEquals("PICKPOCKET"))
            {
                iconColor = "ff0000";
                iconCharacter = "b";
            }
            // Steal From
            else if (activateTextOverride.ToUpperEquals("STEAL FROM"))
            {
                iconColor = "ff0000";
                iconCharacter = "V";
            }
            // Close
            else if (activateTextOverride.ToUpperEquals("CLOSE"))
            {
                iconColor = "dddddd";
                iconCharacter = "X";
            }
            // Chest | Search | Open Chest
            else if (name.ToUpperEquals("CHEST")
                    || activateTextOverride.ToUpperEquals("SEARCH")
                    || name.ToUpperContains("CHEST") && activateTextOverride.ToUpperEquals("OPEN"))
            {
                iconCharacter = "V";
            }
            // Grab & Touch
            else if (activateTextOverride.ToUpperEqualsAny("GRAB", "TOUCH"))
            {
                iconCharacter = "S";
            }
            // Levers
            else if (activator.Keywords != null && activator.Keywords.Contains(Skyrim.Keyword.ActivatorLever.FormKey)
                    || name.ToUpperContains("LEVER")
                    || editorId.ToUpperContains("PULLBAR"))
            {
                iconCharacter = "D";
            }
            // Chains
            else if (name.ToUpperContains("CHAIN"))
            {
                iconCharacter = "E";
            }
            // Mine
            else if (activateTextOverride.ToUpperEquals("MINE"))
            {
                iconCharacter = "G";
            }
            // Button | Press, Examine, Push, Investigate
            else if (name.ToUpperContains("BUTTON")
                    || activateTextOverride.ToUpperEqualsAny("PRESS", "EXAMINE", "PUSH", "INVESTIGATE"))
            {
                iconCharacter = "F";
            }
            // Business Ledger | Write
            else if (name.ToUpperContains("LEDGER")
                    || activateTextOverride.ToUpperEquals("WRITE"))
            {
                iconCharacter = "H";
            }
            // Pray
            else if (name.ToUpperContainsAny("SHRINE", "ALTAR")
                    || editorId.ToUpperContains("DLC2STANDINGSTONE")
                    || activateTextOverride.ToUpperEqualsAny("PRAY", "WORSHIP"))
            {
                iconCharacter = "C";
            }
            // Drink
            else if (activateTextOverride.ToUpperEquals("DRINK"))
            {
                iconCharacter = "J";
            }
            // Eat
            else if (activateTextOverride.ToUpperEquals("EAT"))
            {
                iconCharacter = "K";
            }
            // Drop, Place, Exchange
            else if (activateTextOverride.ToUpperEqualsAny("DROP", "PLACE", "EXCHANGE"))
            {
                iconCharacter = "N";
            }
            // Pick up
            else if (activateTextOverride.ToUpperEquals("PICK UP"))
            {
                iconCharacter = "O";
            }
            // Read
            else if (activateTextOverride.ToUpperEquals("READ"))
            {
                iconCharacter = "P";
            }
            // Harvest
            else if (activateTextOverride.ToUpperEquals("HARVEST"))
            {
                iconCharacter = "Q";
            }
            // Take or Catch
            else if (activateTextOverride.ToUpperEqualsAny("TAKE", "CATCH"))
            {
                iconCharacter = "S";
            }
            // Talk, Speak
            else if (activateTextOverride.ToUpperEqualsAny("TALK", "SPEAK"))
            {
                iconCharacter = "T";
            }
            // Sit
            else if (activateTextOverride.ToUpperEquals("SIT"))
            {
                iconCharacter = "U";
            }
            // Open (Door)
            else if (activateTextOverride.ToUpperEquals("OPEN"))
            {
                iconCharacter = "X";
            }
            // Activate
            else if (activateTextOverride.ToUpperEquals("ACTIVATE"))
            {
                iconCharacter = "Y";
            }
            // Unlock
            else if (activateTextOverride.ToUpperEquals("UNLOCK"))
            {
                iconCharacter = "Z";
            }
            // Sleep
            else if (activateTextOverride.ToUpperEquals("SLEEP")
                    || name.ToUpperContainsAny("BED", "HAMMOCK", "COFFIN"))
            {
                iconCharacter = "a";
            }
            // Torch
            else if (editorId.ToUpperContains("TORCHSCONCE"))
            {
                iconCharacter = "i";
            }
            // Dragon Claw
            else if (name.ToUpperContains("KEYHOLE"))
            {
                iconCharacter = "j";
            }
            // Civil War Map & Map Marker (Flags)
            else if (editorId.ToUpperContains("CWMAP"))
            {
                iconCharacter = "F";
            }
            // EVG Ladder | Float, Climb
            else if (editorId.ToUpperContains("LADDER")
                    || activateTextOverride.ToUpperEqualsAny("FLOAT", "CLIMB"))
            {
                iconCharacter = "d";
            }
            // EVG Squeeze
            else if (editorId.ToUpperContains("SQUEEZE"))
            {
                iconCharacter = "e";
            }
            // CC Fishing
            else if (name.ToUpperContains("FISHING SUPPLIES"))
            {
                iconCharacter = "I";
            }
            else
            {
                iconCharacter = "W";
                known = false;
            }

            return (iconCharacter, iconColor, known);
        }

        private static (string iconCharacter, string? iconColor) GetIconInfo(IFloraGetter flora)
        {
            string iconCharacter = "Q"; // Default
            string? iconColor = null;
            
            var name = flora.Name != null ? flora.Name.String : null;
            var activateTextOverride = flora.ActivateTextOverride != null ? flora.ActivateTextOverride.String : null;

            // Mushrooms
            if (flora.HarvestSound.FormKey.Equals(Skyrim.SoundDescriptor.ITMIngredientMushroomUp.FormKey)
                || name.ToUpperContainsAny("SPORE", "CAP", "CROWN", "SHROOM"))
            {
                iconCharacter = "A";
            }
            // Clams
            else if (flora.HarvestSound.FormKey.Equals(Skyrim.SoundDescriptor.ITMIngredientClamUp.FormKey)
                    || name.ToUpperContains("CLAM"))
            {
                iconCharacter = "b";
            }
            // Fill | Cask or Barrel (Fill)
            else if (flora.HarvestSound.FormKey.Equals(Skyrim.SoundDescriptor.ITMPotionUpSD.FormKey)
                    || activateTextOverride.ToUpperContains("FILL BOTTLES")
                    || name.ToUpperContainsAny("BARREL", "CASK"))
            {
                iconCharacter = "L";
            }
            // Coin Pouch
            else if (flora.HarvestSound.FormKey.Equals(Skyrim.SoundDescriptor.ITMCoinPouchUp.FormKey)
                    || flora.HarvestSound.FormKey.Equals(Skyrim.SoundDescriptor.ITMCoinPouchDown.FormKey)
                    || name.ToUpperContains("COIN PURSE"))
            {
                iconCharacter = "S";
            }
            // Catch, Scavenge
            else if (activateTextOverride.ToUpperEqualsAny("CATCH", "SCAVENGE"))
            {
                iconCharacter = "S";
            }
            // Other Flora
            else
            {
                iconCharacter = "Q";
            }

            return (iconCharacter, iconColor);
        }
    }
}
