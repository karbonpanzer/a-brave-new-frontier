using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using RimWorld;

namespace BNF.StyleSwitcher
{
	// Safe weapon disabler: avoids deep recursive mutation and skips ThingSetMaker alteration by default.
	// This focuses on trader stockGenerators and recipes, and annotates defs. It's intentionally conservative.
	public static class BNF_WeaponDisabler
	{
		public static void ApplyAll(BNFSettings settings)
		{
			if (settings == null) return;

			try
			{
				AnnotateDefs(settings);
				RemoveFromTraderStockGenerators(settings);
				// NOTE: Removing from ThingSetMakerDefs can be dangerous; skip by default to avoid crashes.
				//RemoveFromThingSetMakerDefs(settings);
				RemoveFromRecipes(settings);

				Log.Message("[BNF] Weapon pools updated (safe mode). Reopen trader windows to refresh current offers.");
			}
			catch (Exception e)
			{
				Log.Error($"[BNF] WeaponDisabler.ApplyAll unexpected error: {e}");
			}
		}

		static void AnnotateDefs(BNFSettings settings)
		{
			foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
			{
				try
				{
					var wext = def.GetModExtension<BNFWeaponExtension>();
					if (wext == null) continue;

					bool disabled = settings.IsWeaponDisabled(def);

					string desc = def.description ?? string.Empty;

					if (disabled)
					{
						def.tradeability = Tradeability.None;
						def.BaseMarketValue = 0f;
						if (!desc.StartsWith("(Disabled) "))
							def.description = "(Disabled) " + desc;
					}
					else
					{
						if (desc.StartsWith("(Disabled) "))
							def.description = desc.Substring("(Disabled) ".Length);
					}
				}
				catch (Exception e)
				{
					Log.Warning($"[BNF] AnnotateDefs failed for {def?.defName ?? "null"}: {e}");
				}
			}
		}

		static void RemoveFromTraderStockGenerators(BNFSettings settings)
		{
			foreach (var trader in DefDatabase<TraderKindDef>.AllDefs)
			{
				try
				{
					if (trader.stockGenerators == null) continue;

					// stockGenerators is List<StockGenerator>
					var keep = new List<StockGenerator>();
					foreach (var gen in trader.stockGenerators)
					{
						bool remove = false;
						try
						{
							var td = GetThingDefFromObject(gen);
							if (td != null && settings.IsWeaponDisabled(td))
							{
								remove = true;
								Log.Message($"[BNF] Removing stock generator referencing disabled thingDef {td.defName} from trader {trader.defName}");
							}
						}
						catch (Exception ex)
						{
							Log.Warning($"[BNF] Error inspecting stockGenerator for trader {trader.defName}: {ex} (kept generator to be safe)");
							remove = false;
						}

						if (!remove) keep.Add(gen);
					}

					if (keep.Count != trader.stockGenerators.Count)
					{
						try
						{
							trader.stockGenerators = keep;
							Log.Message($"[BNF] Updated trader {trader.defName} stockGenerators");
						}
						catch (Exception ex)
						{
							Log.Warning($"[BNF] Failed to assign updated stockGenerators for {trader.defName}: {ex}");
						}
					}
				}
				catch (Exception e)
				{
					Log.Warning($"[BNF] Error while cleaning trader {trader.defName}: {e}");
				}
			}
		}

		static void RemoveFromThingSetMakerDefs(BNFSettings settings)
		{
			// left intentionally empty in safe mode - implement later with care
		}

		static void RemoveFromRecipes(BNFSettings settings)
		{
			foreach (var recipe in DefDatabase<RecipeDef>.AllDefs)
			{
				try
				{
					bool producesDisabled = false;

					if (recipe.products != null)
					{
						foreach (var prod in recipe.products)
						{
							if (prod?.thingDef != null && settings.IsWeaponDisabled(prod.thingDef))
							{
								producesDisabled = true;
								break;
							}
						}
					}

					if (producesDisabled)
					{
						try { recipe.recipeUsers = new List<ThingDef>(); }
						catch (Exception ex) { Log.Warning($"[BNF] Failed to clear recipeUsers for {recipe.defName}: {ex}"); }
					}
				}
				catch (Exception e)
				{
					Log.Warning($"[BNF] Error scanning RecipeDef {recipe.defName}: {e}");
				}
			}
		}

		static ThingDef GetThingDefFromObject(object obj)
		{
			if (obj == null) return null;
			var t = obj.GetType();

			try { var f = t.GetField("thingDef", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (f != null) return f.GetValue(obj) as ThingDef; } catch { }
			try { var p = t.GetProperty("thingDef", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (p != null) return p.GetValue(obj, null) as ThingDef; } catch { }
			try { var fList = t.GetField("thingDefs", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (fList != null) { var val = fList.GetValue(obj) as IEnumerable; if (val != null) foreach (var item in val) if (item is ThingDef td) return td; } } catch { }
			try { var pList = t.GetProperty("thingDefs", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (pList != null) { var val = pList.GetValue(obj, null) as IEnumerable; if (val != null) foreach (var item in val) if (item is ThingDef td) return td; } } catch { }
			try { var fTdc = t.GetField("thingDefCountClasses", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (fTdc != null) { var val = fTdc.GetValue(obj) as IEnumerable; if (val != null) foreach (var item in val) { var tdObj = item?.GetType().GetField("thingDef")?.GetValue(item) as ThingDef; if (tdObj != null) return tdObj; } } } catch { }

			return null;
		}
	}	
}