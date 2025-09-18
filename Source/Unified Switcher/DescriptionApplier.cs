using System;
using System.Text;
using Verse;

namespace BNF.StyleSwitcher
{
    // Safe, instrumented Description applier.
    // This file intentionally does NOT use StaticConstructorOnStartup or automatic deferred initialization.
    public static class DescriptionApplier
    {
        /// <summary>
        /// Compatibility wrapper used by older callers.
        /// For safety we forward to the queued-with-progress implementation.
        /// </summary>
        public static void ApplyAll(BNFSettings settings)
        {
            // Forward to queued version for safety and progress UI.
            ApplyAllQueuedWithProgress(settings);
        }

        /// <summary>
        /// Apply descriptions immediately on the calling thread.
        /// Only call this from the main thread (e.g., a settings button) to be safe.
        /// This will not touch Unity objects; it's string assignment only.
        /// </summary>
        public static void ApplyAllImmediateSafe(BNFSettings settings)
        {
            if (settings == null)
            {
                Log.Warning("[BNF] Description applier called with null settings, aborting.");
                return;
            }

            if (!settings.EnableDescriptionSwitch)
            {
                Log.Message("[BNF] Description switching disabled in settings; skipping ApplyAllImmediateSafe.");
                return;
            }

            try
            {
                Log.Message("[BNF] DescriptionApplier.ApplyAllImmediateSafe started.");
                var sw = System.Diagnostics.Stopwatch.StartNew();
                int changed = 0;
                int total = 0;
                foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    total++;
                    try
                    {
                        var ext = def.GetModExtension<BNFDescriptionExtension>();
                        if (ext == null) continue;

                        var newText = settings.UseLoreDescriptions ? ext.loreDesc : ext.vanillaDesc;
                        if (string.IsNullOrEmpty(newText)) continue;

                        if (def.description != newText)
                        {
                            def.description = newText;
                            changed++;
                        }
                    }
                    catch (Exception inner)
                    {
                        Log.Warning($"[BNF] Exception while processing def {def?.defName ?? "<null>"}: {inner}");
                    }

                    // micro-progress log to help capture long-run stalls
                    if (total % 1000 == 0)
                    {
                        Log.Message($"[BNF] DescriptionApplier progress: processed {total} defs, changed {changed} so far.");
                    }
                }
                sw.Stop();
                Log.Message($"[BNF] DescriptionApplier.ApplyAllImmediateSafe finished. Processed {total} defs, changed {changed} in {sw.ElapsedMilliseconds} ms.");
            }
            catch (Exception e)
            {
                Log.Warning($"[BNF] DescriptionApplier.ApplyAllImmediateSafe top-level failure: {e}");
            }
        }

        /// <summary>
        /// Queue the same operation as a main-thread long event with progress UI.
        /// This is safer for very large sets and avoids blocking the main thread with raw loops.
        /// Note: use positional args for QueueLongEvent so it matches the RimWorld API.
        /// </summary>
        public static void ApplyAllQueuedWithProgress(BNFSettings settings)
        {
            if (settings == null)
            {
                Log.Warning("[BNF] Description applier (queued) called with null settings, aborting.");
                return;
            }

            if (!settings.EnableDescriptionSwitch)
            {
                Log.Message("[BNF] Description switching disabled in settings; skipping ApplyAllQueuedWithProgress.");
                return;
            }

            // Positional args: (Action action, string text, bool doAsync, Action onFinished)
            LongEventHandler.QueueLongEvent(() =>
            {
                try
                {
                    Log.Message("[BNF] DescriptionApplier.ApplyAllQueuedWithProgress started (queued).");
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    int changed = 0;
                    int total = 0;
                    foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
                    {
                        total++;
                        try
                        {
                            var ext = def.GetModExtension<BNFDescriptionExtension>();
                            if (ext == null) continue;

                            var newText = settings.UseLoreDescriptions ? ext.loreDesc : ext.vanillaDesc;
                            if (string.IsNullOrEmpty(newText)) continue;

                            if (def.description != newText)
                            {
                                def.description = newText;
                                changed++;
                            }
                        }
                        catch (Exception inner)
                        {
                            Log.Warning($"[BNF] (queued) Exception while processing def {def?.defName ?? "<null>"}: {inner}");
                        }

                        if (total % 1000 == 0)
                        {
                            LongEventHandler.SetCurrentEventText($"BNF: applied descriptions to {total} defs...");
                        }
                    }
                    sw.Stop();
                    Log.Message($"[BNF] DescriptionApplier.ApplyAllQueuedWithProgress finished. Processed {total} defs, changed {changed} in {sw.ElapsedMilliseconds} ms.");
                }
                catch (Exception e)
                {
                    Log.Warning($"[BNF] DescriptionApplier.ApplyAllQueuedWithProgress top-level failure: {e}");
                }
            }, "BNF_ApplyingDescriptions", false, null);
        }
    }
}
