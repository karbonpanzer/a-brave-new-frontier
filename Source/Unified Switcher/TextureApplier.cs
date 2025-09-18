using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace BNF.StyleSwitcher
{
    // Removed StaticConstructorOnStartup to avoid init/unload ordering issues.
    public static class TextureApplier
    {
        /// <summary>
        /// Lightweight apply: set texPath strings only. Does not force recreation of Graphics or unload assets.
        /// Safe to call at most once after load.
        /// </summary>
        public static void ApplyAll(BNFSettings settings)
        {
            if (settings == null) return;
            if (!settings.EnableTextureSwitch)
            {
                Log.Message("[BNF] Texture switching disabled in settings; skipping ApplyAll.");
                return;
            }

            try
            {
                int changed = 0;
                foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    var ext = def.GetModExtension<BNFTextureExtension>();
                    if (ext == null) continue;

                    var chosen = settings.UseGreyscaleTextures ? ext.greyscalePath : ext.originalPath;
                    if (string.IsNullOrEmpty(chosen)) continue;
                    chosen = PathUtil.Normalize(chosen);

                    // set the texPath string only. Do NOT null def.graphic here.
                    if (def.graphicData != null && def.graphicData.texPath != chosen)
                    {
                        def.graphicData.texPath = chosen;
                        changed++;
                    }

                    if (def.apparel != null && def.apparel.wornGraphicPath != chosen)
                    {
                        def.apparel.wornGraphicPath = chosen;
                        changed++;
                    }
                }
                Log.Message($"[BNF] ApplyAll finished. Texture path updates requested for {changed} entries.");
            }
            catch (Exception e)
            {
                Log.Warning($"[BNF] TextureApplier.ApplyAll failed: {e}");
            }
        }

        /// <summary>
        /// Force recreate graphics for defs that changed. This is an explicit, potentially heavy operation.
        /// It will run on the main thread via LongEventHandler.QueueLongEvent with doAsync=false.
        /// Call this only in response to a user action (Apply Now), not automatically on shutdown.
        /// </summary>
        public static void ForceRecreateGraphicsSafe(BNFSettings settings)
        {
            if (settings == null) return;
            if (!settings.EnableTextureSwitch)
            {
                Log.Message("[BNF] Texture switching disabled in settings; skipping ForceRecreateGraphicsSafe.");
                return;
            }

            // build a list of defs to refresh so we can recreate in a controlled manner on main thread
            var toRefresh = new List<Tuple<ThingDef, string>>();
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                var ext = def.GetModExtension<BNFTextureExtension>();
                if (ext == null) continue;
                var chosen = settings.UseGreyscaleTextures ? ext.greyscalePath : ext.originalPath;
                if (string.IsNullOrEmpty(chosen)) continue;
                chosen = PathUtil.Normalize(chosen);

                // only include if texPath differs from current graphicData (minimizes churn)
                if (def.graphicData != null && def.graphicData.texPath != chosen)
                {
                    toRefresh.Add(Tuple.Create(def, chosen));
                }
                else if (def.apparel != null && def.apparel.wornGraphicPath != chosen && def.graphicData != null)
                {
                    toRefresh.Add(Tuple.Create(def, chosen));
                }
            }

            if (toRefresh.Count == 0)
            {
                Log.Message("[BNF] ForceRecreateGraphicsSafe: nothing to refresh.");
                return;
            }

            // Queue the recreation on the main thread with a progress screen. doAsync=false is critical.
            LongEventHandler.QueueLongEvent(() =>
            {
                try
                {
                    int done = 0;
                    foreach (var tup in toRefresh)
                    {
                        var def = tup.Item1;
                        var chosen = tup.Item2;

                        // Set the texPath (again) and create a new Graphic instance using safe API
                        if (def.graphicData != null)
                        {
                            def.graphicData.texPath = chosen;

                            // Attempt to create the appropriate Graphic type. Use try/catch per-def to avoid a single failure halting the loop.
                            try
                            {
                                // Use Cutout shader for most RimWorld textures; change if you have special shader needs
                                var shader = ShaderDatabase.Cutout;
                                var draw = def.graphicData.drawSize;
                                def.graphic = GraphicDatabase.Get<Graphic_Multi>(chosen, shader, draw, Color.white, Color.white);
                            }
                            catch (Exception e)
                            {
                                Log.Warning($"[BNF] Failed to recreate graphic for {def.defName}: {e}");
                            }
                            done++;
                        }
                    }
                    Log.Message($"[BNF] ForceRecreateGraphicsSafe completed: recreated {done} graphics.");
                }
                catch (Exception e)
                {
                    Log.Warning($"[BNF] ForceRecreateGraphicsSafe failed: {e}");
                }
            }, "BNF_RecreatingGraphics", false, null); // doAsync=false ensures main-thread execution
        }

        /// <summary>
        /// Safe initializer: call this from your Mod constructor (or other safe place) to defer initial apply until after load finishes.
        /// </summary>
        public static void InitializeDeferred(BNFSettings settings)
        {
            // ExecuteWhenFinished ensures we run after RimWorld has finished initial loading.
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                ApplyAll(settings);
            });
        }
    }
}
