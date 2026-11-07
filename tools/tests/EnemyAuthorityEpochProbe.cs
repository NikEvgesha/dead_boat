using System;
using DeadBoat.Online;

// Executes the actual pure readiness gate, not Unity actors or Photon transport.
internal static class EnemyAuthorityEpochProbe
{
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static int Main()
    {
        try
        {
            var run = new object(); var nextRun = new object(); var gate = new EnemyAuthorityEpoch();
            int canonicalHP = 40, localHP = 150, phase = 0, mask = 0, restores = 0, resumes = 0;
            bool pagesOwned = false;
            Func<bool> restore = () => {
                if (!pagesOwned) return false;
                restores++; localHP = canonicalHP; phase = 2; mask = 1;
                Require(!gate.TryPrepare(run, 1, () => throw new Exception("recursive restore"), () => {}), "no reentrant simulation during restore");
                return true;
            };
            Action resume = () => {
                Require(gate.IsReady(run, 1) && localHP == canonicalHP && phase == 2 && mask == 1, "resume only after all canonical state is installed");
                Require(gate.TryPrepare(run, 1, () => throw new Exception("resume reentry restored again"), () => {}), "resume can query authority");
                resumes++;
            };
            // Reproduce the stale-local overwrite using the previous order, without Unity.
            Require(localHP - 5 == 145 && canonicalHP - 5 == 35, "fixture exposes stale burn rollback");
            Require(!gate.TryPrepare(run, 1, restore, resume) && restores == 0 && resumes == 0, "root alone cannot resume while pages are unowned");
            pagesOwned = true;
            Require(gate.TryPrepare(run, 1, restore, resume), "delayed page ownership permits preparation");
            localHP -= 5; canonicalHP = localHP;
            Require(localHP == 35, "first burn follows canonical HP, never stale local 150");
            Require(gate.TryPrepare(run, 1, restore, resume), "subsequent sample shares the same preparation");
            localHP -= 7;
            Require(localHP == 28 && restores == 1 && resumes == 1, "ongoing damage is not rewound or coroutines restarted every frame");
            Require((mask & 1) != 0, "already hit dive victim survives preparation");
            mask |= 2;
            Require(gate.TryPrepare(run, 1, restore, resume) && mask == 3, "new dive victim is not erased by next authority query");

            gate.Invalidate(); canonicalHP = 12;
            Require(gate.TryPrepare(run, 1, restore, resume) && localHP == 12 && restores == 2, "losing and regaining authority rehydrates even with same owner ID");
            int before = restores;
            Require(gate.TryPrepare(run, 2, () => { restores++; return true; }, () => {}) && restores == before + 1, "different owner starts another epoch");
            Require(gate.TryPrepare(nextRun, 2, () => { restores++; return true; }, () => {}) && restores == before + 2, "different run with same owner cannot reuse readiness");

            gate.Invalidate(); int obsoleteResume = 0;
            Require(!gate.TryPrepare(run, 1, () => { gate.Invalidate(); return true; }, () => obsoleteResume++), "invalidation during restore cancels obsolete completion");
            Require(obsoleteResume == 0 && !gate.IsReady(run, 1), "obsolete attempt cannot resume");
            Require(!gate.TryPrepare(run, 1, () => {
                Require(!gate.TryPrepare(nextRun, 2, () => true, () => {}), "new context cannot enter an old preparation");
                return true;
            }, () => obsoleteResume++), "context replacement cancels outer preparation");
            Require(obsoleteResume == 0, "no callback from replaced run");

            foreach (bool failInResume in new[] { false, true })
            {
                gate.Invalidate(); bool threw = false;
                try { gate.TryPrepare(run, 1, () => { if (!failInResume) throw new InvalidOperationException("restore"); return true; }, () => { throw new InvalidOperationException("resume"); }); }
                catch (InvalidOperationException) { threw = true; }
                Require(threw && !gate.IsReady(run, 1), "restore/resume exception cannot leave false readiness");
                Require(gate.TryPrepare(run, 1, () => true, () => {}), "retry remains possible after failure");
            }
            Require(!gate.TryPrepare(null, 1, () => throw new Exception("null context restored"), () => {}), "missing context invalidates readiness");
            var reentrantGate = new EnemyAuthorityEpoch(); var actionRun = new object();
            long oldEntry = reentrantGate.Generation; int innerActions = 0, obsoleteActions = 0;
            Require(reentrantGate.TryPrepare(actionRun, 1, () => true, () => {
                long newEntry = reentrantGate.Generation;
                if (reentrantGate.IsCurrent(newEntry)) innerActions++;
            }), "world prepared inside the old local action");
            if (reentrantGate.IsCurrent(oldEntry)) obsoleteActions++;
            Require(innerActions == 1 && obsoleteActions == 0, "restored attack can start while the obsolete outer attack/selection is rejected");
            long stableEntry = reentrantGate.Generation;
            Require(reentrantGate.IsCurrent(stableEntry), "ordinary actions in the prepared world remain allowed");
            reentrantGate.Invalidate();
            Require(!reentrantGate.IsCurrent(stableEntry), "authority loss invalidates captured action entry");
            Require(!BossRecoveryRules.CanonicalDefeat(true, 25, 100), "a local Win signal cannot prove victory while canonical boss HP is positive");
            Require(BossRecoveryRules.CanonicalDefeat(true, 0, 100) && !BossRecoveryRules.CanonicalDefeat(false, 0, 100), "canonical defeat requires the bound final boss");
            Console.WriteLine("PASS: production epoch generation rejects obsolete outer actions while allowing restored inner actions; canonical defeat proof rejects positive HP/wrong boss. No Unity event execution.");
            // The previous leader's death callback can disappear after it disabled the dragon.
            gate.Invalidate(); bool verdict = false; int hp = 25; bool actorActive = false;
            Require(gate.TryPrepare(run, 3, () => true, () => verdict |= BossRecoveryRules.RecoverInactiveDragon(true, hp, 100, actorActive)) && !verdict,
                "inactive/dead-looking local actor cannot override positive canonical HP");
            gate.Invalidate(); hp = 0;
            Require(gate.TryPrepare(run, 4, () => true, () => verdict |= BossRecoveryRules.RecoverInactiveDragon(true, hp, 100, actorActive)) && verdict,
                "defeated inactive final dragon recovers verdict without its lost callback");
            Require(!BossRecoveryRules.RecoverInactiveDragon(true, 0, 100, true), "active death animation keeps its ordinary completion path");
            Require(!BossRecoveryRules.RecoverInactiveDragon(false, 0, 100, false), "wrong boss identity cannot finalize this run");
            Require(!BossRecoveryRules.RecoverInactiveDragon(true, 0, 0, false), "uninitialized HP is not proof of defeat");
            Require(BossRecoveryRules.RecoverInactiveDragon(true, -10, 100, false), "lethal overkill also proves defeat");

            var parts = new TentacleRecoveryEvidence();
            Require(parts.Action == TentacleRecoveryAction.Wait, "empty boss cannot complete");
            parts.Observe(true, 0, 100, true, false);
            parts.Observe(false, 0, 0, false, false);
            Require(parts.Action == TentacleRecoveryAction.Wait, "one missing part blocks a false victory");
            parts = new TentacleRecoveryEvidence();
            parts.Observe(true, 0, 100, true, false); parts.Observe(true, -20, 100, true, false);
            Require(parts.Action == TentacleRecoveryAction.Victory, "all known defeated parts recover final verdict");
            parts.Observe(true, 30, 100, false, false);
            Require(parts.Action == TentacleRecoveryAction.SelectNext, "one surviving inactive part prevents victory and selects next");
            parts.Observe(true, 40, 100, false, true);
            Require(parts.Action == TentacleRecoveryAction.ResumeActive, "existing active part resumes without choosing another");
            parts.Observe(true, 60, 100, true, false);
            Require(parts.Action == TentacleRecoveryAction.Wait, "canonical alive/local dead mismatch cannot resume a corpse");
            parts = new TentacleRecoveryEvidence(); parts.Observe(true, 0, 0, true, false);
            Require(parts.Action == TentacleRecoveryAction.Wait, "invalid maximum cannot prove defeat");
            parts = new TentacleRecoveryEvidence(); parts.Observe(true, 120, 100, false, true);
            Require(parts.Action == TentacleRecoveryAction.Wait, "invalid current HP blocks recovery");
            gate.Invalidate(); var lateRun = new object();
            bool lateMissing = true, latePending = false; int lateLocal = 150, lateCanonical = 25, lateResumes = 0;
            Func<TentacleRecoveryAction> decideLate = () => {
                var facts = new TentacleRecoveryEvidence();
                facts.Observe(true, 0, 100, true, false);
                facts.Observe(!lateMissing, lateCanonical, 100, false, false);
                return facts.Action;
            };
            Func<bool> restoreLate = () => { if (!lateMissing) lateLocal = lateCanonical; return true; };
            Action resumeLate = () => { latePending = decideLate() == TentacleRecoveryAction.Wait; if (!latePending) lateResumes++; };
            Require(gate.TryPrepare(lateRun, 9, restoreLate, resumeLate) && latePending && lateResumes == 0,
                "incomplete group can remain deferred after the root epoch is ready");
            lateMissing = false;
            Require(decideLate() == TentacleRecoveryAction.SelectNext && lateLocal == 150,
                "late canonical data can become complete while the local actor is still stale");
            gate.Invalidate(); // model of the runtime's deferred retry after its sampling pass
            Require(gate.TryPrepare(lateRun, 9, restoreLate, resumeLate) && !latePending && lateLocal == 25 && lateResumes == 1,
                "deferred completion restores the new canonical HP before resuming");
            lateLocal -= 5;
            Require(gate.TryPrepare(lateRun, 9, restoreLate, resumeLate) && lateLocal == 20 && lateResumes == 1,
                "settled group does not retry or reset its attack every frame");
            Console.WriteLine("PASS: modelled late-record retry using production epoch/evidence rules; incomplete group, late canonical HP25 over local150, restore before one resume, ongoing HP20 preserved. Runtime sampling/queue wiring not executed.");
            Console.WriteLine("PASS: production BossRecoveryRules/TentacleRecoveryEvidence; lost inactive dragon callback; no false win from positive/missing/invalid HP or wrong boss; active animation preserved; complete/partial/live/active/corpse tentacle groups. No Unity actor/transport/reward execution.");
            Console.WriteLine("PASS: real EnemyAuthorityEpoch; stale burn model 145 -> 35; page wait; restore-before-resume; once per epoch; ongoing damage/mask preserved; owner/run change; invalidate/reentry; exceptions/retry/null. No Unity actors, physics, coroutines, rewards, Photon or Player build.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
