using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace ForceOutfitChange.Jobs
{
    /// <summary>
    /// Job driver that wears apparel while dropping conflicting clothes on the floor without hauling.
    /// Based on vanilla JobDriver_Wear but with modified dropping behavior.
    /// </summary>
    public class JobDriver_ForceWear : JobDriver
    {
        private int duration;
        private int unequipBuffer;
        
        private const TargetIndex ApparelInd = TargetIndex.A;
        private const TargetIndex ApparelSourceIndex = TargetIndex.B;
        
        private Apparel Apparel => (Apparel)job.GetTarget(TargetIndex.A).Thing;
        
        private bool TargetIsOnApparelSource
        {
            get
            {
                Apparel apparel = Apparel;
                if (apparel != null && !apparel.Spawned && apparel.ParentHolder is IApparelSource apparelSource)
                {
                    return apparelSource is Thing;
                }
                return false;
            }
        }
        
        private IApparelSource ApparelSource => (IApparelSource)job.GetTarget(TargetIndex.B).Thing;
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref duration, "duration", 0);
            Scribe_Values.Look(ref unequipBuffer, "unequipBuffer", 0);
        }
        
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (TargetIsOnApparelSource)
            {
                return pawn.Reserve((Thing)Apparel.ParentHolder, job, 1, -1, null, errorOnFailed);
            }
            return pawn.Reserve(Apparel, job, 1, -1, null, errorOnFailed);
        }
        
        public override void Notify_Starting()
        {
            base.Notify_Starting();
            if (TargetIsOnApparelSource)
            {
                job.targetB = (Thing)Apparel.ParentHolder;
            }
            duration = (int)(Apparel.GetStatValue(StatDefOf.EquipDelay) * 60f);
            
            // Calculate time for unequipping conflicting apparel
            Apparel apparel = Apparel;
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int num = wornApparel.Count - 1; num >= 0; num--)
            {
                if (!ApparelUtility.CanWearTogether(apparel.def, wornApparel[num].def, pawn.RaceProps.body))
                {
                    duration += (int)(wornApparel[num].GetStatValue(StatDefOf.EquipDelay) * 60f);
                }
            }
        }
        
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnBurningImmobile(TargetIndex.A);
            bool usingSource = TargetIsOnApparelSource;
            
            // Go to the apparel location
            if (usingSource)
            {
                yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.InteractionCell)
                    .FailOnDespawnedNullOrForbidden(TargetIndex.B);
            }
            else
            {
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch)
                    .FailOnDespawnedNullOrForbidden(TargetIndex.A);
            }
            
            // Main equip toil: remove conflicting apparel and wear new apparel
            Toil equipToil = ToilMaker.MakeToil("ForceWear");
            equipToil.tickIntervalAction = delegate(int delta)
            {
                unequipBuffer += delta;
                TryUnequipSomething();
                pawn.rotationTracker.FaceTarget(Apparel.PositionHeld);
            };
            equipToil.WithProgressBarToilDelay((!usingSource) ? TargetIndex.A : TargetIndex.B);
            equipToil.FailOnDespawnedNullOrForbidden((!usingSource) ? TargetIndex.A : TargetIndex.B);
            equipToil.defaultCompleteMode = ToilCompleteMode.Delay;
            equipToil.defaultDuration = duration;
            equipToil.handlingFacing = true;
            equipToil.PlaySustainerOrSound(GetCurrentWearSound);
            yield return equipToil;
            
            // Final toil: equip the new apparel
            yield return Toils_General.Do(delegate
            {
                Apparel apparel = Apparel;
                if (usingSource)
                {
                    ApparelSource.RemoveApparel(apparel);
                }
                pawn.apparel.Wear(apparel);
                if (pawn.outfits != null && job.playerForced)
                {
                    pawn.outfits.forcedHandler.SetForced(apparel, forced: true);
                }
                

            });
        }
        
        private SoundDef GetCurrentWearSound()
        {
            Apparel apparel = Apparel;
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int num = wornApparel.Count - 1; num >= 0; num--)
            {
                if (!ApparelUtility.CanWearTogether(apparel.def, wornApparel[num].def, pawn.RaceProps.body))
                {
                    if (unequipBuffer >= (int)(wornApparel[num].GetStatValue(StatDefOf.EquipDelay) * 60f))
                    {
                        break;
                    }
                    return wornApparel[num].def.apparel.soundRemove;
                }
            }
            return apparel.def.apparel.soundWear;
        }
        
        /// <summary>
        /// Modified version that drops conflicting apparel on the floor instead of queueing haul jobs.
        /// </summary>
        private void TryUnequipSomething()
        {
            Apparel targetApparel = Apparel;
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            
            for (int num = wornApparel.Count - 1; num >= 0; num--)
            {
                Apparel wornItem = wornApparel[num];
                if (!ApparelUtility.CanWearTogether(targetApparel.def, wornItem.def, pawn.RaceProps.body))
                {
                    if (unequipBuffer >= (int)(wornItem.GetStatValue(StatDefOf.EquipDelay) * 60f))
                    {
                        // Drop the apparel here without hauling - key difference from vanilla!
                        if (pawn.apparel.TryDrop(wornItem, out Apparel droppedApparel))
                        {
                            // Reset forbidden flag so it can be picked up later
                            droppedApparel.SetForbidden(false, false);
                            unequipBuffer = 0;
                            
                            if (Prefs.DevMode)
                            {
                                Messages.Message($"[ForceOutfitChange] {pawn.LabelShort} dropped {droppedApparel.LabelShort} to wear {targetApparel.LabelShort}", 
                                    droppedApparel, MessageTypeDefOf.NeutralEvent, false);
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}
