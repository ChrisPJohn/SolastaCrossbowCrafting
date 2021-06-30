using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityModManagerNet;
using SolastaModApi;
using ModKit;
using ModKit.Utility;
using System.Collections.Generic;
using SolastaModApi.Extensions;

namespace SolastaCrossbowCrafting
{
    public static class Main
    {
        public static readonly string MOD_FOLDER = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static Guid ModGuidNamespace = new Guid("6eff8e23-1b2f-4e48-8cde-3abda9d4bc3b");

        [Conditional("DEBUG")]
        internal static void Log(string msg) => Logger.Log(msg);
        internal static void Error(Exception ex) => Logger?.Error(ex.ToString());
        internal static void Error(string msg) => Logger?.Error(msg);
        internal static void Warning(string msg) => Logger?.Warning(msg);
        internal static UnityModManager.ModEntry.ModLogger Logger { get; private set; }
        internal static ModManager<Core, Settings> Mod { get; private set; }
        internal static MenuManager Menu { get; private set; }
        internal static Settings Settings { get { return Mod.Settings; } }

        internal static bool Load(UnityModManager.ModEntry modEntry)
        {
            try
            {
                Logger = modEntry.Logger;

                Mod = new ModManager<Core, Settings>();
                Menu = new MenuManager();
                modEntry.OnToggle = OnToggle;

                Translations.Load(MOD_FOLDER);
            }
            catch (Exception ex)
            {
                Error(ex);
                throw;
            }

            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool enabled)
        {
            if (enabled)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                Mod.Enable(modEntry, assembly);
                Menu.Enable(modEntry, assembly);
            }
            else
            {
                Menu.Disable(modEntry);
                Mod.Disable(modEntry, false);
                ReflectionCache.Clear();
            }
            return true;
        }

        private struct MagicItemDataHolder
        {
            public string Name;
            public ItemDefinition Item;
            public RecipeDefinition Recipe;

            public MagicItemDataHolder(string name, ItemDefinition item, RecipeDefinition recipe)
            {
                this.Name = name;
                this.Item = item;
                this.Recipe = recipe;
            }
        }

        internal static void OnGameReady()
        {
            List<ItemDefinition> BaseCrossbows = new List<ItemDefinition>()
            {
                DatabaseHelper.ItemDefinitions.LightCrossbow,
                DatabaseHelper.ItemDefinitions.HeavyCrossbow,
            };

            List<ItemDefinition> PossiblePrimedItemsToReplace = new List<ItemDefinition>()
            {
                DatabaseHelper.ItemDefinitions.Primed_Longbow,
                DatabaseHelper.ItemDefinitions.Primed_Shortbow,
            };

            List<MagicItemDataHolder> BowsToCopy = new List<MagicItemDataHolder>()
            {
                // Same as +1
                new MagicItemDataHolder("Accuracy", DatabaseHelper.ItemDefinitions.Enchanted_Longbow_Of_Accurary,
                    DatabaseHelper.RecipeDefinitions.Recipe_Enchantment_LongbowOfAcurracy),
                // Same as +2
                new MagicItemDataHolder("Sharpshooting", DatabaseHelper.ItemDefinitions.Enchanted_Shortbow_Of_Sharpshooting,
                    DatabaseHelper.RecipeDefinitions.Recipe_Enchantment_ShortbowOfSharpshooting),
                new MagicItemDataHolder("Lightbringer", DatabaseHelper.ItemDefinitions.Enchanted_Longbow_Lightbringer,
                    DatabaseHelper.RecipeDefinitions.Recipe_Enchantment_LongbowLightbringer),
                new MagicItemDataHolder("Stormbow", DatabaseHelper.ItemDefinitions.Enchanted_Longbow_Stormbow,
                    DatabaseHelper.RecipeDefinitions.Recipe_Enchantment_LongsbowStormbow),
                new MagicItemDataHolder("Medusa", DatabaseHelper.ItemDefinitions.Enchanted_Shortbow_Medusa,
                    DatabaseHelper.RecipeDefinitions.Recipe_Enchantment_ShortbowMedusa),
            };

            foreach (ItemDefinition baseItem in BaseCrossbows)
            {
                foreach(MagicItemDataHolder itemData in BowsToCopy)
                {
                    // Generate Crossbow items
                    ItemDefinition newCrossbow = ItemBuilder.BuildNewMagicWeapon(baseItem, itemData.Item, itemData.Name);
                    // Generate recipes for crossbows
                    string recipeName = "RecipeEnchanting" + newCrossbow.Name;
                    RecipeBuilder builder = new RecipeBuilder(recipeName, GuidHelper.Create(Main.ModGuidNamespace, recipeName).ToString());
                    builder.AddIngredient(baseItem);
                    foreach(IngredientOccurenceDescription ingredient in itemData.Recipe.Ingredients)
                    {
                        if (PossiblePrimedItemsToReplace.Contains(ingredient.ItemDefinition))
                        {
                            continue;
                        }
                        builder.AddIngredient(ingredient);
                    }
                    builder.SetCraftedItem(newCrossbow);
                    builder.SetCraftingCheckData(itemData.Recipe.CraftingHours, itemData.Recipe.CraftingDC, itemData.Recipe.ToolType);
                    RecipeDefinition newRecipe = builder.AddToDB();
                    // Stock Crossbow Recipes
                    ItemDefinition craftintgManual = ItemBuilder.BuilderCopyFromItemSetRecipe(newRecipe, DatabaseHelper.ItemDefinitions.CraftingManual_Enchant_Longbow_Of_Accuracy,
                    "CraftingManual_" + newRecipe.Name, DatabaseHelper.ItemDefinitions.CraftingManualRemedy.GuiPresentation, 200);
                    StockItem(DatabaseHelper.MerchantDefinitions.Store_Merchant_Circe, craftintgManual);
                    StockItem(DatabaseHelper.MerchantDefinitions.Store_Merchant_Gorim_Ironsoot_Cyflen_GeneralStore, craftintgManual);
                }

            }
            // Bolt +2
        }

        private static void StockItem(MerchantDefinition merchant, ItemDefinition item)
        {
            StockUnitDescription stockUnit = new StockUnitDescription();
            stockUnit.SetItemDefinition(item);
            stockUnit.SetInitialAmount(1);
            stockUnit.SetInitialized(true);
            stockUnit.SetMaxAmount(2);
            stockUnit.SetMinAmount(1);
            stockUnit.SetStackCount(1);
            stockUnit.SetReassortAmount(1);
            merchant.StockUnitDescriptions.Add(stockUnit);
        }


    }
}
