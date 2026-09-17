using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace ForceOutfitChange.Jobs
{
    /// <summary>
    /// Job driver that removes apparel and drops it on the floor without hauling to storage.
    /// Based on vanilla JobDriver_RemoveApparel but with haulDroppedApparel = false behavior.
    /// </summary>
    public class JobDriver_ForceRemoveApparel : JobDriver
    {
        private int duration;
        
        private const TargetIndex ApparelInd = TargetIndex.A;
        
        private Apparel Apparel => (Apparel)job.GetTarget(TargetIndex.A).Thing;
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref duration, "duration", 0);
        }
        
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        
        public override void Notify_Starting()
        {
            base.Notify_Starting();
            duration = (int)(Apparel.GetStatValue(StatDefOf.EquipDelay) * 60f);
        }
        
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            
            // Wait while removing the apparel
            yield return Toils_General.Wait(duration)
                .PlaySustainerOrSound(Apparel.def.apparel.soundRemove)
                .WithProgressBarToilDelay(TargetIndex.A);
            
            // Remove and drop the apparel (no hauling)
            yield return Toils_General.Do(delegate
            {
                if (!pawn.apparel.WornApparel.Contains(Apparel))
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }
                
                if (pawn.apparel.TryDrop(Apparel, out Apparel droppedApparel))
                {
                    // Mark as not forbidden so it can be picked up later if needed
                    droppedApparel.SetForbidden(false, false);
                    
                    if (Prefs.DevMode)
                    {
                        Messages.Message($"[ForceOutfitChange] {pawn.LabelShort} dropped {droppedApparel.LabelShort}", 
                            droppedApparel, MessageTypeDefOf.NeutralEvent, false);
                    }
                    
                    EndJobWith(JobCondition.Succeeded);
                }
                else
                {
                    EndJobWith(JobCondition.Incompletable);
                }
            });
        }
    }
}
